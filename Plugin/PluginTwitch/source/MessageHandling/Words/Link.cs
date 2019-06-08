namespace PluginTwitchChat
{
    public class Link : Word, IPositioned
    {
        public string Url;

        public double X { get; set; }
        public double Y { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }
        public Link(string content, int start, int len, StringMeasurer measurer) : this(content.Substring(start, len), measurer) { }

        public Link(string url, StringMeasurer measurer) : this(url, url, measurer) { }

        public Link(string url, string str, StringMeasurer measurer) : base(str)
        {
            Url = url;
            var size = measurer.MeasureString(str);
            Width = size.Width;
            Height = size.Height;
        }

        public Link(Link link): base(link.String)
        {
            Url = Url;
            Positioned.CopyPosition(this, link);
        }

        public IPositioned Copy()
        {
            return new Link(this);
        }

        public override string ToString()
        {
            return string.Format("Link({0})", Url);
        }
    }
}
