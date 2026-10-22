using ArcGIS.Desktop.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace ConstructionAddIn.Services.EnterpriseConnection
{
    /// <summary>
    /// Reads the shared connection settings file maintained by the GIS admin.
    ///
    /// Users never type the server address. The add-in first uses the server the
    /// layers already point to; this file is only the fallback for when the server
    /// address has changed. The admin edits ONE file and every user picks it up
    /// the next time they press "Refresh Enterprise Connection".
    ///
    /// File content (JSON):
    /// {
    ///   "Instance": "10.1.70.172",
    ///   "Database": "New_ConstructionMap_2026"
    /// }
    ///
    /// Where the file is searched, in order:
    /// 1. The path in the CONSTRUCTION_ADDIN_SETTINGS environment variable
    ///    (IT can set this for every user with Group Policy).
    /// 2. SharedSettingsPath below (a network share).
    /// 3. "ConstructionAddIn.connection.json" in the ArcGIS Pro project's home folder.
    /// </summary>
    public static class EnterpriseConnectionSettings
    {
        public const string EnvironmentVariableName = "CONSTRUCTION_ADDIN_SETTINGS";

        public const string ProjectSettingsFileName = "ConstructionAddIn.connection.json";

        /// <summary>
        /// Network location of the shared settings file.
        /// Set this ONCE to a share every designer can read, e.g.
        /// @"\\fileserver\GIS\ConstructionAddIn\ConstructionAddIn.connection.json".
        /// Leave empty to rely on the environment variable / project folder only.
        /// </summary>
        public const string SharedSettingsPath = "";

        /// <summary>
        /// Returns the settings from the first file found, or null if none exists or it is invalid.
        /// Touches the file system (possibly a network share): do not call on the UI thread.
        /// </summary>
        public static EnterpriseConnectionInfo Load(out string loadedFrom)
        {
            loadedFrom = null;

            foreach (var path in GetCandidatePaths())
            {
                try
                {
                    if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
                        continue;

                    var info = JsonSerializer.Deserialize<EnterpriseConnectionInfo>(
                        File.ReadAllText(path),
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    if (info == null ||
                        string.IsNullOrWhiteSpace(info.Instance) ||
                        string.IsNullOrWhiteSpace(info.Database))
                        continue;

                    loadedFrom = path;
                    return info;
                }
                catch
                {
                    // Unreadable / malformed file: try the next location.
                }
            }

            return null;
        }

        private static IEnumerable<string> GetCandidatePaths()
        {
            yield return Environment.GetEnvironmentVariable(EnvironmentVariableName);

            yield return SharedSettingsPath;

            string homeFolder = Project.Current?.HomeFolderPath;
            if (!string.IsNullOrWhiteSpace(homeFolder))
                yield return Path.Combine(homeFolder, ProjectSettingsFileName);
        }
    }
}
