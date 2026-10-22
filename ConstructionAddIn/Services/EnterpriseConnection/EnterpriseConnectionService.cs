using ArcGIS.Core.CIM;
using ArcGIS.Core.Data;
using ArcGIS.Desktop.Catalog;
using ArcGIS.Desktop.Core;
using ArcGIS.Desktop.Core.Geoprocessing;
using ArcGIS.Desktop.Framework.Dialogs;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using ConstructionAddIn.Dialogs.ReconnectProgress;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using MessageBox = ArcGIS.Desktop.Framework.Dialogs.MessageBox;

namespace ConstructionAddIn.Services.EnterpriseConnection
{
    /// <summary>
    /// Finds, tests and refreshes the enterprise geodatabase connection used by the
    /// project, and checks that every layer in the Contents pane uses that same source.
    ///
    /// The server address is never hard-coded:
    /// 1. it is read from the layers already in the project, and
    /// 2. if that server cannot be reached, from the shared settings file
    ///    (see <see cref="EnterpriseConnectionSettings"/>), maintained by the GIS admin.
    /// </summary>
    public static class EnterpriseConnectionService
    {
        private const string Title = "Refresh Enterprise Connection";

        /// <summary>
        /// Layers the Construction tools write to. If any of these is in the map but does not
        /// point to the enterprise database, editing stays disabled after reconnecting.
        /// </summary>
        private static readonly HashSet<string> ConstructionLayerNames = new(StringComparer.OrdinalIgnoreCase)
        {
            "pipes",
            "CrossingNew",
            "HotTap",
            "reducer",
            "hpValve_Pecat",
            "hpValve_Pecat_Reg",
            "hpValve_Pecat_1Vent",
            "hpValve_Pecat_2Vent",
            "hpBallValve_Pecat_1Vent"
        };

        private static bool _isReconnecting;

        #region Layer sources

        /// <summary>One layer / standalone table and the data source it points to.</summary>
        public sealed class LayerSource
        {
            public string MapName { get; set; }
            public string Name { get; set; }
            public string ConnectionString { get; set; }

            /// <summary>Null when the layer is not in an enterprise geodatabase (file gdb, shapefile, service...).</summary>
            public EnterpriseConnectionInfo Connection { get; set; }

            public bool IsBroken { get; set; }
        }

        public enum SourceProblem
        {
            None,
            Broken,
            OldServer,
            DifferentDatabase,
            NotEnterprise
        }

        /// <summary>
        /// Reads the data source of every operational layer and standalone table in every map.
        /// Works even while the database is down, because it only reads the CIM definitions.
        /// Must be called on the MCT.
        /// </summary>
        public static List<LayerSource> ScanLayerSources()
        {
            var result = new List<LayerSource>();

            foreach (var mapItem in Project.Current.GetItems<MapProjectItem>())
            {
                Map map;
                try
                {
                    map = mapItem.GetMap();
                }
                catch
                {
                    continue;
                }

                if (map == null)
                    continue;

                var members = map.GetLayersAsFlattenedList()
                    .OfType<FeatureLayer>()
                    .Where(layer => layer.MapLayerType == MapLayerType.Operational)
                    .Cast<MapMember>()
                    .Concat(map.GetStandaloneTablesAsFlattenedList());

                foreach (var member in members)
                {
                    try
                    {
                        string connectionString = member.GetDataConnection() switch
                        {
                            CIMStandardDataConnection standard => standard.WorkspaceConnectionString,
                            CIMSqlQueryDataConnection sqlQuery => sqlQuery.WorkspaceConnectionString,
                            _ => null
                        };

                        result.Add(new LayerSource
                        {
                            MapName = map.Name,
                            Name = member.Name,
                            ConnectionString = connectionString,
                            Connection = EnterpriseConnectionInfo.FromWorkspaceConnectionString(connectionString),
                            IsBroken = member.ConnectionStatus == ConnectionStatus.Broken
                        });
                    }
                    catch
                    {
                        // Member without a readable data connection: ignore.
                    }
                }
            }

            return result;
        }

        public static SourceProblem GetProblem(LayerSource source, EnterpriseConnectionInfo target)
        {
            if (source.IsBroken)
                return SourceProblem.Broken;

            if (source.Connection == null)
                return SourceProblem.NotEnterprise;

            if (!source.Connection.SameDatabase(target))
                return SourceProblem.DifferentDatabase;

            if (!source.Connection.SameServerAndDatabase(target))
                return SourceProblem.OldServer;

            return SourceProblem.None;
        }

