namespace PluginTwitchChat
{
    public interface IPositioned
    {
        double X { get; set; }
        double Y { get; set; }
        double Width { get; set; }
        double Height { get; set; }

        IPositioned Copy();
    }

    public class Positioned
    {
        public static void CopyPosition(IPositioned to, IPositioned from)
        {
            to.X = from.X;
            to.Y = from.Y;
            to.Width = from.Width;
            to.Height = from.Height;
        }
    }
}
