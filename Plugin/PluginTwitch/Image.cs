using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PluginTwitch
{
    public class Image : Word
    {

        public static string ImageString = null;
        public string Name;
        public int X;
        public int Y;
        public int start;
        public int end;

        public Image(string name, int start, int end) : base(ImageString)
        {
            this.Name = name;
            this.X = 0;
            this.Y = 0;
            this.start = start;
            this.end = end;
        }

        public override string ToString()
        {
            return string.Format("Image({0})", Name);
        }

    }
}
