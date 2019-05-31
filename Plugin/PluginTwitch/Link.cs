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

        public Link(string url) : this(url, url) { }
        public Link(string s, int start, int len) : this(s.Substring(start, len)) { }

        public Link(string url, string str) : base(str)
        {
            this.Url = url;
        }

        public override string ToString()
        {
            return string.Format("Link({0})", Url);
        }
    }
}
