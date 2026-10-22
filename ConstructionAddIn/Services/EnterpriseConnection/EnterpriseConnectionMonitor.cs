using ArcGIS.Core.Events;
using ArcGIS.Desktop.Core;
using ArcGIS.Desktop.Core.Events;
using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Dialogs;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using MessageBox = ArcGIS.Desktop.Framework.Dialogs.MessageBox;

namespace ConstructionAddIn.Services.EnterpriseConnection
{
    /// <summary>
    /// Watches the enterprise database connection in the background.
    ///
    /// When the server stops answering:
    /// - the user gets an alert (with the option to reconnect right away),
    /// - every Construction tool is disabled (DAML state <see cref="ConnectedStateId"/>),
    /// - ArcGIS Pro's own editing is disabled for the project,
    /// - the Construction dockpane shows a red "connection lost" banner.
    ///
    /// Editing stays disabled until the user presses "Refresh Enterprise Connection"
    /// and the reconnect succeeds, even if the network comes back on its own,
    /// because the reconnect is also what verifies the layer sources.
    /// </summary>
    public static class EnterpriseConnectionMonitor
    {
        /// <summary>DAML state: active while editing against the enterprise database is allowed.</summary>
        public const string ConnectedStateId = "ConstructionAddIn_DbConnected";

        private static readonly TimeSpan FirstCheckDelay = TimeSpan.FromSeconds(5);
        private static readonly TimeSpan CheckInterval = TimeSpan.FromSeconds(20);
        private static readonly TimeSpan RecheckDelay = TimeSpan.FromSeconds(3);

        private static Timer _timer;
        private static SubscriptionToken _projectOpenedToken;
        private static int _isChecking;
        private static bool _isAlertOpen;
        private static bool _reachableAgainNotified;
        private static bool _weEnabledEditTabOption;

        /// <summary>Server / database being watched. Null when the project has no enterprise layers.</summary>
        public static EnterpriseConnectionInfo Target { get; private set; }

        /// <summary>False while the connection is lost (or not yet verified after a loss).</summary>
        public static bool IsEditingAllowed { get; private set; } = true;

        /// <summary>Short text explaining why editing is disabled.</summary>
        public static string BlockReason { get; private set; }

        /// <summary>Raised (on any thread) when <see cref="IsEditingAllowed"/> changes.</summary>
        public static event Action StatusChanged;

        #region Start / stop

        public static void Start()
        {
            SetDamlState(true);

            _projectOpenedToken = ProjectOpenedEvent.Subscribe(args =>
            {
                // New project: rediscover its database and start unblocked.
                Target = null;
                _ = SetEditingAllowedAsync(true, null);
            });

            _timer = new Timer(_ => _ = CheckAsync(), null, FirstCheckDelay, CheckInterval);
        }

        public static void Stop()
        {
            _timer?.Dispose();
            _timer = null;

            if (_projectOpenedToken != null)
            {
                ProjectOpenedEvent.Unsubscribe(_projectOpenedToken);
                _projectOpenedToken = null;
            }
        }

        #endregion

        #region Periodic check

        private static async Task CheckAsync()
        {
            // Never run two checks at the same time.
            if (Interlocked.Exchange(ref _isChecking, 1) == 1)
                return;

            try
            {
                if (Project.Current == null)
                    return;

                Target ??= await EnterpriseConnectionService.DiscoverProjectConnectionAsync();
                if (Target == null)
                    return;

                bool reachable = await SqlServerProbe.IsReachableAsync(Target.Instance);

                // One failed probe can be a network hiccup: confirm before blocking.
                if (!reachable)
                {
                    await Task.Delay(RecheckDelay);
                    reachable = await SqlServerProbe.IsReachableAsync(Target.Instance);
                }

                if (!reachable && IsEditingAllowed)
                {
                    await OnConnectionLostAsync();
                }
                else if (reachable && !IsEditingAllowed && !_reachableAgainNotified)
                {
                    _reachableAgainNotified = true;
                    Notify(
                        "Enterprise database is reachable again",
                        "Press \"Refresh Enterprise Connection\" to reconnect and enable editing.");
                }
            }
            catch
            {
                // A failed check must never crash ArcGIS Pro; the next tick tries again.
            }
            finally
            {
                Interlocked.Exchange(ref _isChecking, 0);
            }
        }

