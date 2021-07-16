using Microsoft.Win32;
using PhotoMosaic.Common;
using PhotoMosaic.Models;
using PhotoMosaic.Windows;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace PhotoMosaic
{
    public class MainWindowVM : INotifyPropertyChanged
    {
        private string _recreateImagePath;
        private ImageDimensions _recreateImageDimensions;

        private int _samplePlotDimensions = 25;
        private int _replacementPlotDimensions = 50;

        private PlotImageProcessing _plotProcessing;
        private bool _adjustImageRGB = true;

        private string _outputFilePath;

        public string RecreateImagePath
        {
            get { return _recreateImagePath; }
            set
            {
                if (value != _recreateImagePath)
                {
                    _recreateImagePath = value;
                    NotifyPropertyChanged();
                    NotifyPropertyChanged("RecreateImageChosen");
                    NotifyPropertyChanged("ProcessToolTip");
                    NotifyPropertyChanged("ProcessEnabled");
                }
            }
        }

        public ImageDimensions RecreateImageDimensions
        {
            get { return _recreateImageDimensions; }
            set
            {
                _recreateImageDimensions = value;
                NotifyPropertyChanged();
                NotifyPropertyChanged("Totalplots");
            }
        }

        public bool BankImagePathsPopulated
        {
            get { return BankImagePaths.Count != 0; }
        }

        public bool RecreateImageChosen
        {
            get { return RecreateImagePath != null; }
        }

        public int SamplePlotDimensions
        {
            get { return _samplePlotDimensions; }
            set
            {
                if (value != _samplePlotDimensions)
                {
                    _samplePlotDimensions = value;
                    NotifyPropertyChanged();
                    NotifyPropertyChanged("Totalplots");
                    NotifyPropertyChanged("NewImageSizePercentage");
                }
            }
        }

        public int Totalplots
        {
            get
            {
                var roundUp = RecreateImageDimensions.RoundUp(SamplePlotDimensions);

                return roundUp.Height / SamplePlotDimensions * roundUp.Width / SamplePlotDimensions;
            }
        }

        public int ReplacementPlotDimensions
        {
            get { return _replacementPlotDimensions; }
            set
            {
                if (value != _replacementPlotDimensions)
                {
                    _replacementPlotDimensions = value;
                    NotifyPropertyChanged();
                    NotifyPropertyChanged("NewImageSizePercentage");
                }
            }
        }

        public double NewImageSizePercentage
        {
            get { return Math.Pow(ReplacementPlotDimensions, 2.0) / Math.Pow(SamplePlotDimensions, 2.0) * 100; }
        }

        public PlotImageProcessing PlotProcessing
        {
            get { return _plotProcessing; }
            set
            {
                if (value != _plotProcessing)
                {
                    _plotProcessing = value;
                    NotifyPropertyChanged();
                }
            }
        }

        public bool AdjustImageRGB 
        {
            get { return _adjustImageRGB; }
            set
            {
                if (value != _adjustImageRGB)
                {
                    _adjustImageRGB = value;
                    NotifyPropertyChanged();
                }
            }
        }

        public string OutputFilePath
        {
            get { return _outputFilePath; }
            set
            {
                if (_outputFilePath != value)
                {
                    _outputFilePath = value;
                    NotifyPropertyChanged();
                    NotifyPropertyChanged("ProcessToolTip");
                    NotifyPropertyChanged("ProcessEnabled");
                }
            }
        }

        public string ProcessToolTip
        {
            get
            {
                if (BankImagePaths.Count == 0)
                    return "Select Bank Images";
                else if (string.IsNullOrEmpty(RecreateImagePath))
                    return "Select Recreate Image";
                else if (string.IsNullOrEmpty(OutputFilePath))
                    return "Select Output File Path";
                else
                    return string.Empty;
            }
        }

        public bool ProcessEnabled
        {
            get { return string.IsNullOrEmpty(ProcessToolTip); }
        }

        public ObservableCollection<string> BankImagePaths { get; set; } = new ObservableCollection<string>();
        public ICommand GetImageBankCommand => new DelegateCommand((obj) => GetImageBank());
        public ICommand GetRecreateImageCommand => new DelegateCommand((obj) => GetRecreateImage());
        public ICommand SelectOutputFileCommand => new DelegateCommand((obj) => SelectOutputFile());
        public ICommand ProcessFinalImageCommand => new DelegateCommand((obj) => ProcessFinalImage());

        public MainWindowVM()
        {
            BankImagePaths.CollectionChanged += BankImagePaths_CollectionChanged;
        }


        ~MainWindowVM()
        {
            BankImagePaths.CollectionChanged -= BankImagePaths_CollectionChanged;
        }

        private void BankImagePaths_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            NotifyPropertyChanged("BankImagePathsPopulated");
            NotifyPropertyChanged("ProcessToolTip");
            NotifyPropertyChanged("ProcessEnabled");

        }


        private void GetImageBank()
        {
            var bank = new ImageBankWindow();

            bank.UpdateAddedPaths(BankImagePaths);

            bank.ShowDialog();
            var vm = bank.GetViewModel();

            if (!vm.ProceedWithChanges)
                return;

            BankImagePaths.Clear();

            foreach (var path in vm.AddedImagePaths.Select(e => e.ImagePath))
            {
                BankImagePaths.Add(path);
            }
        }
        private void GetRecreateImage()
        {
            var dlg = new Microsoft.Win32.OpenFileDialog();

            // Set filter for file extension and default file extension 
            dlg.Filter = "JPEG Files (*.jpeg)|*.jpeg|PNG Files (*.png)|*.png|JPG Files (*.jpg)|*.jpg|GIF Files (*.gif)|*.gif";

            // Get the selected file name and display in a TextBox 
            if (dlg.ShowDialog() == true)
            {
                // Open document 
                RecreateImagePath = dlg.FileName;

                using (var bit = new Bitmap(RecreateImagePath))
                {
                    RecreateImageDimensions = new ImageDimensions(bit.Height, bit.Width);
                }
            }
        }

        private void SelectOutputFile()
        {
            SaveFileDialog saveFileDialog1 = new SaveFileDialog();

            saveFileDialog1.Filter = "jpeg files (*.jpeg)|*.jpeg";
            saveFileDialog1.FilterIndex = 2;
            saveFileDialog1.RestoreDirectory = true;

            if (saveFileDialog1.ShowDialog() == true)
            {
                OutputFilePath = saveFileDialog1.FileName;
            }
        }

        private void ProcessFinalImage()
        {
            if (!ProcessEnabled)
                return;

            var model = new RenderingModel(RecreateImagePath, BankImagePaths, new RenderingOptionsModel(SamplePlotDimensions, ReplacementPlotDimensions, AdjustImageRGB, PlotProcessing), OutputFilePath);

            var render = new RenderProcessWindow(model);
            render.ShowDialog();
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
