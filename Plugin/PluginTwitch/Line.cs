using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PluginTwitch
{
    class Line
    {
        public List<Image> Images;
        public string Text { get ; private set; }

        public Line()
        {
            Text = string.Empty;
            Images = new List<Image>();
        }

        public void Add(Word w)
        {
            if (Text == string.Empty)
                Text = w.String;
            else
                Text += " " + w.String;

            if(w is Image)
                Images.Add(w as Image);
        }

        public override string ToString()
        {
            return Text;
        }
    }
}
