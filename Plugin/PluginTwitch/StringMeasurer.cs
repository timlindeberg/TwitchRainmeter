using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;

namespace PluginTwitchChat
{
    public class StringMeasurer
    {
        public Font Font { get; private set;}
        private Graphics graphics;
        private StringFormat format;

        public StringMeasurer(Font font)
        {
            Font = font;
            graphics = Graphics.FromImage(new Bitmap(1, 1));
            graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;
            format = new StringFormat(StringFormat.GenericTypographic) { FormatFlags = StringFormatFlags.MeasureTrailingSpaces };
        }

        public double GetWidth(string s)
        {
            return MeasureString(s).Width;
        }

        public double GetHeight(string s)
        {
            return MeasureString(s).Height;
        }

        public SizeF MeasureString(string s)
        {
            return graphics.MeasureString(s, Font, 10000, format);
        }

    }
}