        /// <summary>
        /// The enterprise database the project works with: the database named in the shared
        /// settings file if there is one, otherwise the one used by most layers.
        /// The server is the one the layers use for that database.
        /// Returns null when no layer uses an enterprise database.
        /// </summary>
        public static EnterpriseConnectionInfo PickProjectConnection(
            List<LayerSource> sources,
            EnterpriseConnectionInfo settings)
        {
            var enterprise = sources.Where(s => s.Connection != null).ToList();

            string database = settings?.Database ??
                enterprise
                    .GroupBy(s => s.Connection.Database, StringComparer.OrdinalIgnoreCase)
                    .OrderByDescending(g => g.Count())
                    .Select(g => g.Key)
                    .FirstOrDefault();

            if (database == null)
                return null;

            string instance = enterprise
                .Where(s => string.Equals(s.Connection.Database, database, StringComparison.OrdinalIgnoreCase))
                .GroupBy(s => s.Connection.Instance, StringComparer.OrdinalIgnoreCase)
                .OrderByDescending(g => g.Count())
                .Select(g => g.Key)
                .FirstOrDefault();

            if (instance == null)
                return settings;

            return new EnterpriseConnectionInfo { Instance = instance, Database = database };
        }

        /// <summary>Finds the project's enterprise connection (used by the background monitor).</summary>
        public static async Task<EnterpriseConnectionInfo> DiscoverProjectConnectionAsync()
        {
            if (Project.Current == null)
                return null;

            var settings = await Task.Run(() => EnterpriseConnectionSettings.Load(out _));
            var sources = await QueuedTask.Run(ScanLayerSources);

            return PickProjectConnection(sources, settings);
        }

        #endregion

        #region Reconnect

        private static ReconnectProgressWindow _progressWindow;

        /// <summary>
        /// Full reconnect workflow used by the ribbon button and the dockpane banner:
        /// find a reachable server, create a fresh .sde connection file, repoint every layer
        /// to it, verify all layer sources, and re-enable editing if everything is correct.
        ///
        /// Every step is shown live in a progress window, so the user sees what is happening.
        /// </summary>
        public static async Task ReconnectAsync()
        {
            // Already running: just bring its window to the front.
            if (_isReconnecting)
            {
                _progressWindow?.Activate();
                return;
            }

            if (Project.Current == null)
            {
                MessageBox.Show("No ArcGIS Pro project is currently open.", Title);
                return;
            }

            _isReconnecting = true;

            var progress = new ReconnectProgressViewModel();
            _progressWindow = new ReconnectProgressWindow(progress);
            _progressWindow.Show();

            try
            {
                await ReconnectCoreAsync(progress);
            }
            catch (Exception ex)
            {
                progress.CurrentStep?.Fail(ex.Message);
                progress.Finish(false, "Could not refresh the enterprise database connection. Editing stays disabled.");
            }
            finally
            {
                _isReconnecting = false;
                _progressWindow = null;
            }
        }

