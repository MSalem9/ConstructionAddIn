using ArcGIS.Core.CIM;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Core;
using ArcGIS.Desktop.Core.Events;
using ArcGIS.Desktop.Editing;
using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Dialogs;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using ConstructionAddIn.Services;
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

namespace ConstructionAddIn.Tools.RemovingTools
{
    internal class RemoveLineTool : MapTool
    {
        private string TargetLayerName;

        // Administrator passcode for overriding the creation date restriction
        private const string ADMIN_SECRET_CODE = "Admin@2026";
        private const int MAX_ARCHIVE_RECORDS = 100;
        private const string ARCHIVE_FILENAME = "DeletedFeaturesArchive.txt";

        public RemoveLineTool()
        {
            IsSketchTool = true;
            SketchType = SketchGeometryType.Rectangle;
            SketchOutputMode = SketchOutputMode.Map;
        }

        protected override Task OnToolActivateAsync(bool active)
        {
            TargetLayerName = RemoveLineContext.TargetLayerName;
            return Task.CompletedTask;
        }

        protected override async Task<bool> OnSketchCompleteAsync(Geometry geometry)
        {
            if (geometry == null || geometry.IsEmpty)
            {
                MessageBox.Show("Please draw a valid rectangle.", "Select Area");
                return false;
            }

            // Determine the executing username (ArcGIS Pro Active Portal Account or Windows Login)
            string currentOperator = GetCurrentOperatorName();

            // Retrieve intersecting feature IDs, check creation date, and package archive data
            var (objectIds, isRestricted, restrictionMessage, archiveFeatures) = await QueuedTask.Run(() =>
            {
                var ids = new List<long>();
                var archivedList = new List<JsonObject>();

                if (MapView.Active?.Map == null)
                    return (ids, false, (string)null, archivedList);

                var featureLayer = MapView.Active.Map
                    .GetLayersAsFlattenedList()
                    .OfType<FeatureLayer>()
                    .FirstOrDefault(l => l.Name.Equals(TargetLayerName, StringComparison.OrdinalIgnoreCase));

                if (featureLayer == null || featureLayer.ShapeType != esriGeometryType.esriGeometryPolyline)
                    return (ids, false, (string)null, archivedList);

                var filter = new SpatialQueryFilter
                {
                    FilterGeometry = geometry,
                    SpatialRelationship = SpatialRelationship.Intersects
                };

                // Threshold: features must be created within past 14 days
                var threshold = DateTime.UtcNow.AddDays(-14);
                bool hasRestrictedFeatures = false;
                string violationReason = null;

                using (RowCursor cursor = featureLayer.Search(filter))
                {
                    while (cursor.MoveNext())
                    {
                        using var feature = cursor.Current as Feature;
                        if (feature == null)
                            continue;

                        ids.Add(feature.GetObjectID());

                        // Snapshot feature to GeoJSON structure including the operator name
                        var geoJsonObject = CreateGeoJsonObject(feature, featureLayer.Name, currentOperator);
                        if (geoJsonObject != null)
                            archivedList.Add(geoJsonObject);

                        if (!hasRestrictedFeatures)
                        {
                            var createdDate = GetCreatedDate(feature);

                            if (!createdDate.HasValue)
                            {
                                hasRestrictedFeatures = true;
                                violationReason = $"Feature (OID: {feature.GetObjectID()}) has no valid creation timestamp.";
                            }
                            else
                            {
                                var createdUtc = createdDate.Value.ToUniversalTime();
                                if (createdUtc < threshold)
                                {
                                    hasRestrictedFeatures = true;
                                    violationReason = $"Feature (OID: {feature.GetObjectID()}) was created on {createdDate.Value.ToLocalTime():g}, which is older than 14 days.";
                                }
                            }
                        }
                    }
                }

                return (ids, hasRestrictedFeatures, violationReason, archivedList);
            });

            if (objectIds.Count == 0)
            {
                MessageBox.Show("No lines were found inside the selected area.", "Delete Lines");
                return false;
            }

            bool isPasscodeBypassUsed = false;

            // If features are older than 14 days, require passcode
            if (isRestricted)
            {
                if (!DeletionSessionManager.IsSessionActive())
                {
                    var promptResult = MessageBox.Show(
                        $"{restrictionMessage}\n\nStandard deletion is restricted to lines created within the past 14 days.\nDo you want to enter the supervisor passcode to override and authorize deletion?",
                        "Passcode Override Required",
                        System.Windows.MessageBoxButton.YesNo,
                        System.Windows.MessageBoxImage.Warning);

                    if (promptResult != System.Windows.MessageBoxResult.Yes)
                        return false;

                    if (!PromptPasscodeDialog())
                    {
                        MessageBox.Show("Incorrect passcode or operation cancelled. Deletion blocked.", "Access Denied", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                        return false;
                    }

                    DeletionSessionManager.StartSession(TimeSpan.FromHours(1));
                    isPasscodeBypassUsed = true;
                }
                else
                {
                    isPasscodeBypassUsed = true;
                }
            }

            var confirmResult = MessageBox.Show(
                $"Are you sure you want to delete {objectIds.Count} line(s)?",
                "Confirm Delete",
                System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Warning);

            if (confirmResult != System.Windows.MessageBoxResult.Yes)
                return false;

            bool deleted = await QueuedTask.Run(() =>
            {
                if (MapView.Active?.Map == null)
                    return false;

                var featureLayer = MapView.Active.Map
                    .GetLayersAsFlattenedList()
                    .OfType<FeatureLayer>()
                    .FirstOrDefault(l => l.Name.Equals(TargetLayerName, StringComparison.OrdinalIgnoreCase));

                if (featureLayer == null)
                {
                    MessageBox.Show($"Layer '{TargetLayerName}' was not found.", "Layer Missing");
                    return false;
                }

                var operation = new EditOperation
                {
                    Name = $"Delete lines from {TargetLayerName} by {currentOperator}"
                };

                operation.Delete(featureLayer, objectIds);

                bool ok = false;

                try
                {
                    DeletionController.IsAddInDeleting = true;
                    ok = operation.Execute();
                }
                finally
                {
                    DeletionController.IsAddInDeleting = false;
                }

                if (!ok)
                    MessageBox.Show(operation.ErrorMessage, "Delete Failed");

                return ok;
            });

            if (!deleted)
                return false;

            bool saved = await Project.Current.SaveEditsAsync();
            if (!saved)
            {
                MessageBox.Show("Lines were deleted, but saving edits failed.", "Save Failed");
                return false;
            }

            // Archive the deleted features to GeoJSON text file if passcode bypass was used
            if (isPasscodeBypassUsed && archiveFeatures.Count > 0)
            {
                try
                {
                    ArchiveFeaturesToGeoJsonFile(archiveFeatures);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Features were deleted, but archival failed: {ex.Message}", "Archive Warning");
                }
            }

            MessageBox.Show("Lines deleted successfully.", "Delete Complete");

            // Return to default Explore tool
            _ = FrameworkApplication.SetCurrentToolAsync("esri_mapping_exploreTool");

            return true;
        }

        /// <summary>
        /// Retrieves the local Windows PC operator account (Domain\Username or Machine\Username).
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
        /// Writes features into a JSON array in a .txt file next to the .aprx project path, maintaining at most 100 features.
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

            // Enforce the 100 limit: remove oldest features from index 0
            while (archiveArray.Count > MAX_ARCHIVE_RECORDS)
            {
                archiveArray.RemoveAt(0);
            }

            var options = new JsonSerializerOptions { WriteIndented = true };
            File.WriteAllText(filePath, archiveArray.ToJsonString(options));
        }

        /// <summary>
        /// Serializes the deleted feature into a GeoJSON Feature object with operator attribution.
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
    }

    internal static class DeletionSessionManager
    {
        private static DateTime? _sessionExpiryUtc;
        private static bool _isSubscribedToProjectClosing;

        public static bool IsSessionActive()
        {
            EnsureSubscribed();

            if (_sessionExpiryUtc.HasValue && DateTime.UtcNow < _sessionExpiryUtc.Value)
                return true;

            _sessionExpiryUtc = null;
            return false;
        }

        public static void StartSession(TimeSpan duration)
        {
            EnsureSubscribed();
            _sessionExpiryUtc = DateTime.UtcNow.Add(duration);
        }

        public static void InvalidateSession()
        {
            _sessionExpiryUtc = null;
        }

        private static void EnsureSubscribed()
        {
            if (_isSubscribedToProjectClosing)
                return;

            ProjectClosingEvent.Subscribe(args =>
            {
                InvalidateSession();
                return Task.CompletedTask;
            });

            _isSubscribedToProjectClosing = true;
        }
    }

    internal static class RemoveLineContext
    {
        public static string TargetLayerName { get; set; }
    }
}