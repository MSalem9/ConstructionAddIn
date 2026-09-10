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
using ArcGIS.Core.Data;
using ArcGIS.Desktop.Editing;
using ArcGIS.Desktop.Editing.Attributes;

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
                new ComboBoxItemCode { Name = "3"+'"' ,  Value = 3 },
                new ComboBoxItemCode { Name = "4"+'"' ,  Value = 4 },
                new ComboBoxItemCode { Name = "6"+'"' ,  Value = 6 },
                new ComboBoxItemCode { Name = "8"+'"' ,  Value = 8 },
                new ComboBoxItemCode { Name = "10"+'"' , Value = 10 },
                new ComboBoxItemCode { Name = "12"+'"' , Value = 12 },
                new ComboBoxItemCode { Name = "14"+'"' , Value = 14 },
                new ComboBoxItemCode { Name = "16"+'"' , Value = 16 },
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
        public RelayCommand ConvertToExist { get; }

        public PipeViewModel()
        {
            ProjectsName = new ObservableCollection<string>();
            StartDrawing = new RelayCommand(async () => await OnStartDrawing());
            RefreshNames = new RelayCommand(async () => await LoadAttributeValuesAsync());
            DeleteLine = new RelayCommand<string>(async (layerName) => await OnDeleteLine(layerName));
            ConvertToExist = new RelayCommand(async () => await OnConvertToExist());
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
        private async Task OnConvertToExist()
        {
            // ============================================================
            // SETTINGS
            // Change these if your actual layer/field names are different
            // ============================================================
            const string LAYER_NAME = "pipes";
            const string FIELD_NAME = "Details";
            // ============================================================

            var mapView = MapView.Active;

            if (mapView?.Map == null)
            {
                MessageBox.Show(
                    "No active map is open.",
                    "Convert To Exist");

                return;
            }

            try
            {
                string resultMessage = await QueuedTask.Run(() =>
                {
                    // ----------------------------------------------------
                    // Find the pipe layer
                    // ----------------------------------------------------
                    FeatureLayer layer = mapView.Map
                        .GetLayersAsFlattenedList()
                        .OfType<FeatureLayer>()
                        .FirstOrDefault(l =>
                            string.Equals(
                                l.Name,
                                LAYER_NAME,
                                StringComparison.OrdinalIgnoreCase));

                    if (layer == null)
                    {
                        return $"Layer '{LAYER_NAME}' was not found.";
                    }

                    // ----------------------------------------------------
                    // Check that field exists
                    // ----------------------------------------------------
                    string actualFieldName;

                    using (Table table = layer.GetTable())
                    using (TableDefinition definition = table.GetDefinition())
                    {
                        Field field = definition
                            .GetFields()
                            .FirstOrDefault(f =>
                                string.Equals(
                                    f.Name,
                                    FIELD_NAME,
                                    StringComparison.OrdinalIgnoreCase));

                        if (field == null)
                        {
                            return
                                $"Field '{FIELD_NAME}' was not found " +
                                $"in layer '{LAYER_NAME}'.";
                        }

                        if (field.FieldType != FieldType.String)
                        {
                            return
                                $"Field '{FIELD_NAME}' is not a text field.";
                        }

                        actualFieldName = field.Name;
                    }

                    // ----------------------------------------------------
                    // Get selected features
                    // ----------------------------------------------------
                    using Selection selection = layer.GetSelection();

                    var selectedOids = selection.GetObjectIDs();

                    if (selectedOids.Count == 0)
                    {
                        return
                            $"No features are selected in '{LAYER_NAME}'.";
                    }

                    // ----------------------------------------------------
                    // Create ArcGIS Pro edit operation
                    // ----------------------------------------------------
                    EditOperation editOperation = new EditOperation
                    {
                        Name = "Convert Pipe To Exist"
                    };

                    int changedCount = 0;
                    int alreadyExistCount = 0;

                    // ----------------------------------------------------
                    // Process selected features
                    // ----------------------------------------------------
                    foreach (long oid in selectedOids)
                    {
                        Inspector inspector = new Inspector();

                        inspector.Load(layer, oid);

                        object value = inspector[actualFieldName];

                        string currentText =
                            value == null || value == DBNull.Value
                                ? ""
                                : value.ToString();

                        currentText = currentText.Trim();

                        // ------------------------------------------------
                        // Check whether it already starts with Exist
                        //
                        // Valid examples:
                        //
                        // Exist
                        //
                        // Exist - 8'' CS API-5L GR.B SCH.80 PE-COATED
                        // ------------------------------------------------
                        bool alreadyExist =
                            currentText.Equals(
                                "Exist",
                                StringComparison.OrdinalIgnoreCase)
                            ||
                            currentText.StartsWith(
                                "Exist - ",
                                StringComparison.OrdinalIgnoreCase);

                        if (alreadyExist)
                        {
                            alreadyExistCount++;
                            continue;
                        }

                        // ------------------------------------------------
                        // Create new text
                        // ------------------------------------------------
                        string newText;

                        if (string.IsNullOrWhiteSpace(currentText))
                        {
                            newText = "Exist";
                        }
                        else
                        {
                            newText = $"Exist - {currentText}";
                        }

                        // ------------------------------------------------
                        // Update attribute
                        // ------------------------------------------------
                        inspector[actualFieldName] = newText;

                        editOperation.Modify(inspector);

                        changedCount++;
                    }

                    // ----------------------------------------------------
                    // Nothing needed changing
                    // ----------------------------------------------------
                    if (changedCount == 0)
                    {
                        return
                            "No changes were needed.\n\n" +
                            $"{alreadyExistCount} selected feature(s) " +
                            "already contain 'Exist'.";
                    }

                    // ----------------------------------------------------
                    // Execute edits
                    // ----------------------------------------------------
                    bool success = editOperation.Execute();

                    if (!success)
                    {
                        return
                            "Edit operation failed.\n\n" +
                            editOperation.ErrorMessage;
                    }

                    return
                        "Completed successfully.\n\n" +
                        $"Updated: {changedCount}\n" +
                        $"Already Exist: {alreadyExistCount}";
                });

                MessageBox.Show(
                    resultMessage,
                    "Convert To Exist");
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"An error occurred:\n\n{ex.Message}",
                    "Convert To Exist");
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
