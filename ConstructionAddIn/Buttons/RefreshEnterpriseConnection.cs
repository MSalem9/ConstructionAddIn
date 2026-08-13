using ArcGIS.Core.CIM;
using ArcGIS.Core.Data;

using ArcGIS.Desktop.Catalog;
using ArcGIS.Desktop.Core;
using ArcGIS.Desktop.Core.Geoprocessing;
using ArcGIS.Desktop.Framework.Contracts;
using ArcGIS.Desktop.Framework.Dialogs;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ConstructionAddIn.Buttons
{
    internal class RefreshEnterpriseConnection : Button
    {
        protected override async void OnClick()
        {
            // ============================================================
            // DATABASE SETTINGS
            // ============================================================

            const string SDE_FILE_NAME =
                "SQLServer-10-New_ConstructionMap_2026.sde";

            const string DATABASE_PLATFORM =
                "SQL_SERVER";

            // SQL Server IP / instance
            const string INSTANCE =
                @"10.1.70.172";

            // SQL Server database
            const string DATABASE =
                "New_ConstructionMap_2026";

            // ============================================================
            // AUTHENTICATION
            //
            // Windows / Operating System Authentication
            //
            // NO USERNAME
            // NO PASSWORD
            // ============================================================


            // ============================================================
            // CHECK PROJECT
            // ============================================================

            if (Project.Current == null)
            {
                MessageBox.Show(
                    "No ArcGIS Pro project is currently open.",
                    "Refresh Enterprise Database");

                return;
            }


            try
            {
                // ========================================================
                // NEW CONNECTION LOCATION
                //
                // The NEW .sde file will be created in the current
                // project's home folder.
                //
                // We DO NOT expect the old .sde file to be here.
                // ========================================================

                string sdeFolder =
                    Project.Current.HomeFolderPath;


                if (string.IsNullOrWhiteSpace(sdeFolder))
                {
                    MessageBox.Show(
                        "Could not determine the project's home folder.",
                        "Refresh Enterprise Database");

                    return;
                }


                string newSdePath =
                    Path.Combine(
                        sdeFolder,
                        SDE_FILE_NAME);


                // ========================================================
                // STEP 1
                //
                // FIND THE OLD DATABASE CONNECTION DIRECTLY FROM
                // ALL LAYERS / TABLES IN THE PROJECT.
                //
                // This means we don't care where the old .sde file is.
                // ========================================================

                HashSet<string> oldConnectionStrings =
                    await QueuedTask.Run(() =>
                    {
                        var connections =
                            new HashSet<string>(
                                StringComparer.OrdinalIgnoreCase);


                        var mapItems =
                            Project.Current
                                .GetItems<MapProjectItem>()
                                .ToList();


                        foreach (MapProjectItem mapItem in mapItems)
                        {
                            Map map = mapItem.GetMap();

                            if (map == null)
                                continue;


                            // Get every MapMember:
                            //
                            // layers
                            // standalone tables
                            // nested/group items
                            //
                            var mapMembers =
                                map.GetMapMembersAsFlattenedList();


                            foreach (MapMember mapMember in mapMembers)
                            {
                                try
                                {
                                    CIMDataConnection dataConnection =
                                        mapMember.GetDataConnection();


                                    if (dataConnection == null)
                                        continue;


                                    string workspaceConnectionString =
                                        GetWorkspaceConnectionString(
                                            dataConnection);


                                    if (string.IsNullOrWhiteSpace(
                                        workspaceConnectionString))
                                    {
                                        continue;
                                    }


                                    // We only want connections pointing to
                                    // our enterprise database.
                                    if (IsTargetDatabaseConnection(
                                        workspaceConnectionString,
                                        INSTANCE,
                                        DATABASE))
                                    {
                                        connections.Add(
                                            workspaceConnectionString);
                                    }
                                }
                                catch
                                {
                                    // Some MapMembers may not expose
                                    // a normal database data connection.
                                }
                            }
                        }


                        return connections;
                    });


                // ========================================================
                // CHECK THAT WE FOUND THE OLD CONNECTION
                // ========================================================

                if (oldConnectionStrings.Count == 0)
                {
                    MessageBox.Show(
                        "No layers or tables connected to the target " +
                        "enterprise database were found."
                        + Environment.NewLine
                        + Environment.NewLine
                        + $"Server: {INSTANCE}"
                        + Environment.NewLine
                        + $"Database: {DATABASE}",
                        "Refresh Enterprise Database");

                    return;
                }


                // ========================================================
                // STEP 2
                //
                // REMOVE OLD DATABASE CONNECTION ITEMS FROM THE PROJECT
                //
                // IMPORTANT:
                //
                // We remove the project connection only.
                //
                // We DO NOT delete the old physical .sde file because
                // that file may belong to another user/project.
                // ========================================================

                await QueuedTask.Run(() =>
                {
                    var connections =
                        Project.Current
                            .GetItems<GDBProjectItem>()
                            .ToList();


                    foreach (GDBProjectItem connection in connections)
                    {
                        try
                        {
                            string path = connection.Path;

                            if (string.IsNullOrWhiteSpace(path))
                                continue;


                            string fileName =
                                Path.GetFileName(path);


                            // Remove our existing connection item
                            if (string.Equals(
                                fileName,
                                SDE_FILE_NAME,
                                StringComparison.OrdinalIgnoreCase))
                            {
                                Project.Current.RemoveItem(
                                    connection);
                            }
                        }
                        catch
                        {
                            // Ignore unrelated project items
                        }
                    }
                });


                // ========================================================
                // STEP 3
                //
                // MAKE SURE PROJECT HOME FOLDER EXISTS
                // ========================================================

                if (!Directory.Exists(sdeFolder))
                {
                    Directory.CreateDirectory(sdeFolder);
                }


                // ========================================================
                // STEP 4
                //
                // DELETE ONLY THE NEW LOCAL CONNECTION FILE IF IT
                // ALREADY EXISTS.
                //
                // We are NOT deleting the old connection somewhere
                // under another user's profile.
                // ========================================================

                if (File.Exists(newSdePath))
                {
                    File.Delete(newSdePath);
                }


                // ========================================================
                // STEP 5
                //
                // CREATE NEW ENTERPRISE DATABASE CONNECTION
                //
                // WINDOWS AUTHENTICATION
                // ========================================================

                var parameters =
                    Geoprocessing.MakeValueArray(

                        // Output folder
                        sdeFolder,

                        // Connection file
                        SDE_FILE_NAME,

                        // Database platform
                        DATABASE_PLATFORM,

                        // SQL Server
                        INSTANCE,

                        // Windows Authentication
                        "OPERATING_SYSTEM_AUTH",

                        // Username - not required
                        "#",

                        // Password - not required
                        "#",

                        // Save username/password
                        "DO_NOT_SAVE_USERNAME",

                        // Database
                        DATABASE,

                        // Schema
                        "#",

                        // Version type
                        "#",

                        // Version
                        "#"
                    );


                var gpResult =
                    await Geoprocessing.ExecuteToolAsync(
                        "management.CreateDatabaseConnection",
                        parameters,
                        null,
                        null,
                        null,
                        GPExecuteToolFlags.GPThread |
                        GPExecuteToolFlags.RefreshProjectItems);


                // ========================================================
                // CHECK CREATE CONNECTION RESULT
                // ========================================================

                if (gpResult.IsFailed)
                {
                    string errors =
                        string.Join(
                            Environment.NewLine,
                            gpResult.Messages
                                .Select(m => m.Text));


                    MessageBox.Show(
                        "Could not create the new enterprise " +
                        "database connection."
                        + Environment.NewLine
                        + Environment.NewLine
                        + errors,
                        "Refresh Enterprise Database");

                    return;
                }


                // ========================================================
                // STEP 6
                //
                // VERIFY NEW .SDE FILE EXISTS
                // ========================================================

                if (!File.Exists(newSdePath))
                {
                    MessageBox.Show(
                        "ArcGIS reported that the database connection " +
                        "was created, but the .sde file could not be found."
                        + Environment.NewLine
                        + Environment.NewLine
                        + newSdePath,
                        "Refresh Enterprise Database");

                    return;
                }


                // ========================================================
                // STEP 7
                //
                // TEST NEW CONNECTION
                // ========================================================

                string newConnectionString =
                    await QueuedTask.Run(() =>
                    {
                        var connectionFile =
                            new DatabaseConnectionFile(
                                new Uri(newSdePath));


                        using (Geodatabase geodatabase =
                               new Geodatabase(connectionFile))
                        {
                            string connectionString =
                                geodatabase.GetConnectionString();


                            if (string.IsNullOrWhiteSpace(
                                connectionString))
                            {
                                throw new Exception(
                                    "The new enterprise database " +
                                    "connection could not be opened.");
                            }


                            return connectionString;
                        }
                    });


                // ========================================================
                // STEP 8
                //
                // ADD THE NEW .SDE CONNECTION TO THE PROJECT
                // ========================================================

                await QueuedTask.Run(() =>
                {
                    bool alreadyAdded =
                        Project.Current
                            .GetItems<GDBProjectItem>()
                            .Any(item =>
                                string.Equals(
                                    item.Path,
                                    newSdePath,
                                    StringComparison.OrdinalIgnoreCase));


                    if (!alreadyAdded)
                    {
                        var newConnectionItem =
                            ItemFactory.Instance.Create(
                                newSdePath)
                            as IProjectItem;


                        if (newConnectionItem == null)
                        {
                            throw new Exception(
                                "The new .sde connection could not " +
                                "be converted to a project item.");
                        }


                        Project.Current.AddItem(
                            newConnectionItem);
                    }
                });


                // ========================================================
                // STEP 9
                //
                // UPDATE EVERY MAP IN THE PROJECT
                //
                // Replace every OLD connection string that pointed
                // to the target enterprise database.
                // ========================================================

                int mapCount = 0;
                int connectionCount =
                    oldConnectionStrings.Count;


                await QueuedTask.Run(() =>
                {
                    var mapItems =
                        Project.Current
                            .GetItems<MapProjectItem>()
                            .ToList();


                    foreach (MapProjectItem mapItem in mapItems)
                    {
                        Map map = mapItem.GetMap();

                        if (map == null)
                            continue;


                        // There may be more than one old connection
                        // string, for example different versions.
                        foreach (string oldConnectionString
                                 in oldConnectionStrings)
                        {
                            map.FindAndReplaceWorkspacePath(
                                oldConnectionString,
                                newSdePath,
                                true);
                        }


                        // =================================================
                        // CLEAR DISPLAY CACHE
                        // =================================================

                        foreach (Layer layer in
                                 map.GetLayersAsFlattenedList())
                        {
                            try
                            {
                                layer.ClearDisplayCache();
                            }
                            catch
                            {
                                // Some layer types do not support it
                            }
                        }


                        mapCount++;
                    }
                });


                // ========================================================
                // STEP 10
                //
                // REDRAW ACTIVE MAP AND CLEAR FEATURE CACHE
                // ========================================================

                if (MapView.Active != null)
                {
                    await MapView.Active.RedrawAsync(true);
                }


                // ========================================================
                // SUCCESS
                // ========================================================

                MessageBox.Show(
                    "Enterprise database connection refreshed successfully."
                    + Environment.NewLine
                    + Environment.NewLine
                    + $"Server: {INSTANCE}"
                    + Environment.NewLine
                    + $"Database: {DATABASE}"
                    + Environment.NewLine
                    + $"Authentication: Windows"
                    + Environment.NewLine
                    + Environment.NewLine
                    + $"New connection:"
                    + Environment.NewLine
                    + newSdePath
                    + Environment.NewLine
                    + Environment.NewLine
                    + $"Old workspace connections replaced: {connectionCount}"
                    + Environment.NewLine
                    + $"Maps processed: {mapCount}",
                    "Refresh Enterprise Database");
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Could not refresh the enterprise database connection."
                    + Environment.NewLine
                    + Environment.NewLine
                    + ex.Message,
                    "Refresh Enterprise Database");
            }
        }


        // ================================================================
        // HELPER:
        //
        // Extract the workspace connection string from the different
        // CIM database connection types.
        // ================================================================

        private static string GetWorkspaceConnectionString(
            CIMDataConnection dataConnection)
        {
            if (dataConnection is CIMStandardDataConnection standard)
            {
                return standard.WorkspaceConnectionString;
            }


            if (dataConnection is CIMSqlQueryDataConnection sqlQuery)
            {
                return sqlQuery.WorkspaceConnectionString;
            }


            return null;
        }


        // ================================================================
        // HELPER:
        //
        // Check if this connection belongs to our SQL Server/database.
        // ================================================================

        private static bool IsTargetDatabaseConnection(
            string connectionString,
            string instance,
            string database)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
                return false;


            bool databaseMatches =
                connectionString.IndexOf(
                    database,
                    StringComparison.OrdinalIgnoreCase) >= 0;


            // Database name is the main identifier.
            //
            // We don't REQUIRE instance match because ArcGIS connection
            // strings may represent SQL Server instance information in
            // different formats.
            return databaseMatches;
        }
    }
}