        private static async Task OnConnectionLostAsync()
        {
            await SetEditingAllowedAsync(false, $"Connection to the enterprise database ({Target}) was lost.");

            Notify(
                "Enterprise database connection lost",
                "Editing is disabled. Press \"Refresh Enterprise Connection\" to reconnect.");

            await RunOnUiThreadAsync(async () =>
            {
                if (_isAlertOpen)
                    return;

                _isAlertOpen = true;
                try
                {
                    var answer = MessageBox.Show(
                        "The connection to the enterprise database was lost." +
                        Environment.NewLine + Environment.NewLine +
                        $"Server: {Target?.Instance}" + Environment.NewLine +
                        $"Database: {Target?.Database}" +
                        Environment.NewLine + Environment.NewLine +
                        "All editing is disabled until you reconnect." +
                        Environment.NewLine + Environment.NewLine +
                        "Reconnect now?",
                        "Enterprise Database Connection Lost",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Error);

                    if (answer == MessageBoxResult.Yes)
                        await EnterpriseConnectionService.ReconnectAsync();
                }
                finally
                {
                    _isAlertOpen = false;
                }
            });
        }

        #endregion

        #region Called by the reconnect workflow

        /// <summary>Reconnect succeeded and the layers were verified: allow editing again.</summary>
        public static async Task MarkConnectedAsync(EnterpriseConnectionInfo target)
        {
            Target = target;
            await SetEditingAllowedAsync(true, null);
        }

        /// <summary>Keep / put editing in the disabled state with the given reason.</summary>
        public static Task BlockEditingAsync(string reason) => SetEditingAllowedAsync(false, reason);

        #endregion

        #region Editing on / off

        private static Task SetEditingAllowedAsync(bool allowed, string reason)
        {
            // Project / framework calls below are made from the UI thread, also when the
            // background timer triggers the change.
            return RunOnUiThreadAsync(async () =>
            {
                bool changed = IsEditingAllowed != allowed || BlockReason != reason;

                IsEditingAllowed = allowed;
                BlockReason = reason;
                _reachableAgainNotified = false;

                SetDamlState(allowed);

                if (changed)
                    StatusChanged?.Invoke();

                await SetNativeEditingAsync(allowed);
            });
        }

        /// <summary>Enables / disables every DAML control whose condition uses <see cref="ConnectedStateId"/>.</summary>
        private static void SetDamlState(bool connected)
        {
            RunOnUiThread(() =>
            {
                if (connected)
                    FrameworkApplication.State.Activate(ConnectedStateId);
                else
                    FrameworkApplication.State.Deactivate(ConnectedStateId);
            });
        }

        /// <summary>
        /// Turns ArcGIS Pro's own editing (Edit tab, attribute edits, native delete...) off or on.
        ///
        /// Pro can only switch editing off when the option
        /// "Enable and disable editing from the Edit tab" is on, so it is switched on
        /// while the connection is lost and restored to its original value afterwards.
        /// </summary>
        private static async Task SetNativeEditingAsync(bool enabled)
        {
            var project = Project.Current;
            if (project == null)
                return;

            try
            {
                if (!enabled)
                {
                    await QueuedTask.Run(() =>
                    {
                        if (!ApplicationOptions.EditingOptions.EnableEditingFromEditTab)
                        {
                            ApplicationOptions.EditingOptions.EnableEditingFromEditTab = true;
                            _weEnabledEditTabOption = true;
                        }
                    });

                    // Fails while unsaved edits exist; those edits are handled when reconnecting,
                    // and the Creation/Deletion controllers still reject new edits.
                    if (project.IsEditingEnabled)
                        await project.SetIsEditingEnabledAsync(false);
                }
                else
                {
                    if (!project.IsEditingEnabled)
                        await project.SetIsEditingEnabledAsync(true);

                    if (_weEnabledEditTabOption)
                    {
                        await QueuedTask.Run(() =>
                            ApplicationOptions.EditingOptions.EnableEditingFromEditTab = false);
                        _weEnabledEditTabOption = false;
                    }
                }
            }
            catch
            {
                // The DAML state and the controllers still block the Construction tools.
            }
        }

        #endregion

        #region Helpers

        private static void Notify(string title, string message)
        {
            RunOnUiThread(() => FrameworkApplication.AddNotification(new Notification
            {
                Title = title,
                Message = message,
                ImageUrl = @"pack://application:,,,/ArcGIS.Desktop.Resources;component/Images/GenericWarning32.png"
            }));
        }

        private static void RunOnUiThread(Action action)
        {
            var dispatcher = Application.Current?.Dispatcher;
            if (dispatcher == null || dispatcher.CheckAccess())
                action();
            else
                dispatcher.Invoke(action);
        }

        private static Task RunOnUiThreadAsync(Func<Task> action)
        {
            var dispatcher = Application.Current?.Dispatcher;
            if (dispatcher == null || dispatcher.CheckAccess())
                return action();

            return dispatcher.InvokeAsync(action).Task.Unwrap();
        }

        #endregion
    }
}
