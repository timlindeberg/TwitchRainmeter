using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PluginTwitchChat
{
    public class Line
    {
        public List<Positioned> Positioned;
        public string Text { get ; private set; }
        public bool IsEmpty { get { return Text == string.Empty; } }

        private StringMeasurer measurer;
        public Line(StringMeasurer measurer)
        {
            Text = string.Empty;
            this.measurer = measurer;
            Positioned = new List<Positioned>();
        }

        public void Add(String s)
        {
            Add(new Word(s));
        }

        public void Add(Word w)
        {
            if(w is Positioned)
                Positioned.Add(w as Positioned);

            string t = w is Link ? CalculateSpaceString(w) : w;
            Text += (Text == string.Empty) ? t : ' ' + t;
        }

        public override string ToString()
        {
            return Text;
        }

        private string CalculateSpaceString(string url)
        {
            var spaceWidth = measurer.GetWidth(" ");
            var urlWidth = measurer.GetWidth(url);
            var width = spaceWidth;
            var sb = new StringBuilder();
            while (width < urlWidth)
            {
                width += spaceWidth;
                sb.Append(' ');
            }
            return sb.ToString();
        }
    }
}
