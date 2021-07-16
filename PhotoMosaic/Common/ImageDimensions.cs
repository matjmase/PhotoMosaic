using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PhotoMosaic.Common
{
    public struct ImageDimensions
    {
        public int Height { get; private set; }
        public int Width { get; private set; }

        public ImageDimensions(int height, int width)
        {
            Height = height;
            Width = width;
        }

        public ImageDimensions RoundUp(int mod)
        {
            return new ImageDimensions(RoundUp(Height, mod), RoundUp(Width, mod));
        }

        private int RoundUp(int number, int mod)
        {
            if (number % mod == 0)
                return number;
            else
                return number + (mod - number % mod);

        }
    }
}
