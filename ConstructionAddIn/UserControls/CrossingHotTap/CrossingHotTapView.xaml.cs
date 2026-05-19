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

namespace ConstructionAddIn.UserControls.CrossingHotTap
{
    /// <summary>
    /// Interaction logic for CrossingHotTapView.xaml
    /// </summary>
    public partial class CrossingHotTapView : UserControl
    {
        public CrossingHotTapView()
        {
            InitializeComponent();
            DataContext = new CrossingHotTapViewModel();
        }
    }
}
