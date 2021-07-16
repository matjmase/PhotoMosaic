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

        public RenderProcessWindowVM(RenderingModel model)
        {
            _model = model;
            Render();
        }

        private async void Render()
        {
            await Task.Run(CompileFinalImage);
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

            var finalReferenceLock = new object();
            var outputBitmapLock = new object();

            var pool = new Semaphore(0, _maxThreads);
            var timer = new Stopwatch();
            timer.Start();

            using (var initialReference = new Bitmap(_model.RecreateImage))
            {
                var referenceDimensions = new ImageDimensions(initialReference.Height, initialReference.Width).RoundUp(_model.Options.SamplePlotSize);

                using (var finalReference = ResizeBitmap(initialReference, referenceDimensions.Height, referenceDimensions.Width))
                {
                    var widthPlots = referenceDimensions.Width / _model.Options.SamplePlotSize;
                    var heightPlots = referenceDimensions.Height / _model.Options.SamplePlotSize;

                    var totalPlots = widthPlots * heightPlots;
                    var progessCount = 1;

                    var finalWidth = widthPlots * _model.Options.FinalPlotSize;
                    var finalHeight = heightPlots * _model.Options.FinalPlotSize;

                    var tasks = new List<Task>();

                    using (var outputBitmap = new Bitmap(finalWidth, finalHeight))
                    {
                        for (var i = 0; i < widthPlots; i++)
                        {
                            for (var j = 0; j < heightPlots; j++)
                            {
                                var ii = i;
                                var jj = j;
                                tasks.Add(Task.Run(() =>
                                {
                                    pool.WaitOne();

                                    var avgPixel = new ColorDouble();
                                    var count = 0;
                                    for (var x = 0; x < _model.Options.SamplePlotSize; x++)
                                    {
                                        for (var y = 0; y < _model.Options.SamplePlotSize; y++)
                                        {
                                            System.Drawing.Color pixel;

                                            lock (finalReferenceLock)
                                            {
                                                pixel = finalReference.GetPixel(ii * _model.Options.SamplePlotSize + x, jj * _model.Options.SamplePlotSize + y);
                                            }

                                            avgPixel.R = avgPixel.R / (count + 1) * count + pixel.R / (double)(count + 1);
                                            avgPixel.G = avgPixel.G / (count + 1) * count + pixel.G / (double)(count + 1);
                                            avgPixel.B = avgPixel.B / (count + 1) * count + pixel.B / (double)(count + 1);
                                            count++;
                                        }
                                    }

                                    var minDistImage = preProcessed.MinOrDefault(e => avgPixel.CalculateDistance(e.Item2));

                                    using (var plotImage = new Bitmap(minDistImage.Item1))
                                    {
                                        switch (_model.Options.PlotProcessing)
                                        {
                                            case PlotImageProcessing.Stretch:
                                                {
                                                    using (var selectedPlot = ResizeBitmap(plotImage, _model.Options.FinalPlotSize, _model.Options.FinalPlotSize))
                                                    {
                                                        if (_model.Options.AdjustRGB)
                                                        {
                                                            var multiplier = avgPixel / minDistImage.Item2;

                                                            for (var i = 0; i < selectedPlot.Width; i++)
                                                            {
                                                                for (var j = 0; j < selectedPlot.Height; j++)
                                                                {
                                                                    var currentPixel = ColorDouble.FromColor(selectedPlot.GetPixel(i, j));

                                                                    selectedPlot.SetPixel(i, j, (currentPixel * multiplier).ToColor());
                                                                }
                                                            }
                                                        }

                                                        lock (outputBitmapLock)
                                                        {
                                                            DrawAtLocation(outputBitmap, selectedPlot, ii * _model.Options.FinalPlotSize, jj * _model.Options.FinalPlotSize);
                                                            RenderingPercentage = progessCount / (double)totalPlots * 100;
                                                            RenderingRemainTime = timer.Elapsed / progessCount * (totalPlots - progessCount);
                                                            progessCount++;
                                                        }
                                                    }
                                                    break;
                                                }
                                            case PlotImageProcessing.Crop:
                                                {
                                                    using (var cropedBitmap = CropSquareBitmapBitmap(plotImage))
                                                    using (var selectedPlot = ResizeBitmap(cropedBitmap, _model.Options.FinalPlotSize, _model.Options.FinalPlotSize))
                                                    {
                                                        if (_model.Options.AdjustRGB)
                                                        {
                                                            var multiplier = avgPixel / minDistImage.Item2;

                                                            for (var i = 0; i < selectedPlot.Width; i++)
                                                            {
                                                                for (var j = 0; j < selectedPlot.Height; j++)
                                                                {
                                                                    var currentPixel = ColorDouble.FromColor(selectedPlot.GetPixel(i, j));

                                                                    selectedPlot.SetPixel(i, j, (currentPixel * multiplier).ToColor());
                                                                }
                                                            }
                                                        }

                                                        lock (outputBitmapLock)
                                                        {
                                                            DrawAtLocation(outputBitmap, selectedPlot, ii * _model.Options.FinalPlotSize, jj * _model.Options.FinalPlotSize);
                                                            RenderingPercentage = progessCount / (double)totalPlots * 100;
                                                            RenderingRemainTime = timer.Elapsed / progessCount * (totalPlots - progessCount);
                                                            progessCount++; 
                                                        }
                                                    }
                                                    break;
                                                }
                                            default:
                                                {
                                                    throw new NotImplementedException("Not implement plot processing method");
                                                }
                                        }
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

                        outputBitmap.Save(_model.SavePath, ImageFormat.Jpeg);
                    }

                }

            }
        }

        private Tuple<string, ColorDouble>[] PreProcess()
        {
            var preProcessedList = new List<Tuple<string, ColorDouble>>();
            var timer = new Stopwatch();
            timer.Start();

            var progresscount = 1;
            var total = _model.BankImages.Count();
            foreach (var filePath in _model.BankImages)
            {
                PreProcessingPercentage = progresscount / (double)total * 100;
                PreProcessingRemainTime = timer.Elapsed / progresscount * (total - progresscount);
                progresscount++;

                using (var plotImage = new Bitmap(filePath))
                {
                    var preProcessColorDouble = new ColorDouble();

                    switch (_model.Options.PlotProcessing)
                    {
                        case PlotImageProcessing.Stretch:
                            {
                                using (var thumbnailBitmap = ResizeBitmap(plotImage, _model.Options.FinalPlotSize, _model.Options.FinalPlotSize))
                                {
                                    var count = 0;

                                    for (var i = 0; i < thumbnailBitmap.Width; i++)
                                    {
                                        for (var j = 0; j < thumbnailBitmap.Height; j++)
                                        {
                                            var pixel = thumbnailBitmap.GetPixel(i, j);

                                            preProcessColorDouble.R = preProcessColorDouble.R / (count + 1) * count + pixel.R / (double)(count + 1);
                                            preProcessColorDouble.G = preProcessColorDouble.G / (count + 1) * count + pixel.G / (double)(count + 1);
                                            preProcessColorDouble.B = preProcessColorDouble.B / (count + 1) * count + pixel.B / (double)(count + 1);

                                            count++;
                                        }
                                    }
                                }
                                break;
                            }
                        case PlotImageProcessing.Crop:
                            {
                                using (var cropedBitmap = CropSquareBitmapBitmap(plotImage))
                                using (var thumbnailBitmap = ResizeBitmap(cropedBitmap, _model.Options.FinalPlotSize, _model.Options.FinalPlotSize))
                                {
                                    var count = 0;

                                    for (var i = 0; i < thumbnailBitmap.Width; i++)
                                    {
                                        for (var j = 0; j < thumbnailBitmap.Height; j++)
                                        {
                                            var pixel = thumbnailBitmap.GetPixel(i, j);

                                            preProcessColorDouble.R = preProcessColorDouble.R / (count + 1) * count + pixel.R / (double)(count + 1);
                                            preProcessColorDouble.G = preProcessColorDouble.G / (count + 1) * count + pixel.G / (double)(count + 1);
                                            preProcessColorDouble.B = preProcessColorDouble.B / (count + 1) * count + pixel.B / (double)(count + 1);

                                            count++;
                                        }
                                    }
                                }
                                break;
                            }
                        default:
                            {
                                throw new NotImplementedException("Not implement plot processing method");
                            }
                    }
                    preProcessedList.Add(new Tuple<string, ColorDouble>( filePath, preProcessColorDouble));
                }
            }

            return preProcessedList.ToArray();
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
                g.DrawImage(oldBitmap, new Rectangle(0,0, target.Width, target.Height),
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
