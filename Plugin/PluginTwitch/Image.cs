using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PluginTwitchChat
{
    public class Image : Word
    {

        public static string ImageString = null;
        public string Name;
        public string DisplayName;
        public int X;
        public int Y;

        public Image(string name, string displayName) : base(ImageString)
        {
            this.Name = name;
            this.DisplayName = displayName;
            this.X = 0;
            this.Y = 0;
        }

        public override string ToString()
        {
            return string.Format("Image({0})", DisplayName);
        }
    }
}
