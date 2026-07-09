using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Dialogs;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using ConstructionAddIn.Helpers.LineHelpers;
using ConstructionAddIn.Tools.RemovingTools;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConstructionAddIn.UserControls.Pipe
{
    public class PipeViewModel : INotifyPropertyChanged
    {
        // Fields
        private string _pipeType;
        private string _pipeWidth;
        private string _selectedProjectName;
        private string _selectedPipeLayerName = "pipes";
        private string _selectedPipeLineName;
        private string _optionalDetails;
        private int _selectedTabIndex = 0;

        private ObservableCollection<string> _projectsName;
        private ObservableCollection<string> _PipeLinesName;


        // Properties
        public string PipeType
        {
            get => _pipeType;
            set
            {
                if (_pipeType != value)
                {
                    _pipeType = value;
                    OnPropertyChanged(nameof(PipeType));
                }
            }
        }
        public string PipeWidth
        {
            get => _pipeWidth;
            set
            {
                if (_pipeWidth != value)
                {
                    _pipeWidth = value;
                    OnPropertyChanged(nameof(PipeWidth));
                }
            }
        }
        public int SelectedTabIndex
        {
            get => _selectedTabIndex;
            set
            {
                if (_selectedTabIndex != value)
                {
                    _selectedTabIndex = value;
                    OnPropertyChanged(nameof(SelectedTabIndex));

                    _ = LoadAttributeValuesAsync();
                }
            }
        }
        public string SelectedProjectName
        {
            get => _selectedProjectName;
            set
            {
                if (_selectedProjectName != value)
                {
                    _selectedProjectName = value;
                    OnPropertyChanged(nameof(SelectedProjectName));
                }
            }
        }
        public string SelectedPipeLayerName
        {
            get => _selectedPipeLayerName;
            set
            {
                if (_selectedPipeLayerName != value)
                {
                    _selectedPipeLayerName = value;
                    OnPropertyChanged(nameof(SelectedPipeLayerName));
                    _ = LoadAttributeValuesAsync();
                }
            }
        }
        public string SelectedPipeLineName
        {
            get => _selectedPipeLineName;
            set
            {
                if (_selectedPipeLineName != value)
                {
                    _selectedPipeLineName = value;
                    OnPropertyChanged(nameof(SelectedPipeLineName));
                }
            }
        }
        public string OptionalDetails 
        {
            get => _optionalDetails;
            set 
            {
                if (_optionalDetails != value) 
                {
                    _optionalDetails = value;
                    OnPropertyChanged(nameof(OptionalDetails));
                }
            }
        }
        public ObservableCollection<string> ProjectsName
        {
            get => _projectsName;
            set
            {
                if (_projectsName != value)
                {
                    _projectsName = value;
                    OnPropertyChanged(nameof(ProjectsName));
                }
            }
        }
        public ObservableCollection<string> PipeLinesName
        {
            get => _PipeLinesName;
            set
            {
                if (_PipeLinesName != value)
                {
                    _PipeLinesName = value;
                    OnPropertyChanged(nameof(PipeLinesName));
                }
            }
        }

        // Commands
        public RelayCommand<string> StartDrawing { get; }
        public RelayCommand<string> DeleteLine { get; }
        public RelayCommand RefreshNames { get; }

        public PipeViewModel()
        {
            ProjectsName = new ObservableCollection<string>();

            StartDrawing = new RelayCommand<string>(async width => await OnStartDrawing(width));
            RefreshNames = new RelayCommand(async () => await LoadAttributeValuesAsync());
            DeleteLine = new RelayCommand<string>(async (layerName) => await OnDeleteLine(layerName));
        }

        private async Task OnStartDrawing(string width)
        {
            bool isFormValid =
                !string.IsNullOrWhiteSpace(SelectedProjectName) &&
                !string.IsNullOrWhiteSpace(SelectedPipeLineName) &&
                !string.IsNullOrWhiteSpace(width);

            if (isFormValid)
            {
                PipeWidth = width;
            }

            if (!isFormValid)
            {
                MessageBox.Show("You must fill all the fields.");
                return;
            }

            var dto = new PipeDTO
            {
                LineName = SelectedPipeLineName,
                ProjectName = SelectedProjectName,
                PipeWidth = PipeWidth,
                PipeMatrial = SelectedTabIndex == 0 ? "PE" : "Steel",
                OptionalDetails = OptionalDetails
            };


            var request = PipeDtoMapper.ToPipeRequest(dto);
            LineDrawContext.CurrentRequest = request;

            await FrameworkApplication.SetCurrentToolAsync("ConstructionAddIn_Tools_AddingTools_CreateLineTool");
        }

        private async Task OnDeleteLine(string layerName)
        {
            RemoveLineContext.TargetLayerName = "pipes";
            await FrameworkApplication.SetCurrentToolAsync(
                "ConstructionAddIn_Tools_RemovingTools_RemoveLineTool");
        }


        // Helps

        // Notify Property Changed
        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        private async Task LoadAttributeValuesAsync()
        {
            //ProjectsName.Clear();
            //PipeLinesName.Clear();

            if (string.IsNullOrWhiteSpace(SelectedPipeLayerName))
                return;

            var mapView = MapView.Active;
            if (mapView?.Map == null)
                return;

            var result = await QueuedTask.Run(() =>
            {
                var pipeLinesNames = new HashSet<string>();
                var projectsNames = new HashSet<string>();

                var layer = mapView.Map
                    .GetLayersAsFlattenedList()
                    .OfType<FeatureLayer>()
                    .FirstOrDefault(l =>
                        string.Equals(l.Name, SelectedPipeLayerName, StringComparison.OrdinalIgnoreCase));

                if (layer == null)
                    return (pipeLinesNames, projectsNames);

                using (var cursor = layer.Search())
                {
                    while (cursor.MoveNext())
                    {
                        using (var feature = cursor.Current as ArcGIS.Core.Data.Feature)
                        {
                            if (feature == null)
                                continue;

                            var p = feature["ProjectName"];

                            if (p != null && p != DBNull.Value)
                                projectsNames.Add(p.ToString());
                        }
                    }
                }

                return (pipeLinesNames, projectsNames);
            });

            foreach (var p in result.projectsNames
                                    .Where(x => !string.IsNullOrWhiteSpace(x))
                                    .OrderBy(x => x))
            {
                ProjectsName.Add(p);
            }
        }
        /// <summary>
        /// Resets the internal tool state.
        /// </summary>
        private void ResetToolState()
        {
            SelectedPipeLineName = null;
            OptionalDetails = null;
        }
    }
}
