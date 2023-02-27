using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImagePixel
{
    public class Pixel
    {
        public Pixel(Point point, Color color)
        {
            Point = point;
            Color = color;
        }
    
        public Point Point { get; set; }
        public Color Color { get; set; }
    }
}