        private static async Task ReconnectCoreAsync(ReconnectProgressViewModel progress)
        {
            // --------------------------------------------------------
            // 1. Unsaved edits must be saved (or discarded) before the
            //    layers can be moved to a new connection.
            // --------------------------------------------------------
            if (Project.Current.HasEdits)
            {
                var editsStep = progress.Begin("Saving unsaved edits");
                if (!await HandlePendingEditsAsync())
                {
                    editsStep.Fail("You chose to keep the unsaved edits.");
                    progress.Finish(false, "Reconnect cancelled. Editing stays disabled.");
                    return;
                }

                editsStep.Done();
            }

            // --------------------------------------------------------
            // 2. Which database / server? From the layers first, then
            //    from the admin's shared settings file.
            // --------------------------------------------------------
            var readStep = progress.Begin("Reading the data sources of the layers");

            string settingsPath = null;
            var settings = await Task.Run(() => EnterpriseConnectionSettings.Load(out settingsPath));
            var sources = await QueuedTask.Run(ScanLayerSources);
            var fromLayers = PickProjectConnection(sources, settings);

            int enterpriseCount = sources.Count(s => s.Connection != null);
            readStep.Done(
                $"{sources.Count} layers / tables, {enterpriseCount} from an enterprise database. " +
                (settings != null
                    ? $"Settings file: {settingsPath}"
                    : "No shared settings file found."));

            var candidates = new List<(EnterpriseConnectionInfo Info, string Origin)>();
            if (fromLayers != null)
                candidates.Add((fromLayers, "from the layers"));
            if (settings != null && !candidates.Any(c => c.Info.SameServerAndDatabase(settings)))
                candidates.Add((settings, "from the settings file"));

            if (candidates.Count == 0)
            {
                readStep.Fail(
                    "No layer uses an enterprise database and no shared settings file was found.");
                progress.Finish(false, "Nothing to connect to. Please contact the GIS administrator.");
                return;
            }

            // --------------------------------------------------------
            // 3. Use the first candidate that really opens.
            // --------------------------------------------------------
            EnterpriseConnectionInfo target = null;

            foreach (var (candidate, origin) in candidates)
            {
                var testStep = progress.Begin($"Connecting to server {candidate.Instance} ({origin})");

                string error = await TestConnectionAsync(candidate);
                if (error == null)
                {
                    testStep.Done($"Database {candidate.Database}: connected.");
                    target = candidate;
                    break;
                }

                testStep.Fail(error);
            }

            if (target == null)
            {
                progress.Finish(
                    false,
                    "The enterprise database could not be reached. Editing stays disabled. " +
                    "Check your network connection and try again. If the server address has changed, " +
                    "ask the GIS administrator to update the shared connection settings file.");
                return;
            }

            // --------------------------------------------------------
            // 4. Create a fresh .sde file in the project home folder.
            // --------------------------------------------------------
            var fileStep = progress.Begin("Creating the connection file");
            string sdePath = await CreateConnectionFileAsync(target);
            fileStep.Done(sdePath);

            // --------------------------------------------------------
            // 5. Repoint every layer / table of this database to it.
            // --------------------------------------------------------
            var repointStep = progress.Begin("Updating the data source of the layers");

            var oldConnectionStrings = sources
                .Where(s => s.Connection != null && s.Connection.SameDatabase(target))
                .Select(s => s.ConnectionString)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            int mapCount = await QueuedTask.Run(() =>
            {
                int count = 0;

                foreach (var mapItem in Project.Current.GetItems<MapProjectItem>())
                {
                    var map = mapItem.GetMap();
                    if (map == null)
                        continue;

                    foreach (var oldConnectionString in oldConnectionStrings)
                        map.FindAndReplaceWorkspacePath(oldConnectionString, sdePath, true);

                    foreach (var layer in map.GetLayersAsFlattenedList())
                    {
                        try
                        {
                            layer.ClearDisplayCache();
                        }
                        catch
                        {
                            // Some layer types do not support it.
                        }
                    }

                    count++;
                }

                return count;
            });

            if (MapView.Active != null)
                await MapView.Active.RedrawAsync(true);

            repointStep.Done($"{mapCount} map(s) updated.");

            // --------------------------------------------------------
            // 6. Verify that the layers now use this database.
            // --------------------------------------------------------
            var checkStep = progress.Begin("Checking that all layers use the enterprise database");

            var afterSources = await QueuedTask.Run(ScanLayerSources);
            var problems = afterSources
                .Select(s => (Source: s, Problem: GetProblem(s, target)))
                .Where(x => x.Problem != SourceProblem.None)
                .ToList();

            int okCount = afterSources.Count - problems.Count;
            string checkDetail = $"{okCount} of {afterSources.Count} layers / tables use {target.Database}.";

            if (problems.Count == 0)
                checkStep.Done(checkDetail);
            else
                checkStep.Warn(checkDetail);

            // --------------------------------------------------------
            // 7. Enable editing only if the Construction layers are OK.
            // --------------------------------------------------------
            var problemLines = problems
                .Select(p => $"{p.Source.MapName} → {p.Source.Name}: {Describe(p.Problem, p.Source)}")
                .ToList();

            var editStep = progress.Begin("Enabling editing");
            bool constructionLayersOk = !problems.Any(p => ConstructionLayerNames.Contains(p.Source.Name));

            if (constructionLayersOk)
            {
                await EnterpriseConnectionMonitor.MarkConnectedAsync(target);
                editStep.Done();

                progress.Finish(
                    true,
                    $"Connected to {target.Database} on {target.Instance}. Editing is enabled.",
                    problemLines);
            }
            else
            {
                await EnterpriseConnectionMonitor.BlockEditingAsync(
                    "Some Construction layers do not point to the enterprise database.");
                editStep.Fail("Some Construction layers do not point to the enterprise database (see the list below).");

                progress.Finish(
                    false,
                    "Connected, but editing stays DISABLED because some Construction layers " +
                    "do not point to the enterprise database.",
                    problemLines);
            }
        }

        private static string Describe(SourceProblem problem, LayerSource source) => problem switch
        {
            SourceProblem.Broken => "data source is broken (red exclamation mark)",
            SourceProblem.OldServer => $"still points to old server {source.Connection?.Instance}",
            SourceProblem.DifferentDatabase => $"points to another database ({source.Connection})",
            SourceProblem.NotEnterprise => "not in the enterprise database (file geodatabase, shapefile or service)",
            _ => ""
        };

