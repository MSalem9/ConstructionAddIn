using System;
using System.Collections.Generic;

namespace ConstructionAddIn.Services.EnterpriseConnection
{
    /// <summary>
    /// Server + database of an enterprise geodatabase (SQL Server, Windows authentication).
    ///
    /// Built from the workspace connection string that every enterprise layer keeps in
    /// its CIM definition, so nothing has to be hard-coded or typed by the user.
    ///
    /// Example workspace connection string:
    /// SERVER=...;INSTANCE=sde:sqlserver:10.1.70.172;DBCLIENT=sqlserver;
    /// DB_CONNECTION_PROPERTIES=10.1.70.172;DATABASE=New_ConstructionMap_2026;AUTHENTICATION_MODE=OSA
    /// </summary>
    public sealed class EnterpriseConnectionInfo
    {
        /// <summary>SQL Server address, e.g. "10.1.70.172", "server\instance" or "server,1500".</summary>
        public string Instance { get; set; }

        /// <summary>SQL Server database name.</summary>
        public string Database { get; set; }

        public override string ToString() => $"{Instance} / {Database}";

        /// <summary>
        /// Parses an ArcGIS workspace connection string.
        /// Returns null when the string is not an enterprise (SDE) connection.
        /// </summary>
        public static EnterpriseConnectionInfo FromWorkspaceConnectionString(string connectionString)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
                return null;

            var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            foreach (var part in connectionString.Split(';'))
            {
                int index = part.IndexOf('=');
                if (index <= 0)
                    continue;

                values[part.Substring(0, index).Trim()] = part.Substring(index + 1).Trim();
            }

            // File geodatabases / shapefiles only have DATABASE=<path>, no DBCLIENT.
            if (!values.ContainsKey("DBCLIENT") && !values.ContainsKey("DB_CONNECTION_PROPERTIES"))
                return null;

            values.TryGetValue("DATABASE", out var database);

            values.TryGetValue("DB_CONNECTION_PROPERTIES", out var instance);
            if (string.IsNullOrWhiteSpace(instance) && values.TryGetValue("INSTANCE", out var sdeInstance))
            {
                // "sde:sqlserver:10.1.70.172" -> "10.1.70.172"
                int lastColon = sdeInstance.LastIndexOf(':');
                instance = lastColon >= 0 ? sdeInstance.Substring(lastColon + 1) : sdeInstance;
            }

            if (string.IsNullOrWhiteSpace(instance) || string.IsNullOrWhiteSpace(database))
                return null;

            return new EnterpriseConnectionInfo
            {
                Instance = instance,
                Database = database
            };
        }

        public bool SameDatabase(EnterpriseConnectionInfo other) =>
            other != null &&
            string.Equals(Database?.Trim(), other.Database?.Trim(), StringComparison.OrdinalIgnoreCase);

        public bool SameServerAndDatabase(EnterpriseConnectionInfo other) =>
            SameDatabase(other) &&
            string.Equals(Instance?.Trim(), other.Instance?.Trim(), StringComparison.OrdinalIgnoreCase);
    }
}
