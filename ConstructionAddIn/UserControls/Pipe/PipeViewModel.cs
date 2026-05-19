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
        private string _pipeDepth;
        private string _pipePressure;
        private string _pipeWidth;
        private string _projectName;
        private string _selectedProjectName;
        private string _selectedPressure;
        private int _selectedTabIndex;
        private string _selectedClaimDate;


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
        public string PipeDepth
        {
            get => _pipeDepth;
            set
            {
                if (_pipeDepth != value)
                {
                    _pipeDepth = value;
                    OnPropertyChanged(nameof(PipeDepth));
                }
            }
        }
        public string ProjectName
        {
            get => _projectName;
            set
            {
                if (_projectName != value)
                {
                    _projectName = value;
                    OnPropertyChanged(nameof(ProjectName));
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
        public string PipePressure
        {
            get => _pipePressure;
            set
            {
                if (_pipePressure != value)
                {
                    _pipePressure = value;
                    OnPropertyChanged(nameof(PipePressure));
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

        // Commands
        public RelayCommand<string> StartDrawing { get; }
        public RelayCommand<string> DeleteLine { get; }

        public PipeViewModel()
        {
            StartDrawing = new RelayCommand<string>(async width => await OnStartDrawing(width));
            //DeleteLine = new RelayCommand<string>(async (layerName) =>
            //{
            //    RemoveLineContext.TargetLayerName = "PePipes";

            //    await FrameworkApplication.SetCurrentToolAsync(
            //        "FirstTryTest_Tools_RemovingTools_RemoveLineTool");
            //});
            //DeleteLine = new RelayCommand<string>(async (layerName) => await OnDeleteLine(layerName));
        }

        private async Task OnStartDrawing(string width)
        {
            //    bool isFormValid =
            //        !string.IsNullOrWhiteSpace(ProjectName) &&
            //        !string.IsNullOrWhiteSpace(PipeDepth) &&
            //        !string.IsNullOrWhiteSpace(PipePressure) &&
            //        !string.IsNullOrWhiteSpace(width);

            //    if (isFormValid)
            //    {
            //        PipeWidth = width;
            //    }

            //    if (!isFormValid)
            //    {
            //        MessageBox.Show("You must fill all the fields.");
            //        return;
            //    }

            //    var dto = new PipeDTO
            //    {
            //        ProjectName = SelectedProjectName,
            //        Depth = PipeDepth,
            //        Diameter = PipeWidth,
            //        OPSPres = SelectedPressure,
            //        HighOrLow = "عالي",
            //        ClaimDate = SelectedClaimDate
            //    };

            //    if (_selectedTabIndex == 0)
            //    {
            //        var request = PipeDtoMapper.ToPePipeRequest(dto);
            //        LineDrawContext.CurrentRequest = request;
            //    }
            //    else if (_selectedTabIndex == 1)
            //    {
            //        var request = PipeDtoMapper.ToSteelPipeRequest(dto);
            //        LineDrawContext.CurrentRequest = request;
            //    }

            //    //var request = PipeDtoMapper.ToPePipeRequest(dto);
            //    //LineDrawContext.CurrentRequest = request;

            //    await FrameworkApplication.SetCurrentToolAsync("FirstTryTest_Tools_AddingTools_CreateLineTool");
            //}

            //private async Task OnDeleteLine(string layerName)
            //{
            //    if (layerName == "PePipes")
            //    {
            //        RemoveLineContext.TargetLayerName = "PePipes";
            //        await FrameworkApplication.SetCurrentToolAsync(
            //            "FirstTryTest_Tools_RemovingTools_RemoveLineTool");
            //    }
            //    else
            //    {
            //        RemoveLineContext.TargetLayerName = "Steel";
            //        await FrameworkApplication.SetCurrentToolAsync(
            //            "FirstTryTest_Tools_RemovingTools_RemoveLineTool");
            //    }
            //}
        }


            // Notify Property Changed
        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
