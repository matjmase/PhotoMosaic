using PhotoMosaic.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PhotoMosaic.Models
{
    public class RenderingOptionsModel
    {
        public PlotImageProcessing PlotProcessing { get; private set; }
        public bool AdjustRGB { get; private set; }
        public int SamplePlotSize { get; private set; }
        public int FinalPlotSize { get; private set; }
        public RenderingOptionsModel(int samplePlotSize, int finalPlotSize, bool adjustRGB, PlotImageProcessing plotProcessing)
        {
            SamplePlotSize = samplePlotSize;
            FinalPlotSize = finalPlotSize;
            AdjustRGB = adjustRGB;
            PlotProcessing = plotProcessing;
        }
    }
}