        /// <summary>
        /// Saves pending edits. If saving fails, asks whether to discard them.
        /// Returns false if the user chooses to keep them (reconnect is cancelled).
        /// </summary>
        private static async Task<bool> HandlePendingEditsAsync()
        {
            if (!Project.Current.HasEdits)
                return true;

            try
            {
                if (await Project.Current.SaveEditsAsync())
                    return true;
            }
            catch
            {
                // Saving fails while the database is down: handled below.
            }

            var answer = MessageBox.Show(
                "There are unsaved edits that could not be saved because the database is not reachable." +
                Environment.NewLine + Environment.NewLine +
                "To reconnect, these edits must be discarded. Discard them and reconnect?",
                Title,
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (answer != MessageBoxResult.Yes)
                return false;

            await Project.Current.DiscardEditsAsync();
            return true;
        }

        /// <summary>
        /// Opens the geodatabase with Windows authentication.
        /// Returns null on success, otherwise the error message.
        /// </summary>
        public static async Task<string> TestConnectionAsync(EnterpriseConnectionInfo info)
        {
            // Quick network check first, so an unreachable server does not freeze the MCT.
            if (!await SqlServerProbe.IsReachableAsync(info.Instance))
                return "server is not reachable on the network";

            return await QueuedTask.Run(() =>
            {
                try
                {
                    using var geodatabase = new Geodatabase(ToConnectionProperties(info));
                    return (string)null;
                }
                catch (Exception ex)
                {
                    return ex.Message;
                }
            });
        }

        public static DatabaseConnectionProperties ToConnectionProperties(EnterpriseConnectionInfo info) =>
            new DatabaseConnectionProperties(EnterpriseDatabaseType.SQLServer)
            {
                AuthenticationMode = AuthenticationMode.OSA,
                Instance = info.Instance,
                Database = info.Database
            };

        /// <summary>
        /// Creates "&lt;Database&gt;.sde" in the project home folder (Windows authentication),
        /// replacing an earlier one, and adds it to the project. Returns its path; throws on failure.
        /// </summary>
        private static async Task<string> CreateConnectionFileAsync(EnterpriseConnectionInfo target)
        {
            string folder = Project.Current.HomeFolderPath;
            if (string.IsNullOrWhiteSpace(folder))
                throw new InvalidOperationException("Could not determine the project's home folder.");

            string fileName =
                string.Concat(target.Database.Select(c => Path.GetInvalidFileNameChars().Contains(c) ? '_' : c)) + ".sde";
            string sdePath = Path.Combine(folder, fileName);

            // Remove our previous project item, then the previous file.
            await QueuedTask.Run(() =>
            {
                foreach (var item in Project.Current.GetItems<GDBProjectItem>().ToList())
                {
                    if (string.Equals(item.Path, sdePath, StringComparison.OrdinalIgnoreCase))
                        Project.Current.RemoveItem(item);
                }
            });

            Directory.CreateDirectory(folder);
            if (File.Exists(sdePath))
                File.Delete(sdePath);

            var parameters = Geoprocessing.MakeValueArray(
                folder,
                fileName,
                "SQL_SERVER",
                target.Instance,
                "OPERATING_SYSTEM_AUTH",
                "#",                     // username: not used with Windows authentication
                "#",                     // password: not used with Windows authentication
                "DO_NOT_SAVE_USERNAME",
                target.Database,
                "#",                     // schema
                "#",                     // version type
                "#");                    // version

            var result = await Geoprocessing.ExecuteToolAsync(
                "management.CreateDatabaseConnection",
                parameters,
                null,
                null,
                null,
                GPExecuteToolFlags.GPThread | GPExecuteToolFlags.RefreshProjectItems);

            if (result.IsFailed || !File.Exists(sdePath))
                throw new InvalidOperationException(
                    "Could not create the database connection file. " +
                    string.Join(" ", result.Messages.Select(m => m.Text)));

            await QueuedTask.Run(() =>
            {
                bool alreadyAdded = Project.Current.GetItems<GDBProjectItem>()
                    .Any(item => string.Equals(item.Path, sdePath, StringComparison.OrdinalIgnoreCase));

                if (!alreadyAdded && ItemFactory.Instance.Create(sdePath) is IProjectItem projectItem)
                    Project.Current.AddItem(projectItem);
            });

            return sdePath;
        }

        #endregion
    }
}
