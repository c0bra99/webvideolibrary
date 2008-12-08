using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;

namespace WebVideoLibrary
{
    /// <summary>
    /// This class will handle calculating the Dominant Color for a set of frames in a video.
    /// </summary>
    class DominantColorCalculator
    {
        public enum DominantColor
        {
            Red,
            Green,
            Blue,
            Black,
            White
        }

        private int _height;
        private int _width;
        private long _totalR;
        private long _totalG;
        private long _totalB;


        /// <summary>
        /// Default Constructor
        /// </summary>
        public DominantColorCalculator(int height, int width)
        {
            _height = height;
            _width = width;
            _totalR = 0;
            _totalG = 0;
            _totalB = 0;
        }


        /// <summary>
        /// Private constructor taking all possible parameters
        /// </summary>
        private DominantColorCalculator(int height, int width, long r, long g, long b)
        {
            _height = height;
            _width = width;
            _totalR = r;
            _totalG = g;
            _totalB = b;
        }


        /// <summary>
        /// Adds up the colors for use in the calculation for Dominant Color
        /// </summary>
        public void AddFrame(Bitmap bmp)
        {
            //use a using statement so it will Dispose of the Bitmap when it is done automatically
            for (int x = 0; x < bmp.Width; x++)
            {
                for (int y = 0; y < bmp.Height; y++)
                {
                    Color color = bmp.GetPixel(x, y);
                    _totalR += color.R;
                    _totalG += color.G;
                    _totalB += color.B;
                }
            }
        }


        /// <summary>
        /// Gets the Dominant color for the given image
        /// </summary>
        public DominantColor GetDominantColor()
        {
            if (_totalR > _totalG && _totalR > _totalB)
            {
                return DominantColor.Red;
            }
            else if (_totalG > _totalR && _totalG > _totalB)
            {
                return DominantColor.Green;
            }
            else if (_totalR + _totalG + _totalB == 0)
            {
                return DominantColor.Black;
            }
            else if ((_totalR + _totalG + _totalB) == _height * _width * 255)
            {
                return DominantColor.White;
            }
            else
            {
                return DominantColor.Blue;
            }
        }


        /// <summary>
        /// Adds 2 DominantColorCalculators color buckets together and returns a new DominantColorCalculator
        /// </summary>
        public static DominantColorCalculator Add(DominantColorCalculator calc1, DominantColorCalculator calc2)
        {
            return new DominantColorCalculator(calc1._width, calc1._height, calc1._totalR + calc2._totalR, calc1._totalG + calc2._totalG, calc1._totalB + calc2._totalB);
        }
    }
}
