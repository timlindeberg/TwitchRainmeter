using Rainmeter;

namespace PluginTwitchChat
{
    public class Settings
    {
        public readonly string User;
        public readonly string Ouath;
        public readonly string FontFace;
        public readonly string ImageDir;

        public readonly int Width;
        public readonly int Height;
        public readonly int FontSize;
        public readonly int ChannelUpdateTime;
        public readonly int MaxViewerNames;
        public readonly int ImageQuality;
        public readonly double ImageScale;

        public readonly bool UseSeperator;
        public readonly bool UseBetterTTV;
        public readonly bool UseFrankerFacez;

        public readonly string ErrorMessage;

        public Settings(API rm)
        {
            User = rm.ReadString("Username", "").ToLower();
            Ouath = rm.ReadString("Ouath", "");
            FontFace = rm.ReadString("FontFace", "");
            ImageDir = rm.ReadString("ImageDir", "");
            Width = rm.ReadInt("Width", 0);
            Height = rm.ReadInt("Height", 0);
            FontSize = rm.ReadInt("FontSize", 0);
            ChannelUpdateTime = rm.ReadInt("ChannelUpdateTime", 0);
            MaxViewerNames = rm.ReadInt("MaxViewerNames", 0);
            ImageQuality = Clamp(rm.ReadInt("ImageQuality", 1), 1, 3);
            ImageScale = Clamp(rm.ReadDouble("ImageScale", 0.9), 0.0, 1.0);

            UseSeperator = rm.ReadInt("UseSeperator", 1) == 1;
            UseBetterTTV = rm.ReadInt("UseBetterTTVEmotes", 1) == 1;
            UseFrankerFacez = rm.ReadInt("UseFrankerFacezEmotes", 1) == 1;

            ErrorMessage = User == "" ? "User name is missing in settings files UserSettings.inc." :
                      /**/ Ouath == "" ? "Ouath is missing in settings files UserSettings.inc." :
                      /**/ FontFace == "" ? "Missing FontFace setting in Variables.inc." :
                      /**/ ImageDir == "" ? "Missing ImageDir setting Variables.inc." :
                      /**/ Width == 0 ? "Either Width setting in Variables.inc is missing or is zero." :
                      /**/ Height == 0 ? "Either Height setting in Variables.inc is missing or is zero." :
                      /**/ FontSize == 0 ? "Either FontSize setting in Variables.inc is missing or is zero." :
                      /**/ null;
        }

        private int Clamp(int v, int min, int max)
        {
            return v < min ? min : v > max ? max : v;
        }

        private double Clamp(double v, double min, double max)
        {
            return v < min ? min : v > max ? max : v;
        }
    }
}
