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
        RenderProcessWindowVM _vm;

        public RenderProcessWindow(RenderingModel model)
        {
            _vm = new RenderProcessWindowVM(model);
            DataContext = _vm;
            InitializeComponent();
            this.Closed += RenderProcessWindow_Closed;
        }

        ~RenderProcessWindow()
        {
            this.Closed -= RenderProcessWindow_Closed;
        }

        private void RenderProcessWindow_Closed(object sender, EventArgs e)
        {
            _vm.CancelCommand.Execute(null);
        }
    }
}
