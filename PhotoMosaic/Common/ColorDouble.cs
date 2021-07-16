using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PhotoMosaic.Common
{
    public class ColorDouble
    {
        public double R;
        public double G;
        public double B;

        public double CalculateDistance(ColorDouble other)
        {
            return Math.Sqrt(Math.Pow(R - other.R, 2) + Math.Pow(G - other.G, 2) + Math.Pow(B - other.B, 2));
        }

        public Color ToColor()
        {
            return Color.FromArgb((byte)Math.Clamp(R, byte.MinValue, byte.MaxValue), (byte)Math.Clamp(G, byte.MinValue, byte.MaxValue), (byte)Math.Clamp(B, byte.MinValue, byte.MaxValue));
        }

        public static ColorDouble FromColor(Color inputColor)
        {
            return new ColorDouble() { R = inputColor.R, G = inputColor.G, B = inputColor.B };
        }

        public static ColorDouble operator -(ColorDouble arg1, ColorDouble arg2)
        {
            return new ColorDouble() { R = arg1.R - arg2.R, G = arg1.G - arg2.G, B = arg1.B - arg2.B };
        }
        public static ColorDouble operator +(ColorDouble arg1, ColorDouble arg2)
        {
            return new ColorDouble() { R = arg1.R + arg2.R, G = arg1.G + arg2.G, B = arg1.B + arg2.B };
        }
        public static ColorDouble operator /(ColorDouble arg1, ColorDouble arg2)
        {
            return new ColorDouble() { R = arg1.R / arg2.R, G = arg1.G / arg2.G, B = arg1.B / arg2.B };
        }
        public static ColorDouble operator *(ColorDouble arg1, ColorDouble arg2)
        {
            return new ColorDouble() { R = arg1.R * arg2.R, G = arg1.G * arg2.G, B = arg1.B * arg2.B };
        }
    }
}
