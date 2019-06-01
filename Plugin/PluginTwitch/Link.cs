using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PluginTwitchChat
{
    public class Link : Word, Positioned
    {

        public string Url;

        public double X { get; set; }
        public double Y { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }
        public Link(string s, int start, int len, StringMeasurer measurer) : this(s.Substring(start, len), measurer) { }

        public Link(string url, StringMeasurer measurer) : this(url, url, measurer) { }

        public Link(string url, string str, StringMeasurer measurer) : base(str)
        {
            this.Url = url;
            var size = measurer.MeasureString(str);
            Width = size.Width;
            Height = size.Height;
        }

        public override string ToString()
        {
            return string.Format("Link({0})", Url);
        }
    }
}
