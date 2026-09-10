using ArcGIS.Core.Data;
using ArcGIS.Core.Events;
using ArcGIS.Desktop.Core;
using ArcGIS.Desktop.Core.Events;
using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Dialogs;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using ArcGIS.Desktop.Mapping.Events;
using ConstructionAddIn.Helpers.PointHelpers;
using ConstructionAddIn.Services;
using ConstructionAddIn.Tools.RemovingTools;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using HorizontalAlignment = System.Windows.HorizontalAlignment;
using MessageBox = ArcGIS.Desktop.Framework.Dialogs.MessageBox;
using Orientation = System.Windows.Controls.Orientation;

namespace ConstructionAddIn.Tools.RemoveTools
{
    /// <summary>
    /// Generic delete workflow for construction point fitting features.
    ///
    /// This tool activates rectangle selection, waits until the target layer
    /// has a selected feature, verifies that the feature was created within the last 14 days
    /// (or validates supervisor passcode), archives authorized overrides into GeoJSON,
    /// deletes the feature, and returns to the Explore tool.
    /// </summary>
    public static class RemovePointFeatureTool
    {
        private static SubscriptionToken _selectionChangedToken;
        private static bool _isRunning;

        // Administrator passcode for overriding the creation date restriction
        private const string ADMIN_SECRET_CODE = "Admin@2026";
        private const int MAX_ARCHIVE_RECORDS = 100;
        private const string ARCHIVE_FILENAME = "DeletedFeaturesArchive.txt";

        public static async Task StartAsync(PointDeleteRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.TargetLayerName))
            {
                MessageBox.Show("Delete request is invalid.", "Delete Feature");
                return;
            }

            PointDeleteContext.CurrentRequest = request;

            await ClearSelectionAsync();

            SubscribeSelectionChanged();

            _isRunning = true;

