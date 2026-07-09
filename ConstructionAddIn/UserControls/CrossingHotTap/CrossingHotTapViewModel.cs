using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Dialogs;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using ConstructionAddIn.Helpers.LineHelpers;
using ConstructionAddIn.Helpers.PointHelpers;
using ConstructionAddIn.Helpers.PointHelpers.HotTap;
using ConstructionAddIn.Tools.RemoveTools;
using ConstructionAddIn.Tools.RemovingTools;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;

namespace ConstructionAddIn.UserControls.CrossingHotTap
{
    public class CrossingHotTapViewModel : INotifyPropertyChanged
    {

        // Fields
        private string _name;
        private string _selectedProjectName;
        private string _selectedPipeLayerName = "CrossingNew";
        private string _selectedPipeLineName;
        private int _selectedTabIndex = 0;

        private ObservableCollection<string> _projectsName;
        private ObservableCollection<string> _PipeLinesName;


        // Properties
        public string Name
        {
            get => _name;
            set
            {
                if (_name != value)
                {
                    _name = value;
                    OnPropertyChanged(nameof(Name));
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

        public CrossingHotTapViewModel()
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

            if (!isFormValid)
            {
                MessageBox.Show("You must fill all the fields.");
                return;
            }

            var dto = new CrossingHotTapDTO
            {
                Name = Name,
                LineName = SelectedPipeLineName,
                ProjectName = SelectedProjectName,
            };

            if (SelectedTabIndex == 0)
            {
                var request = PipeDtoMapper.ToCrossingRequest(dto);
                LineDrawContext.CurrentRequest = request;

                await FrameworkApplication.SetCurrentToolAsync("ConstructionAddIn_Tools_AddingTools_CreateCrossingTool");
            }
            else
            {
                PointDrawContext.CurrentRequest = HotTapDTOMapper.CreateRequest(dto);

                await FrameworkApplication.SetCurrentToolAsync("ConstructionAddIn_Tools_AddingTools_CreatePointFeatureTool");
            }
        }

        private async Task OnDeleteLine(string layerName)
        {
            if (SelectedTabIndex == 0)
            {
                RemoveLineContext.TargetLayerName = "CrossingNew";

                await FrameworkApplication.SetCurrentToolAsync(
                     "ConstructionAddIn_Tools_RemovingTools_RemoveLineTool");
            }
            else
            {
                var request = HotTapDTOMapper.CreateDeleteRequest();

                await RemovePointFeatureTool.StartAsync(request);
            }
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
    }
}

