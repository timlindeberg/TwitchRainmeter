using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

        public readonly bool UseSeperator;
        public readonly bool UseBetterTTV;

        public readonly string ErrorMessage;

        public Settings(API api)
        {
            User = api.ReadString("Username", "").ToLower();
            Ouath = api.ReadString("Ouath", "");
            FontFace = api.ReadString("FontFace", "");
            ImageDir = api.ReadString("ImageDir", "");
            Width = api.ReadInt("Width", 0);
            Height = api.ReadInt("Height", 0);
            FontSize = api.ReadInt("FontSize", 0);
            ChannelUpdateTime = api.ReadInt("ChannelUpdateTime", 0);
            MaxViewerNames = api.ReadInt("MaxViewerNames", 0);
            ImageQuality = Clamp(api.ReadInt("ImageQuality", 1), 1, 3);

            UseSeperator = api.ReadInt("UseSeperator", 1) == 1;
            UseBetterTTV = api.ReadInt("UseBetterTTVEmotes", 1) == 1;

            ErrorMessage = User == ""     ? "User name is missing in settings files UserSettings.inc." :
                      /**/ Ouath == ""    ? "Ouath is missing in settings files UserSettings.inc." :
                      /**/ FontFace == "" ? "Missing FontFace setting in Variables.inc." :
                      /**/ ImageDir == "" ? "Missing ImageDir setting Variables.inc." :
                      /**/ Width == 0     ? "Either Width setting in Variables.inc is missing or is zero." :
                      /**/ Height == 0    ? "Either Height setting in Variables.inc is missing or is zero." :
                      /**/ FontSize == 0  ? "Either FontSize setting in Variables.inc is missing or is zero." :
                      /**/ null;
        }

        private int Clamp(int v, int min, int max)
        {
            return v < min ? min : v > max ? max : v;
        }
    }
}
