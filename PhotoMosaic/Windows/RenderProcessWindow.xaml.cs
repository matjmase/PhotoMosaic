using PhotoMosaic.Models;
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

namespace PhotoMosaic.Windows
{
    /// <summary>
    /// Interaction logic for RenderProcessWindow.xaml
    /// </summary>
    public partial class RenderProcessWindow : Window
    {
        public RenderProcessWindow(RenderingModel model)
        {
            DataContext = new RenderProcessWindowVM(model);
            InitializeComponent();
        }
    }
}
