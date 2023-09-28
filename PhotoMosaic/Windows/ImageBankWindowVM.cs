using Microsoft.Win32;
using PhotoMosaic.Common;
using PhotoMosaic.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace PhotoMosaic.Windows
{
    public class ImageBankWindowVM : INotifyPropertyChanged
    {
        private string _currentDirectory;
        private bool _proceedWithChanges;

        public string CurrentDirectory
        {
            get { return _currentDirectory; }
            set
            {
                if (value != _currentDirectory)
                {
                    _currentDirectory = value;
                    NotifyPropertyChanged();
                    UpdateFromNewDirectory();
                }
            }
        }

        public bool ProceedWithChanges
        {
            get { return _proceedWithChanges; }
            set
            {
                if (_proceedWithChanges != value)
                {
                    _proceedWithChanges = value;
                    NotifyPropertyChanged();
                }
            }
        }

        public ObservableCollection<SelectableImageControlVM> CurrentFolderPaths { get; set; } = new ObservableCollection<SelectableImageControlVM>();
        public ObservableCollection<SelectableImageControlVM> AddedImagePaths { get; set; } = new ObservableCollection<SelectableImageControlVM>();

        private HashSet<string> _redundantPaths = new HashSet<string>();

        public ICommand SelectDirectoryCommand => new DelegateCommand((obj) => SelectDirectory());

        public ICommand CurrentFolderPathsSelectAllCommand => new DelegateCommand((obj) =>
        {
            foreach (var current in CurrentFolderPaths)
            {
                current.Selected = true;
            }
        });
        public ICommand CurrentFolderPathsDeselectAllCommand => new DelegateCommand((obj) =>
        {
            foreach (var current in CurrentFolderPaths)
            {
                current.Selected = false;
            }
        });
        public ICommand CurrentFolderPathsAddCommand => new DelegateCommand((obj) =>
        {
            foreach (var current in CurrentFolderPaths.Where(e => e.Selected == true && !_redundantPaths.Contains(e.ImagePath)))
            {
                AddedImagePaths.Add(new SelectableImageControlVM(current.ImagePath));
                _redundantPaths.Add(current.ImagePath);
            }
        });

        public ICommand AddedImagePathsSelectAllCommand => new DelegateCommand((obj) => {
            foreach (var added in AddedImagePaths)
            {
                added.Selected = true;
            }
        });

        public ICommand AddedImagePathsDeselectAllCommand => new DelegateCommand((obj) => {
            foreach (var added in AddedImagePaths)
            {
                added.Selected = false;
            }
        });

        public ICommand AddedImagePathsRemoveCommand => new DelegateCommand((obj) => {
            foreach (var added in AddedImagePaths.Where(e => e.Selected).ToList())
            {
                _redundantPaths.Remove(added.ImagePath);
                AddedImagePaths.Remove(added);
            }
        });

        public ICommand ProceedWithChangesCommand => new DelegateCommand((obj) =>
        {
            ProceedWithChanges = true;
            foreach (Window window in Application.Current.Windows)
            {
                if (object.ReferenceEquals(window.DataContext, this))
                {
                    window.Close();
                    return;
                }
            }
        });

        public ImageBankWindowVM()
        {
            CurrentDirectory = DriveInfo.GetDrives().First().Name;
        }

        public void UpdateAddedPaths(IEnumerable<string> paths)
        {
            if (paths == null)
                return;

            AddedImagePaths.Clear();

            foreach (var item in paths)
            {
                AddedImagePaths.Add(new SelectableImageControlVM(item));
            }
        }

        private void UpdateFromNewDirectory()
        {
            CurrentFolderPaths.Clear();

            var files = Directory.GetFiles(CurrentDirectory).Select(e => e.ToLower()).Where(e => e.EndsWith(".jpg") || e.EndsWith(".jfif") || e.EndsWith(".jpeg") || e.EndsWith(".png"));

            foreach (var file in files)
            {
                CurrentFolderPaths.Add(new SelectableImageControlVM(file));
            }
        }

        private void SelectDirectory()
        {
            //Hack, couldn't get openfolderdialog
            OpenFileDialog folderBrowser = new OpenFileDialog();
            folderBrowser.ValidateNames = false;
            folderBrowser.CheckFileExists = false;
            folderBrowser.CheckPathExists = true;
            folderBrowser.FileName = "Folder Selection.";
            if (folderBrowser.ShowDialog() == true)
            {
                CurrentDirectory = Path.GetDirectoryName(folderBrowser.FileName);
                // ...
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
