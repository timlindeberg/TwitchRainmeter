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
        public string Text { get ; private set; }
        public bool IsEmpty { get { return Text == string.Empty; } }
        public Line()
        {
            Text = string.Empty;
            Images = new List<Image>();
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
        }

        public override string ToString()
        {
            return Text;
        }
    }
}
