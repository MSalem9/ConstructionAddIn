using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Contracts;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace ConstructionAddIn.Dialogs.ReconnectProgress
{
    public enum StepState
    {
        Running,
        Done,
        Warning,
        Failed
    }

    /// <summary>One line in the progress window, e.g. "Testing server 192.168.0.10".</summary>
    public class ReconnectStep : PropertyChangedBase
    {
        private string _detail;
        private StepState _state = StepState.Running;

        public string Text { get; init; }

        public string Detail
        {
            get => _detail;
            set => SetProperty(ref _detail, value);
        }

        public StepState State
        {
            get => _state;
            set => SetProperty(ref _state, value);
        }

        public void Done(string detail = null) => Set(StepState.Done, detail);
        public void Warn(string detail) => Set(StepState.Warning, detail);
        public void Fail(string detail) => Set(StepState.Failed, detail);

        private void Set(StepState state, string detail)
        {
            State = state;
            if (detail != null)
                Detail = detail;
        }
    }

    /// <summary>
    /// What the "Refresh Enterprise Connection" progress window shows.
    /// Must be updated on the UI thread (the reconnect workflow runs there between awaits).
    /// </summary>
    public class ReconnectProgressViewModel : PropertyChangedBase
    {
        private bool _isRunning = true;
        private bool _succeeded;
        private string _summary = "Reconnecting to the enterprise database…";

        public ObservableCollection<ReconnectStep> Steps { get; } = new();

        /// <summary>Layers whose data source is not the enterprise database (shown at the end).</summary>
        public ObservableCollection<string> Problems { get; } = new();

        public bool IsRunning
        {
            get => _isRunning;
            private set
            {
                SetProperty(ref _isRunning, value);
                NotifyPropertyChanged(nameof(IsFinished));
            }
        }

        public bool IsFinished => !IsRunning;

        public bool Succeeded
        {
            get => _succeeded;
            private set => SetProperty(ref _succeeded, value);
        }

        public string Summary
        {
            get => _summary;
            private set => SetProperty(ref _summary, value);
        }

        public bool HasProblems => Problems.Count > 0;

        /// <summary>True while the window is open; set by the window itself.</summary>
        public bool IsWindowOpen { get; set; }

        /// <summary>Adds a new running step.</summary>
        public ReconnectStep Begin(string text)
        {
            var step = new ReconnectStep { Text = text };
            Steps.Add(step);
            return step;
        }

        /// <summary>The step currently running, if any (used to mark it failed on an exception).</summary>
        public ReconnectStep CurrentStep => Steps.LastOrDefault(s => s.State == StepState.Running);

        public void Finish(bool succeeded, string summary, IEnumerable<string> problems = null)
        {
            foreach (var problem in problems ?? Enumerable.Empty<string>())
                Problems.Add(problem);

            Succeeded = succeeded;
            Summary = summary;
            IsRunning = false;
            NotifyPropertyChanged(nameof(HasProblems));

            // The user closed the window before the end: still tell them the result.
            if (!IsWindowOpen)
            {
                FrameworkApplication.AddNotification(new Notification
                {
                    Title = "Refresh Enterprise Connection",
                    Message = summary,
                    ImageUrl = succeeded
                        ? @"pack://application:,,,/ArcGIS.Desktop.Resources;component/Images/GenericCheckMark32.png"
                        : @"pack://application:,,,/ArcGIS.Desktop.Resources;component/Images/GenericWarning32.png"
                });
            }
        }
    }
}
