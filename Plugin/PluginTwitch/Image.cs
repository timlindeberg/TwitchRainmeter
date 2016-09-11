using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PluginTwitchChat
{
    public class Image : Word, Positioned
    {

        protected string _name;
        public virtual string Name { get { return _name; } private set { _name = value; } }
        public string DisplayName;

        public float X { get; set; }
        public float Y { get; set; }
        public float Width { get; set; }
        public float Height { get; set; }

        public Image(string name, string displayName, string imageString) : base(imageString)
        {
            this._name = name;
            this.DisplayName = displayName;
        }
    }
}
