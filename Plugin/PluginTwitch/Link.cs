using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PluginTwitchChat
{
    public class Link : Word
    {

        public string Url;
        public string Str;
        public int X;
        public int Y;
        public int Width;
        public int Height;

        public Link(string url) : this(url, url) { }
        public Link(string s, int start, int len) : this(s.Substring(start, len)) { }

        public Link(string url, string str) : base(str)
        {
            this.Url = url;
            this.X = 0;
            this.Y = 0;
            this.Width = 0;
            this.Height = 0;
        }

        public override string ToString()
        {
            return string.Format("Link({0})", Url);
        }
    }
}
