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
using System.Windows.Shapes;

namespace GCS_G03.DialogBox
{
    /// <summary>
    /// Interaction logic for AppDialogBox.xaml
    /// </summary>
    public partial class AppDialogBox : Window
    {
        public AppDialogBox(string header, string header1, bool warningmb, bool exitmb, bool resetmb)
        {
            InitializeComponent();
        }
    }
}