            await FrameworkApplication.SetCurrentToolAsync("esri_mapping_selectByRectangleTool");
        }

        private static void SubscribeSelectionChanged()
        {
            if (_selectionChangedToken != null)
                return;

            _selectionChangedToken = MapSelectionChangedEvent.Subscribe(OnMapSelectionChanged);
        }

        private static async void OnMapSelectionChanged(MapSelectionChangedEventArgs args)
        {
            try
            {
                if (!_isRunning)
                    return;

                var request = PointDeleteContext.CurrentRequest;
                if (request == null)
                    return;

                if (!await TargetLayerHasSelectionAsync(request.TargetLayerName))
                    return;

                // Stop accepting selection changes while processing
                _isRunning = false;

                string currentOperator = GetCurrentOperatorName();

                // Validate creation date constraint & package GeoJSON archive snapshots
                var (isRestricted, restrictionMessage, archiveFeatures) = await ValidateFeatureCreatedWithinDaysAsync(request.TargetLayerName, 14, currentOperator);

                bool isPasscodeBypassUsed = false;

                if (isRestricted)
                {
                    if (!DeletionSessionManager.IsSessionActive())
                    {
                        var promptResult = MessageBox.Show(
                            $"{restrictionMessage}\n\nStandard deletion is restricted to features created within the past 14 days.\nDo you want to enter the supervisor passcode to authorize deletion?",
                            "Passcode Override Required",
                            System.Windows.MessageBoxButton.YesNo,
                            System.Windows.MessageBoxImage.Warning);

                        if (promptResult != System.Windows.MessageBoxResult.Yes)
                            return;

                        if (!PromptPasscodeDialog())
                        {
                            MessageBox.Show("Incorrect passcode or operation cancelled. Deletion blocked.", "Access Denied", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                            return;
                        }

                        DeletionSessionManager.StartSession(TimeSpan.FromHours(1));
                        isPasscodeBypassUsed = true;
                    }
                    else
                    {
                        isPasscodeBypassUsed = true;
                    }
                }

                try
                {
                    // Signal DeletionController to allow deletion through row interceptors
                    DeletionController.IsAddInDeleting = true;
                    await PointFeatureDeleteService.DeleteSelectedAsync(request);
                }
                finally
                {
                    DeletionController.IsAddInDeleting = false;
                }

                // If override was used, append to GeoJSON file
                if (isPasscodeBypassUsed && archiveFeatures.Count > 0)
                {
                    try
                    {
                        ArchiveFeaturesToGeoJsonFile(archiveFeatures);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Feature was deleted, but archival failed: {ex.Message}", "Archive Warning");
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "Delete Tool Exception");
            }
            finally
            {
                if (!_isRunning)
                {
                    await ClearSelectionAsync();
                    await FrameworkApplication.SetCurrentToolAsync("esri_mapping_exploreTool");

                    PointDeleteContext.CurrentRequest = null;
                    UnsubscribeSelectionChanged();
                }
            }
        }

        /// <summary>
        /// Validates that the selected features on the target layer were created
        /// within the specified number of days from now. Prepares GeoJSON snapshots for archival.
        /// </summary>
        private static async Task<(bool IsRestricted, string Message, List<JsonObject> ArchiveFeatures)> ValidateFeatureCreatedWithinDaysAsync(string targetLayerName, int days, string currentOperator)
        {
            return await QueuedTask.Run(() =>
            {
                var archivedList = new List<JsonObject>();

                var map = MapView.Active?.Map;
                if (map == null)
                    return (false, null, archivedList);

                var targetLayer = map.GetLayersAsFlattenedList()
                    .OfType<FeatureLayer>()
                    .FirstOrDefault(layer =>
                        layer.Name.Equals(targetLayerName, StringComparison.OrdinalIgnoreCase));

                if (targetLayer == null)
                    return (false, null, archivedList);

                var selection = targetLayer.GetSelection();
                if (selection.GetCount() == 0)
                    return (false, null, archivedList);

                var threshold = DateTime.UtcNow.AddDays(-days);

                using (var rowCursor = selection.Search(null))
                {
                    while (rowCursor.MoveNext())
                    {
                        using (var row = rowCursor.Current)
                        {
                            if (row is Feature feature)
                            {
                                var geoJsonObject = CreateGeoJsonObject(feature, targetLayer.Name, currentOperator);
                                if (geoJsonObject != null)
                                    archivedList.Add(geoJsonObject);
                            }

                            var createdDate = GetCreatedDate(row);

                            if (!createdDate.HasValue)
                            {
                                return (true, $"Feature (OID: {row.GetObjectID()}) has no valid creation timestamp.", archivedList);
                            }

                            var createdUtc = createdDate.Value.ToUniversalTime();

                            if (createdUtc < threshold)
                            {
                                return (true, $"Feature (OID: {row.GetObjectID()}) was created on {createdDate.Value.ToLocalTime():g}, which is older than {days} days.", archivedList);
                            }
                        }
                    }
                }

                return (false, null, archivedList);
            });
        }

        /// <summary>
        /// Serializes the deleted point feature into a GeoJSON Feature object with PC operator attribution.
        /// </summary>
        private static JsonObject CreateGeoJsonObject(Feature feature, string layerName, string operatorName)
        {
            try
            {
                var shape = feature.GetShape();
                if (shape == null)
                    return null;

                var featureObj = new JsonObject
                {
                    ["type"] = "Feature",
                    ["deleted_at"] = DateTime.UtcNow.ToString("o"),
                    ["deleted_by"] = operatorName,
                    ["layer_name"] = layerName
                };

                var properties = new JsonObject();
                var fields = feature.GetFields();
                for (int i = 0; i < fields.Count; i++)
                {
                    string fieldName = fields[i].Name;
                    var val = feature[i];
                    if (val == null || val == DBNull.Value)
                    {
                        properties[fieldName] = null;
                    }
                    else if (val is DateTime dt)
                    {
                        properties[fieldName] = dt.ToString("o");
                    }
                    else
                    {
                        properties[fieldName] = val.ToString();
                    }
                }
                featureObj["properties"] = properties;

                string esriJson = shape.ToJson();
                var geomNode = JsonNode.Parse(esriJson);
                featureObj["geometry_esri"] = geomNode;

                return featureObj;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Writes features into a JSON array in a .txt file next to the active .aprx project path, maintaining at most 100 features.
        /// </summary>
        private static void ArchiveFeaturesToGeoJsonFile(List<JsonObject> newEntries)
        {
            string projectUri = Project.Current?.URI;
            if (string.IsNullOrWhiteSpace(projectUri))
                return;

            string projectDirectory = Path.GetDirectoryName(projectUri);
            if (string.IsNullOrEmpty(projectDirectory) || !Directory.Exists(projectDirectory))
                return;

            string filePath = Path.Combine(projectDirectory, ARCHIVE_FILENAME);

            JsonArray archiveArray = new JsonArray();

            if (File.Exists(filePath))
            {
                try
                {
                    string existingJson = File.ReadAllText(filePath);
                    if (!string.IsNullOrWhiteSpace(existingJson))
                    {
                        var parsed = JsonNode.Parse(existingJson);
                        if (parsed is JsonArray jArray)
                        {
                            archiveArray = jArray;
                        }
                    }
                }
                catch
                {
                    archiveArray = new JsonArray();
                }
            }

            foreach (var item in newEntries)
            {
                archiveArray.Add(item);
            }

            // FIFO: delete oldest if total count exceeds 100
            while (archiveArray.Count > MAX_ARCHIVE_RECORDS)
            {
                archiveArray.RemoveAt(0);
            }

            var options = new JsonSerializerOptions { WriteIndented = true };
            File.WriteAllText(filePath, archiveArray.ToJsonString(options));
        }

        /// <summary>
        /// Retrieves the Windows PC operator account (Domain\Username or Machine\Username).
        /// </summary>
        private static string GetCurrentOperatorName()
        {
            try
            {
                string domain = Environment.UserDomainName;
                string user = Environment.UserName;

                if (!string.IsNullOrWhiteSpace(domain) && !string.IsNullOrWhiteSpace(user))
                {
                    return $"{domain}\\{user}";
                }

                return user ?? Environment.MachineName;
            }
            catch
            {
                return Environment.UserName;
            }
        }

        /// <summary>
        /// Displays a modal ProWindow styled identically to native ArcGIS Pro dialogs.
        /// </summary>
        private static bool PromptPasscodeDialog()
        {
            bool isAuthorized = false;

            var window = new ArcGIS.Desktop.Framework.Controls.ProWindow
            {
                Title = "Supervisor Authorization",
                Width = 420,
                Height = 210,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = FrameworkApplication.Current?.MainWindow,
                ResizeMode = ResizeMode.NoResize,
                ShowInTaskbar = false
            };

            window.Resources.MergedDictionaries.Add(new ResourceDictionary
            {
                Source = new Uri("pack://application:,,,/ArcGIS.Desktop.Framework;component/Themes/Default.xaml", UriKind.Absolute)
            });

            var mainGrid = new Grid
            {
                Margin = new Thickness(20)
            };
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            var textLabel = new TextBlock
            {
                Text = "Enter administrator passcode to authorize deletion of older features for 1 hour:",
                FontWeight = FontWeights.SemiBold,
                FontSize = 13,
                Margin = new Thickness(0, 0, 0, 12),
                TextWrapping = TextWrapping.Wrap
            };
            Grid.SetRow(textLabel, 0);

            var passwordBox = new PasswordBox
            {
                Height = 28,
                Margin = new Thickness(0, 0, 0, 16)
            };
            Grid.SetRow(passwordBox, 1);

            var buttonPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right
            };
            Grid.SetRow(buttonPanel, 3);

            var okButton = new System.Windows.Controls.Button
            {
                Content = "Authorize",
                Width = 90,
                Height = 26,
                IsDefault = true,
                Margin = new Thickness(0, 0, 8, 0),
                Style = window.TryFindResource("Esri_ButtonPrimary") as Style
            };

            var cancelButton = new System.Windows.Controls.Button
            {
                Content = "Cancel",
                Width = 90,
                Height = 26,
                IsCancel = true,
                Style = window.TryFindResource("Esri_Button") as Style
            };

            okButton.Click += (s, e) =>
            {
                if (passwordBox.Password == ADMIN_SECRET_CODE)
                {
                    isAuthorized = true;
                    window.DialogResult = true;
                    window.Close();
                }
                else
                {
                    MessageBox.Show(
                        "The passcode you entered is incorrect.",
                        "Invalid Passcode",
                        System.Windows.MessageBoxButton.OK,
                        System.Windows.MessageBoxImage.Warning);
                    passwordBox.Clear();
                    passwordBox.Focus();
                }
            };

            cancelButton.Click += (s, e) =>
            {
                window.DialogResult = false;
                window.Close();
            };

            buttonPanel.Children.Add(okButton);
            buttonPanel.Children.Add(cancelButton);

            mainGrid.Children.Add(textLabel);
            mainGrid.Children.Add(passwordBox);
            mainGrid.Children.Add(buttonPanel);

            window.Content = mainGrid;
            passwordBox.Focus();

            window.ShowDialog();

            return isAuthorized;
        }

        /// <summary>
        /// Reads creation date specifically from known Create Date fields or fallback date attributes.
        /// </summary>
        private static DateTime? GetCreatedDate(Row row)
        {
            if (row == null)
                return null;

            var fields = row.GetFields();
            if (fields == null || fields.Count == 0)
                return null;

            string[] createDateCandidateNames =
            {
                "created_date", "CREATED_DATE", "CreateDate", "DateCreated",
                "GDB_CREATED_DATE", "CREATION_DATE", "CREATED_USER_DATE", "CREATEDDATE"
            };

            foreach (var fieldName in createDateCandidateNames)
            {
                var dt = ExtractDateTimeValue(row, fieldName);
                if (dt.HasValue)
                    return dt;
            }

            for (int i = 0; i < fields.Count; i++)
            {
                var f = fields[i];
                string nameLower = f.Name.ToLowerInvariant();

                if (nameLower.Contains("create") && (f.FieldType == FieldType.Date || nameLower.Contains("date") || nameLower.Contains("time")))
                {
                    var val = row[i];
                    if (val == null || val == DBNull.Value)
                        continue;

                    if (val is DateTime d)
                        return d;
                    if (val is DateTimeOffset dto)
                        return dto.UtcDateTime;
                    if (DateTime.TryParse(val.ToString(), out var parsed))
                        return parsed;
                }
            }

            return null;
        }

        private static DateTime? ExtractDateTimeValue(Row row, string fieldName)
        {
            int index = row.FindField(fieldName);
            if (index == -1)
                return null;

            var val = row[index];
            if (val == null || val == DBNull.Value)
                return null;

            if (val is DateTime dt)
                return dt;

            if (val is DateTimeOffset dto)
                return dto.UtcDateTime;

            if (DateTime.TryParse(val.ToString(), out var parsed))
                return parsed;

            return null;
        }

        /// <summary>
        /// Checks only the requested target layer.
        /// </summary>
        private static async Task<bool> TargetLayerHasSelectionAsync(string targetLayerName)
        {
            return await QueuedTask.Run(() =>
            {
                var map = MapView.Active?.Map;
                if (map == null)
                    return false;

                var targetLayer = map.GetLayersAsFlattenedList()
                    .OfType<FeatureLayer>()
                    .FirstOrDefault(layer =>
                        layer.Name.Equals(targetLayerName, StringComparison.OrdinalIgnoreCase));

                if (targetLayer == null)
                    return false;

                return targetLayer.GetSelection().GetCount() > 0;
            });
        }

        private static async Task ClearSelectionAsync()
        {
            await QueuedTask.Run(() =>
            {
                var map = MapView.Active?.Map;
                map?.ClearSelection();
            });
        }

        private static void UnsubscribeSelectionChanged()
        {
            if (_selectionChangedToken == null)
                return;

            MapSelectionChangedEvent.Unsubscribe(_selectionChangedToken);
            _selectionChangedToken = null;
        }
    }
}