using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Windows.Forms;
using System.Runtime.InteropServices;

namespace PluginTwitchChat
{
    public class StringMeasurer
    {
        public Font Font { get; private set;}
        private Graphics graphics;
        private StringFormat format;

        public StringMeasurer(Font font)
        {
            Application.SetCompatibleTextRenderingDefault(false);
            Font = font;
            graphics = Graphics.FromImage(new Bitmap(1, 1));
            graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;
            format = new StringFormat(StringFormat.GenericTypographic) { FormatFlags = StringFormatFlags.MeasureTrailingSpaces };
            //format.Alignment = StringAlignment.Center;
            //format.LineAlignment = StringAlignment.Center;
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
            if (s == "")
                return new SizeF(0, 0);

            RectangleF rect = new RectangleF(0, 0, 10000, 10000);
            format.SetMeasurableCharacterRanges(new [] { new CharacterRange(0, s.Length) });

            Region[] regions = graphics.MeasureCharacterRanges(s, Font, rect, format);
            rect = regions[0].GetBounds(graphics);
            return new SizeF((rect.Right + 1.0f),(rect.Bottom + 1.0f));
        }

    }

}
