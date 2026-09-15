using ArcGIS.Core.Data;
using ArcGIS.Desktop.Core.Events;
using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Dialogs;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
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

namespace ConstructionAddIn.Services
{
    public class DeletionValidationResult
    {
        public bool IsAuthorized { get; set; }
        public bool BypassUsed { get; set; }
        public List<JsonObject> GeoJsonSnapshots { get; set; } = new List<JsonObject>();
    }

    public static class FeatureDeletionGuardService
    {
        private const string ADMIN_SECRET_CODE = "Admin@2026";
        private const int MAX_ARCHIVE_RECORDS = 100;
        private const string ARCHIVE_FOLDER_NAME = "Archive";
        private const string ARCHIVE_FILENAME = "DeletedFeaturesArchive.txt";

        private static DateTime? _sessionExpiryUtc;
        private static bool _isSubscribedToProjectClosing;

        /// <summary>
        /// Evaluates target features safely inside QueuedTask and returns plain JSON representations.
        /// </summary>
        public static async Task<DeletionValidationResult> EvaluateAndAuthorizeByLayerAsync(
            string layerName,
            IReadOnlyList<long> objectIds,
            int maxDaysAllowed = 14)
        {
            if (objectIds == null || objectIds.Count == 0)
            {
                return new DeletionValidationResult { IsAuthorized = false };
            }

            string operatorName = GetCurrentOperatorName();

            // Run pure Geodatabase reading strictly inside QueuedTask
            var (hasViolations, violationMessage, snapshots) = await QueuedTask.Run(() =>
            {
                var list = new List<JsonObject>();
                bool restricted = false;
                string reason = null;
                var threshold = DateTime.UtcNow.AddDays(-maxDaysAllowed);

                var map = MapView.Active?.Map;
                if (map == null)
                    return (false, null, list);

                var targetLayer = map.GetLayersAsFlattenedList()
                    .OfType<FeatureLayer>()
                    .FirstOrDefault(l => l.Name.Equals(layerName, StringComparison.OrdinalIgnoreCase));

                if (targetLayer == null)
                    return (false, null, list);

                var filter = new QueryFilter
                {
                    ObjectIDs = objectIds.ToList()
                };

                using var cursor = targetLayer.Search(filter);
                while (cursor.MoveNext())
                {
                    using var feature = cursor.Current as Feature;
                    if (feature == null)
                        continue;

                    var snap = CreateGeoJsonObject(feature, layerName, operatorName);
                    if (snap != null)
                        list.Add(snap);

                    if (!restricted)
                    {
                        var createdDate = GetCreatedDate(feature);
                        if (!createdDate.HasValue)
                        {
                            restricted = true;
                            reason = $"Feature (OID: {feature.GetObjectID()}) has no valid creation timestamp.";
                        }
                        else if (createdDate.Value.ToUniversalTime() < threshold)
                        {
                            restricted = true;
                            reason = $"Feature (OID: {feature.GetObjectID()}) was created on {createdDate.Value.ToLocalTime():g}, which is older than {maxDaysAllowed} days.";
                        }
                    }
                }

                return (restricted, reason, list);
            });

            if (!hasViolations)
            {
                return new DeletionValidationResult
                {
                    IsAuthorized = true,
                    BypassUsed = false,
                    GeoJsonSnapshots = snapshots
                };
            }

            if (IsSessionActive())
            {
                return new DeletionValidationResult
                {
                    IsAuthorized = true,
                    BypassUsed = true,
                    GeoJsonSnapshots = snapshots
                };
            }

            // Dialog runs on UI thread OUTSIDE QueuedTask
            var promptResult = MessageBox.Show(
                $"{violationMessage}\n\nStandard deletion is restricted to features created within the past {maxDaysAllowed} days.\nDo you want to enter the supervisor passcode to authorize deletion for 1 hour?",
                "Passcode Override Required",
                System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Warning);

            if (promptResult != System.Windows.MessageBoxResult.Yes)
                return new DeletionValidationResult { IsAuthorized = false };

            if (!PromptPasscodeDialog())
            {
                MessageBox.Show("Incorrect passcode or operation cancelled. Deletion blocked.", "Access Denied", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                return new DeletionValidationResult { IsAuthorized = false };
            }

            StartSession(TimeSpan.FromHours(1));

            return new DeletionValidationResult
            {
                IsAuthorized = true,
                BypassUsed = true,
                GeoJsonSnapshots = snapshots
            };
        }

        public static void ArchiveFeatures(List<JsonObject> snapshots)
        {
            if (snapshots == null || snapshots.Count == 0)
                return;

            string documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            string arcGisProFolder = Path.Combine(documentsPath, "ArcGIS");
            string archiveDirectory = Path.Combine(arcGisProFolder, ARCHIVE_FOLDER_NAME);

            if (!Directory.Exists(archiveDirectory))
                Directory.CreateDirectory(archiveDirectory);

            string filePath = Path.Combine(archiveDirectory, ARCHIVE_FILENAME);
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
                            archiveArray = jArray;
                    }
                }
                catch
                {
                    archiveArray = new JsonArray();
                }
            }

            foreach (var item in snapshots)
                archiveArray.Add(item);

            while (archiveArray.Count > MAX_ARCHIVE_RECORDS)
                archiveArray.RemoveAt(0);

            var options = new JsonSerializerOptions { WriteIndented = true };
            File.WriteAllText(filePath, archiveArray.ToJsonString(options));
        }

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

            var mainGrid = new Grid { Margin = new Thickness(20) };
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
                    MessageBox.Show("The passcode you entered is incorrect.", "Invalid Passcode", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
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

        public static string GetCurrentOperatorName()
        {
            try
            {
                string domain = Environment.UserDomainName;
                string user = Environment.UserName;

                if (!string.IsNullOrWhiteSpace(domain) && !string.IsNullOrWhiteSpace(user))
                    return $"{domain}\\{user}";

                return user ?? Environment.MachineName;
            }
            catch
            {
                return Environment.UserName;
            }
        }

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
                featureObj["geometry_esri"] = JsonNode.Parse(shape.ToJson());

                return featureObj;
            }
            catch
            {
                return null;
            }
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
                int index = row.FindField(fieldName);
                if (index != -1)
                {
                    var val = row[index];
                    if (val is DateTime dt)
                        return dt;
                    if (val is DateTimeOffset dto)
                        return dto.UtcDateTime;
                    if (val != null && val != DBNull.Value && DateTime.TryParse(val.ToString(), out var parsed))
                        return parsed;
                }
            }

            for (int i = 0; i < fields.Count; i++)
            {
                var f = fields[i];
                string nameLower = f.Name.ToLowerInvariant();
                if (nameLower.Contains("create") && (f.FieldType == FieldType.Date || nameLower.Contains("date") || nameLower.Contains("time")))
                {
                    var val = row[i];
                    if (val is DateTime d)
                        return d;
                    if (val is DateTimeOffset dto)
                        return dto.UtcDateTime;
                    if (val != null && val != DBNull.Value && DateTime.TryParse(val.ToString(), out var parsed))
                        return parsed;
                }
            }

            return null;
        }
    }
}