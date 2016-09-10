using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PluginTwitchChat
{
    public class Image : Word, Positioned
    {

        public static string ImageString = null;
        public string Name;
        public string DisplayName;

        public float X { get; set; }
        public float Y { get; set; }
        public float Width { get; set; }
        public float Height { get; set; }

        public Image(string name, string displayName) : base(ImageString)
        {
            this.Name = name;
            this.DisplayName = displayName;
        }

        public override string ToString()
        {
            return string.Format("Image({0})", DisplayName);
        }
    }
}
