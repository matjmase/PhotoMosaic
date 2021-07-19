using PhotoMosaic.Common;
using PhotoMosaic.Common.Extensions;
using PhotoMosaic.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace PhotoMosaic.Windows
{
    public class RenderProcessWindowVM : INotifyPropertyChanged
    {
        private const int _maxThreads = 8;
        private RenderingModel _model;
        private double _preProcessingPercentage;
        private double _renderingPercentage;
        private TimeSpan _preProcessingRemainTime;
        private TimeSpan _renderingRemainTime;
        private bool _processCanceled;
        private bool _exceptionThrown;
        private string _exceptionThrownMessage;

        public double PreProcessingPercentage
        {
            get { return _preProcessingPercentage; }
            set
            {
                if (value != _preProcessingPercentage)
                {
                    _preProcessingPercentage = value;
                    NotifyPropertyChanged();
                }
            }
        }

        public double RenderingPercentage
        {
            get { return _renderingPercentage; }
            set
            {
                if (value != _renderingPercentage)
                {
                    _renderingPercentage = value;
                    NotifyPropertyChanged();
                }
            }
        }

        public TimeSpan PreProcessingRemainTime
        {
            get { return _preProcessingRemainTime; }
            set
            {
                if (value != _preProcessingRemainTime)
                {
                    _preProcessingRemainTime = value;
                    NotifyPropertyChanged();
                }
            }
        }

        public TimeSpan RenderingRemainTime
        {
            get { return _renderingRemainTime; }
            set
            {
                if (value != _renderingRemainTime)
                {
                    _renderingRemainTime = value;
                    NotifyPropertyChanged();
                }
            }
        }

        public ICommand CancelCommand => new DelegateCommand((obj) => 
        { 
            _processCanceled = true;
            FindAndCloseWindow();
        });

        public RenderProcessWindowVM(RenderingModel model)
        {
            _model = model;
            Render();
        }

        private async void Render()
        {
            if (_model.Options.LoadThumbnails)
                await Task.Run(CompileFinalImageLoadThumbnails);
            else
                await Task.Run(CompileFinalImage);
            FindAndCloseWindow();
        }

        private void FindAndCloseWindow()
        {
            foreach (Window window in Application.Current.Windows)
            {
                if (object.ReferenceEquals(window.DataContext, this))
                {
                    window.Close();
                    return;
                }
            }
        }

        private void CompileFinalImage()
        {
            var preProcessed = PreProcess();

            if (_processCanceled || _exceptionThrown)
                return;


            var pool = new Semaphore(0, _maxThreads);
            var timer = new Stopwatch();
            timer.Start();

            var finalReferenceLock = new object();
            var outputBitmapLock = new object();

            Bitmap initialReference = null;
            Bitmap finalReference = null;
            Bitmap outputBitmap = null;

            try
            {

                initialReference = new Bitmap(_model.RecreateImage);
                var referenceDimensions = new ImageDimensions(initialReference.Height, initialReference.Width).RoundUp(_model.Options.SamplePlotSize);

                finalReference = ResizeBitmap(initialReference, referenceDimensions.Height, referenceDimensions.Width);
                var widthPlots = referenceDimensions.Width / _model.Options.SamplePlotSize;
                var heightPlots = referenceDimensions.Height / _model.Options.SamplePlotSize;

                var totalPlots = widthPlots * heightPlots;
                var progessCount = 1;

                var finalWidth = widthPlots * _model.Options.FinalPlotSize;
                var finalHeight = heightPlots * _model.Options.FinalPlotSize;

                var tasks = new List<Task>();

                outputBitmap = new Bitmap(finalWidth, finalHeight);
                for (var i = 0; i < widthPlots; i++)
                {
                    for (var j = 0; j < heightPlots; j++)
                    {
                        var ii = i;
                        var jj = j;
                        tasks.Add(Task.Run(() =>
                        {
                            pool.WaitOne();
                            Bitmap thumbnail = null;

                            try
                            {

                                if (!(_processCanceled || _exceptionThrown))
                                {

                                    var avgPixel = new ColorDouble();
                                    lock (finalReferenceLock)
                                    {
                                        avgPixel = CalculateAveragePixel(finalReference, ii * _model.Options.SamplePlotSize, jj * _model.Options.SamplePlotSize, _model.Options.SamplePlotSize, _model.Options.SamplePlotSize);
                                    }

                                    var minDistImage = preProcessed.MinOrDefault(e => avgPixel.CalculateDistance(e.Item2));

                                    thumbnail = RenderThumbnailBitmap(minDistImage.Item1);

                                    if (_model.Options.AdjustRGB)
                                    {
                                        ColorAdjustImage(thumbnail, avgPixel, minDistImage.Item2);
                                    }

                                }

                                if (!(_processCanceled || _exceptionThrown))
                                {
                                    lock (outputBitmapLock)
                                    {
                                        DrawAtLocation(outputBitmap, thumbnail, ii * _model.Options.FinalPlotSize, jj * _model.Options.FinalPlotSize);
                                        RenderingPercentage = progessCount / (double)totalPlots * 100;
                                        RenderingRemainTime = timer.Elapsed / progessCount * (totalPlots - progessCount);
                                        progessCount++;
                                    }
                                }
                            }
                            catch (Exception e)
                            {
                                _exceptionThrown = true;
                                _exceptionThrownMessage = e.Message;
                            }
                            finally
                            {
                                thumbnail?.Dispose();
                            }

                            pool.Release();
                        }));

                    }
                }

                pool.Release(_maxThreads);

                foreach (var task in tasks)
                {
                    task.Wait();
                }

                if (!_processCanceled && !_exceptionThrown)
                {
                    outputBitmap.Save(_model.SavePath, ImageFormat.Jpeg);
                }
            }
            catch (Exception e)
            {
                _exceptionThrown = true;
                _exceptionThrownMessage = e.Message;
            }
            finally
            {
                initialReference?.Dispose();
                finalReference?.Dispose();
                outputBitmap?.Dispose();
            }

            if (_exceptionThrown)
            {
                MessageBox.Show(_exceptionThrownMessage);
            }
        }

        private void CompileFinalImageLoadThumbnails()
        {
            var preProcessed =  PreProcessLoadThumbnails();

            if (_processCanceled || _exceptionThrown)
                return;


            var pool = new Semaphore(0, _maxThreads);
            var timer = new Stopwatch();
            timer.Start();

            var preProcessedReferenceLock = new object();
            var finalReferenceLock = new object();
            var outputBitmapLock = new object();

            Bitmap initialReference = null;
            Bitmap finalReference = null;
            Bitmap outputBitmap = null;

            try
            {

                initialReference = new Bitmap(_model.RecreateImage);
                var referenceDimensions = new ImageDimensions(initialReference.Height, initialReference.Width).RoundUp(_model.Options.SamplePlotSize);

                finalReference = ResizeBitmap(initialReference, referenceDimensions.Height, referenceDimensions.Width);
                var widthPlots = referenceDimensions.Width / _model.Options.SamplePlotSize;
                var heightPlots = referenceDimensions.Height / _model.Options.SamplePlotSize;

                var totalPlots = widthPlots * heightPlots;
                var progessCount = 1;

                var finalWidth = widthPlots * _model.Options.FinalPlotSize;
                var finalHeight = heightPlots * _model.Options.FinalPlotSize;

                var tasks = new List<Task>();

                outputBitmap = new Bitmap(finalWidth, finalHeight);
                for (var i = 0; i < widthPlots; i++)
                {
                    for (var j = 0; j < heightPlots; j++)
                    {
                        var ii = i;
                        var jj = j;
                        tasks.Add(Task.Run(() =>
                        {
                            pool.WaitOne();
                            Bitmap thumbnail = null;

                            try
                            {

                                if (!(_processCanceled || _exceptionThrown))
                                {

                                    var avgPixel = new ColorDouble();
                                    lock (finalReferenceLock)
                                    {
                                        avgPixel = CalculateAveragePixel(finalReference, ii * _model.Options.SamplePlotSize, jj * _model.Options.SamplePlotSize, _model.Options.SamplePlotSize, _model.Options.SamplePlotSize);
                                    }

                                    var minDistImage = preProcessed.MinOrDefault(e => avgPixel.CalculateDistance(e.Item2));

                                    lock (preProcessedReferenceLock)
                                    {
                                        thumbnail = (Bitmap)minDistImage.Item1.Clone();
                                    }

                                    if (_model.Options.AdjustRGB)
                                    {
                                        ColorAdjustImage(thumbnail, avgPixel, minDistImage.Item2);
                                    }

                                }

                                if (!(_processCanceled || _exceptionThrown))
                                {
                                    lock (outputBitmapLock)
                                    {
                                        DrawAtLocation(outputBitmap, thumbnail, ii * _model.Options.FinalPlotSize, jj * _model.Options.FinalPlotSize);
                                        RenderingPercentage = progessCount / (double)totalPlots * 100;
                                        RenderingRemainTime = timer.Elapsed / progessCount * (totalPlots - progessCount);
                                        progessCount++;
                                    }
                                }
                            }
                            catch (Exception e)
                            {
                                _exceptionThrown = true;
                                _exceptionThrownMessage = e.Message;
                            }
                            finally
                            {
                                thumbnail?.Dispose();
                            }
                            pool.Release();
                        }));

                    }
                }

                pool.Release(_maxThreads);

                foreach (var task in tasks)
                {
                    task.Wait();
                }

                if (!_processCanceled && !_exceptionThrown)
                {
                    outputBitmap.Save(_model.SavePath, ImageFormat.Jpeg);
                }
            }
            catch (Exception e)
            {
                _exceptionThrown = true;
                _exceptionThrownMessage = e.Message;
            }
            finally
            {
                initialReference?.Dispose();
                finalReference?.Dispose();
                outputBitmap?.Dispose();
                foreach (var tuple in preProcessed)
                    tuple.Item1?.Dispose();
            }

            if (_exceptionThrown)
            {
                MessageBox.Show(_exceptionThrownMessage);
            }
        }

        private Tuple<string, ColorDouble>[] PreProcess()
        {
            var preProcessedList = new List<Tuple<string, ColorDouble>>();
            var timer = new Stopwatch();
            timer.Start();
            var progresscount = 1;

            Bitmap thumbnail = null;
            var total = _model.BankImages.Count();
            foreach (var filePath in _model.BankImages)
            {
                try
                {
                    if (!(_processCanceled || _exceptionThrown))
                    {
                        PreProcessingPercentage = progresscount / (double)total * 100;
                        PreProcessingRemainTime = timer.Elapsed / progresscount * (total - progresscount);
                        progresscount++;

                        thumbnail = RenderThumbnailBitmap(filePath);

                        var preProcessColorDouble = CalculateAveragePixel(thumbnail);

                        preProcessedList.Add(new Tuple<string, ColorDouble>(filePath, preProcessColorDouble));
                    }
                }
                catch (Exception e)
                {
                    _exceptionThrown = true;
                    _exceptionThrownMessage = e.Message;
                }
                finally
                {
                    thumbnail?.Dispose();
                }
            }

            return preProcessedList.ToArray();
        }


        private Tuple<Bitmap, ColorDouble>[] PreProcessLoadThumbnails()
        {
            var preProcessedList = new List<Tuple<Bitmap, ColorDouble>>();
            var timer = new Stopwatch();
            timer.Start();
            var progresscount = 1;

            Bitmap thumbnail = null;
            var total = _model.BankImages.Count();
            foreach (var filePath in _model.BankImages)
            {
                try
                {
                    if (!(_processCanceled || _exceptionThrown))
                    {
                        PreProcessingPercentage = progresscount / (double)total * 100;
                        PreProcessingRemainTime = timer.Elapsed / progresscount * (total - progresscount);
                        progresscount++;

                        thumbnail = RenderThumbnailBitmap(filePath);

                        var preProcessColorDouble = CalculateAveragePixel(thumbnail);

                        preProcessedList.Add(new Tuple<Bitmap, ColorDouble>(thumbnail, preProcessColorDouble));
                    }
                }
                catch (Exception e)
                {
                    _exceptionThrown = true;
                    _exceptionThrownMessage = e.Message;
                }
                finally
                {
                    if (_processCanceled || _exceptionThrown)
                    {
                        thumbnail?.Dispose();
                        foreach (var thumb in preProcessedList)
                        {
                            thumb?.Item1?.Dispose();
                        }
                    }
                }
            }

            return preProcessedList.ToArray();
        }

        private Bitmap RenderThumbnailBitmap(string imagePath)
        {
            var initialImage = new Bitmap(imagePath);

            Bitmap outputImage;

            switch (_model.Options.PlotProcessing)
            {
                case PlotImageProcessing.Stretch:
                    {
                        outputImage = ResizeBitmap(initialImage, _model.Options.FinalPlotSize, _model.Options.FinalPlotSize);
                        break;
                    }
                case PlotImageProcessing.Crop:
                    {
                        var croppedBitmap = CropSquareBitmapBitmap(initialImage);

                        outputImage = ResizeBitmap(croppedBitmap, _model.Options.FinalPlotSize, _model.Options.FinalPlotSize);

                        croppedBitmap.Dispose();
                        break;
                    }
                default:
                    {
                        throw new NotImplementedException("Not implement plot processing method");
                    }
            }

            initialImage.Dispose();

            return outputImage;
        }

        private ColorDouble CalculateAveragePixel(Bitmap image)
        {
            return CalculateAveragePixel(image, 0, 0, image.Width, image.Height);
        }

        private ColorDouble CalculateAveragePixel(Bitmap image, int x, int y, int width, int height)
        {
            var outputPixel = new ColorDouble();

            var count = 0;

            for (var i = 0; i < width; i++)
            {
                for (var j = 0; j < height; j++)
                {
                    var pixel = image.GetPixel(x + i, y + j);

                    outputPixel.R = outputPixel.R / (count + 1) * count + pixel.R / (double)(count + 1);
                    outputPixel.G = outputPixel.G / (count + 1) * count + pixel.G / (double)(count + 1);
                    outputPixel.B = outputPixel.B / (count + 1) * count + pixel.B / (double)(count + 1);

                    count++;
                }
            }

            return outputPixel;
        }

        private void ColorAdjustImage(Bitmap image, ColorDouble desiredAvg, ColorDouble currentAvg)
        {
            var multiplier = desiredAvg / currentAvg;

            for (var i = 0; i < image.Width; i++)
            {
                for (var j = 0; j < image.Height; j++)
                {
                    var currentPixel = ColorDouble.FromColor(image.GetPixel(i, j));

                    image.SetPixel(i, j, (currentPixel * multiplier).ToColor());
                }
            }
        }

        private void DrawAtLocation(Bitmap output, Bitmap reference, int x, int y)
        {
            Graphics g = Graphics.FromImage((System.Drawing.Image)output);
            // Draw image with new width and height  
            g.DrawImage(reference, x, y, reference.Width, reference.Height);
            g.Dispose();
        }

        private Bitmap ResizeBitmap(Bitmap oldBitmap, int height, int width)
        {
            Bitmap newBitmap = new Bitmap(width, height);
            Graphics g = Graphics.FromImage((System.Drawing.Image)newBitmap);
            g.CompositingMode = CompositingMode.SourceCopy;
            g.CompositingQuality = CompositingQuality.HighQuality;
            g.SmoothingMode = SmoothingMode.HighQuality;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            // Draw image with new width and height  
            g.DrawImage(oldBitmap, 0, 0, width, height);
            g.Dispose();

            return newBitmap;
        }

        private Bitmap CropSquareBitmapBitmap(Bitmap oldBitmap)
        {
            var smaller = oldBitmap.Height > oldBitmap.Width ? oldBitmap.Width : oldBitmap.Height;

            Rectangle cropRect = new Rectangle(new System.Drawing.Point(oldBitmap.Width / 2 - smaller / 2, oldBitmap.Height / 2 - smaller / 2), new System.Drawing.Size(smaller, smaller));
            Bitmap target = new Bitmap(cropRect.Width, cropRect.Height);

            using (Graphics g = Graphics.FromImage(target))
            {
                g.DrawImage(oldBitmap, new Rectangle(0, 0, target.Width, target.Height),
                                 cropRect,
                                 GraphicsUnit.Pixel);
            }
            return target;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
