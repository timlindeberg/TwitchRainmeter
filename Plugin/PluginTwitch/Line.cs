using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PluginTwitchChat
{
    public class Line
    {
        public List<Image> Images;
        public List<Link> Links;
        public string Text { get ; private set; }
        public bool IsEmpty { get { return Text == string.Empty; } }
        public Line()
        {
            Text = string.Empty;
            Images = new List<Image>();
            Links = new List<Link>();
        }

        public void Add(String s)
        {
            Add(new Word(s));
        }

        public void Add(Word w)
        {
            Text += (Text == string.Empty) ? w : ' ' + w;

            if(w is Image)
                Images.Add(w as Image);
            else if (w is Link)
                Links.Add(w as Link);
        }

        public override string ToString()
        {
            return Text;
        }
    }
}
