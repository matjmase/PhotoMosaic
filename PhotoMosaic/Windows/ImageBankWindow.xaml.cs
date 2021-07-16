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
    /// Interaction logic for ImageBankWindow.xaml
    /// </summary>
    public partial class ImageBankWindow : Window
    {
        public ImageBankWindow()
        {
            DataContext = new ImageBankWindowVM();
            InitializeComponent();
        }

        public ImageBankWindowVM GetViewModel()
        {
            return (ImageBankWindowVM)DataContext;
        }
        public void UpdateAddedPaths(IEnumerable<string> paths)
        {
            var vm = (ImageBankWindowVM)DataContext;
            vm.UpdateAddedPaths(paths);
        }
    }
}
