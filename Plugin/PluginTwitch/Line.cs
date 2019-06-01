using System.Collections.Generic;
using System.Text;

namespace PluginTwitchChat
{
    public class Line
    {
        public List<Positioned> Positioned;
        private string _Text;
        public string Text
        {
            get
            {
                if (_Text.Length != sb.Length)
                    _Text = sb.ToString();
                return _Text;
            }
        }
        public bool IsEmpty { get { return Text == string.Empty; } }

        private readonly StringMeasurer measurer;
        private readonly StringBuilder sb;

        public Line(StringMeasurer measurer)
        {
            _Text = string.Empty;
            sb = new StringBuilder();
            this.measurer = measurer;
            Positioned = new List<Positioned>();
        }

        public void Add(string s)
        {
            Add(new Word(s));
        }

        public void Add(Word w)
        {
            if (w is Positioned)
                Positioned.Add(w as Positioned);

            string t = w is Link ? CalculateSpaceString(w) : w;
            sb.Append((sb.Length == 0) ? t : ' ' + t);
        }

        public override string ToString()
        {
            return Text;
        }

        public override bool Equals(object obj)
        {
            if (!(obj is Line))
                return false;

            return (obj as Line)._Text == _Text;
        }

        public override int GetHashCode()
        {
            return _Text.GetHashCode();
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
