using PhotoMosaic.Common;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace PhotoMosaic.Controls
{
    public class SelectableImageControlVM : INotifyPropertyChanged
    {
        private string _imagePath;
        private bool _selected;
        private BitmapImage _bitmapSource;

        public string ImagePath
        {
            get { return _imagePath; }
            set
            {
                if (value != _imagePath)
                {
                    _imagePath = value;
                    NotifyPropertyChanged();
                    UpdateBitmapFromPath();
                }
            }
        }

        public BitmapImage SourceForImage
        {
            get { return _bitmapSource; }
            set
            {
                if (value != _bitmapSource)
                {
                    _bitmapSource = value;
                    NotifyPropertyChanged();
                }
            }
        }

        public bool Selected
        {
            get { return _selected; }
            set
            {
                if (value != _selected)
                {
                    _selected = value;
                    NotifyPropertyChanged();
                }
            }
        }

        public ICommand ToggleSelectionCommand => new DelegateCommand((obj) => Selected = !Selected);

        public SelectableImageControlVM(string pathToImage)
        {
            ImagePath = pathToImage;
        }

        private void UpdateBitmapFromPath()
        {
            var newBitmap = new BitmapImage();

            newBitmap.BeginInit();
            newBitmap.UriSource = new Uri(ImagePath);
            newBitmap.EndInit();

            SourceForImage = newBitmap;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
