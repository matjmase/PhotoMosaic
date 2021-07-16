using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PhotoMosaic.Models
{
    public class RenderingModel
    {
        public IEnumerable<string> BankImages { get; private set; }
        public string RecreateImage { get; private set; }
        public RenderingOptionsModel Options { get; private set; }
        public string SavePath { get; private set; }

        public RenderingModel(string recreateImage, IEnumerable<string> bankImages, RenderingOptionsModel options, string savePath)
        {
            RecreateImage = recreateImage;
            BankImages = bankImages;
            Options = options;
            SavePath = savePath;
        }

    }
}
