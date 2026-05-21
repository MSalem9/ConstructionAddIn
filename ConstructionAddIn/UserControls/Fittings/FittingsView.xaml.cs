using ConstructionAddIn.UserControls.ValveRegulator;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ConstructionAddIn.UserControls.Fittings
{
    /// <summary>
    /// Interaction logic for FittingsView.xaml
    /// </summary>
    public partial class FittingsView : UserControl
    {
        public FittingsView()
        {
            InitializeComponent();
            DataContext = new FittingsViewModel();
        }
    }
}
