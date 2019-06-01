using System.Collections.Generic;
using System.Text;

namespace PluginTwitchChat
{
    public class Line
    {
        public List<IPositioned> Positioned;
        private string _Text;
        public string Text
        {
            get
            {
                if (_Text.Length != sb.Length)
                {
                    _Text = sb.ToString();
                }
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
            Positioned = new List<IPositioned>();
        }

        public void Add(string word)
        {
            Add(new Word(word));
        }

        public void Add(Word word)
        {
            if (word is IPositioned)
            {
                Positioned.Add(word as IPositioned);
            }

            var wordString = word is Link ? CalculateSpaceString(word) : word;
            sb.Append((sb.Length == 0) ? wordString : ' ' + wordString);
        }

        public override string ToString()
        {
            return Text;
        }

        public override bool Equals(object obj)
        {
            if (!(obj is Line))
            {
                return false;
            }

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
