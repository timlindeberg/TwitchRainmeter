using System;
using System.Text;
using System.Drawing;
using System.Runtime.InteropServices;

namespace PluginTwitchChat
{
    public class StringMeasurer : IDisposable
    {
        [StructLayout(LayoutKind.Sequential, Size = 64)]
        public struct Size
        {
            public float Width;
            public float Height;

            public override string ToString()
            {
                return string.Format("({0}, {1})", Width, Height);
            }
        }

        static StringMeasurer()
        {
            Qromodyn.EmbeddedDllClass.ExtractEmbeddedDlls("StringMeasurer.dll", Properties.Resources.StringMeasurer);
        }

        public Font Font { get; private set; }

        public StringMeasurer(Font font)
        {
            Font = font;
            var str = new StringBuilder(font.Name);
            InitializeMeasurer(str, (int)font.Size, font.Bold, font.Italic, true);
        }

        public float GetWidth(StringBuilder s)
        {
            return MeasureString(s).Width;
        }

        public float GetWidth(string s)
        {
            return MeasureString(s).Width;
        }

        public float GetHeight(StringBuilder s)
        {
            return MeasureString(s).Height;
        }

        public float GetHeight(string s)
        {
            return MeasureString(s).Height;
        }

        public Size MeasureString(StringBuilder s)
        {
            var size = new Size();
            GetTextSize(s, (uint)s.Length, ref size);
            return size;
        }

        public Size MeasureString(string s)
        {
            return MeasureString(new StringBuilder(s));
        }

        public void Dispose()
        {
            DisposeMeasurer();
        }

        [DllImport("StringMeasurer.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
        static private extern void InitializeMeasurer(StringBuilder fontFamily, int size, bool bold, bool italic, bool accurateText);

        [DllImport("StringMeasurer.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
        static private extern void GetTextSize(StringBuilder str, uint strLen, ref Size size);

        [DllImport("StringMeasurer.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
        static private extern void DisposeMeasurer();
    }

}
