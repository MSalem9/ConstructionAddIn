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
        private string _optionalComments;
        private int _selectedTabIndex = 0;
        private ComboBoxItemCode _selectedPipeDiameter;
        private ComboBoxItemCode _selectedPipeMaterial;
        private string _selectedPipeDescription;
        private bool _isPipeDiameterActive = false;
        private bool _isPipeExistEnabled = false;
        private string _isPipeExist = null;

        private ObservableCollection<string> _projectsName;
        private ObservableCollection<string> _PipeLinesName;

        // Properties
        public bool IsPipeExistEnabled
        {
            get => _isPipeExistEnabled;
            set
            {
                if (_isPipeExistEnabled != value)
                {
                    _isPipeExistEnabled = value;

                    if (value) 
                    {
                        _isPipeExist = "Exist - ";
                    }
                    else 
                    {
                        _isPipeExist = null;
                    }

                    OnPropertyChanged(nameof(IsPipeExistEnabled));
                }
            }
        }
        public string SelectedPipeDescription
        {
            get => _selectedPipeDescription;
            set
            {
                if (_selectedPipeDescription != value)
                {
                    _selectedPipeDescription = value;
                    OnPropertyChanged(nameof(SelectedPipeDescription));
                }
            }
        }
        public bool IsPipeDiameterActive
        {
            get => _isPipeDiameterActive;
            set
            {
                if (_isPipeDiameterActive != value)
                {
                    _isPipeDiameterActive = value;
                    OnPropertyChanged(nameof(IsPipeDiameterActive));
                }
            }
        }
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
        public string OptionalComments
        {
            get => _optionalComments;
            set 
            {
                if (_optionalComments != value) 
                {
                    _optionalComments = value;
                    OnPropertyChanged(nameof(OptionalComments));
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
        public ComboBoxItemCode SelectedPipeDiameter
        {
            get => _selectedPipeDiameter;
            set
            {
                if (_selectedPipeDiameter != value)
                {
                    _selectedPipeDiameter = value;
                    OnPropertyChanged(nameof(SelectedPipeDiameter));
                }
            }
        }
        public ComboBoxItemCode SelectedPipeMaterial
        {
            get => _selectedPipeMaterial;
            set
            {
                if (_selectedPipeMaterial != value)
                {
                    _selectedPipeMaterial = value;

                    SelectedPipeDiameter = null;
                    IsPipeDiameterActive = true;

                    OnPropertyChanged(nameof(SelectedPipeMaterial));
                    OnPropertyChanged(nameof(AvailablePipeDiameterList));
                    OnPropertyChanged(nameof(AvailablePipeDescriptionList));
                    OnPropertyChanged(nameof(DrawButtonText));
                }
            }
        }

        // Collections 
        public ObservableCollection<ComboBoxItemCode> AvailablePipeDiameterList
        {
            get
            {
                return SelectedPipeMaterial?.Value == 2
                    ? _steelPipeDiameterList
                    : _pePipeDiameterList;
            }
        }
        public ObservableCollection<string> AvailablePipeDescriptionList
        {
            get
            {
                return SelectedPipeMaterial?.Value == 2
                    ? _steelDescriptionList
                    : _pePipeDescriptionList;
            }
        }
        public ObservableCollection<ComboBoxItemCode> PipeMaterialList { get; }
            = new ObservableCollection<ComboBoxItemCode>
            {
                new ComboBoxItemCode
                {
                    Name = "PE Pipe",
                    Value = 1,
                    AttributeName = "PE"

                },
                new ComboBoxItemCode
                {
                    Name = "Steel Pipe",
                    Value = 2,
                    AttributeName = "steel"
                }
            };
        private readonly ObservableCollection<ComboBoxItemCode> _pePipeDiameterList 
            = new ObservableCollection<ComboBoxItemCode>
            {
                new ComboBoxItemCode { Name = "32 mm",  Value = 32 },
                new ComboBoxItemCode { Name = "63 mm",  Value = 63 },
                new ComboBoxItemCode { Name = "90 mm",  Value = 90 },
                new ComboBoxItemCode { Name = "125 mm", Value = 125 },
                new ComboBoxItemCode { Name = "180 mm", Value = 180 },
                new ComboBoxItemCode { Name = "250 mm", Value = 250 },
                new ComboBoxItemCode { Name = "315 mm", Value = 315 },
                new ComboBoxItemCode { Name = "355 mm", Value = 355 }
            };
        private readonly ObservableCollection<ComboBoxItemCode> _steelPipeDiameterList
            = new ObservableCollection<ComboBoxItemCode>
            {
                new ComboBoxItemCode { Name = "3''",  Value = 3 },
                new ComboBoxItemCode { Name = "4''",  Value = 4 },
                new ComboBoxItemCode { Name = "6''",  Value = 6 },
                new ComboBoxItemCode { Name = "8''",  Value = 8 },
                new ComboBoxItemCode { Name = "10''", Value = 10 },
                new ComboBoxItemCode { Name = "12''", Value = 12 }
            };
        private readonly ObservableCollection<string> _pePipeDescriptionList
            = new ObservableCollection<string>
            {
                "PE-100 (SDR11)",
                "PE-80 (SDR11)",
                "PE-80 (SDR17)",
            };
        private readonly ObservableCollection<string> _steelDescriptionList
            = new ObservableCollection<string>
            {
                "CS API-5L GR.B SCH.40 PE-COATED",
                "CS API-5L GR.B SCH.80 PE-COATED",
                "CS API-5L GR.B SCH.40 BARE",
            };
        public string DrawButtonText =>
            SelectedPipeMaterial?.Value == 2
                ? "Draw Steel Pipe"
                : "Draw PE Pipe";

        // Commands
        public RelayCommand StartDrawing { get; }
        public RelayCommand<string> DeleteLine { get; }
        public RelayCommand RefreshNames { get; }

        public PipeViewModel()
        {
            ProjectsName = new ObservableCollection<string>();
            StartDrawing = new RelayCommand(async () => await OnStartDrawing());
            RefreshNames = new RelayCommand(async () => await LoadAttributeValuesAsync());
            DeleteLine = new RelayCommand<string>(async (layerName) => await OnDeleteLine(layerName));
        }

        private async Task OnStartDrawing()
        {
            bool isFormValid =
                !string.IsNullOrWhiteSpace(SelectedProjectName) &&
                !string.IsNullOrWhiteSpace(SelectedPipeLineName) &&
                SelectedPipeDescription != null &&
                SelectedPipeMaterial != null &&
                SelectedPipeDiameter != null;

            if (!isFormValid)
            {
                MessageBox.Show("You must fill all the fields.");
                return;
            }

            var dto = new PipeDTO
            {
                LineName = SelectedPipeLineName,
                ProjectName = SelectedProjectName,
                PipeWidth = SelectedPipeDiameter.Value.ToString(),
                PipeMatrial = SelectedPipeMaterial.AttributeName,
                OptionalDetails = _isPipeExist + SelectedPipeDiameter.Name.ToString() + " " + SelectedPipeDescription,
                Comments = OptionalComments
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
            ProjectsName.Clear();
            //PipeLinesName.Clear();

            // Rest form
            SelectedPipeDescription = null;
            SelectedPipeDiameter = null;
            SelectedPipeLineName = null;
            OptionalComments = null;

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
        public class ComboBoxItemCode
        {
            public string Name { get; set; }
            public int Value { get; set; }
            public string AttributeName { get; set; }

            public override string ToString()
            {
                return Name;
            }
        }
        /// <summary>
        /// Resets the internal tool state.
        /// </summary>
        private void ResetToolState()
        {
            SelectedPipeLineName = null;
            OptionalComments = null;
        }
    }
}
