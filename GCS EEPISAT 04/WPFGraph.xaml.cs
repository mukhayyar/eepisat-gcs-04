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
using System.Windows.Input;
using WPFGraph;

namespace GraphGCS04.UsersControl
{
    /// <summary>
    /// Interaction logic for UserControl1.xaml
    /// </summary>
    public partial class GraphExample : UserControl
    {
        public GraphExample()
        {
            InitializeComponent();
        }
        double altitude;
        public void CallClickAltitude(double value)
        {
            altitude = value;
            System.Diagnostics.Debug.WriteLine("Check " + altitude);

            AltitudeTestGraphBtn_Click(null, null);
        }
        private void AltitudeTestGraphBtn_Click(object sender, RoutedEventArgs e)
        {
            var graph = new ViewModel();
            graph.altitude = altitude;
            graph.AddItem();
            System.Diagnostics.Debug.WriteLine("Check Again");

        }
    }
    
}
