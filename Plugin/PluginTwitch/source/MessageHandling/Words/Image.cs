namespace PluginTwitchChat
{
    public class Image : Word, IPositioned
    {
        protected string _name;
        public virtual string Name { get { return _name; } private set { _name = value; } }
        public string DisplayName;

        public double X { get; set; }
        public double Y { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }

        public Image(string name, string displayName, string imageString) : base(imageString)
        {
            _name = name;
            DisplayName = displayName;
        }

        public Image(Image image): base(image.String)
        {
            Positioned.CopyPosition(this, image);
            _name = image._name;
            DisplayName = image.DisplayName;
        }

        virtual public IPositioned Copy()
        {
            return new Image(this);
        }
    }
}
