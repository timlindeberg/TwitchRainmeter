// Uncomment these only if you want to export GetString() or ExecuteBang().
#define DLLEXPORT_GETSTRING
#define DLLEXPORT_EXECUTEBANG

using System;
using System.Runtime.InteropServices;
using Rainmeter;
using System.Text.RegularExpressions;
using System.Drawing;
using System.Diagnostics;
using System.Windows.Automation;

// Overview: This is a blank canvas on which to build your plugin.

// Note: Measure.GetString, Plugin.GetString, Measure.ExecuteBang, and
// Plugin.ExecuteBang have been commented out. If you need GetString
// and/or ExecuteBang and you have read what they are used for from the
// SDK docs, uncomment the function(s). Otherwise leave them commented out
// (or get rid of them)!

namespace PluginTwitch
{

    internal class Measure
    {
        static TwitchClient twitch = null;
        static WebBrowserURLLocator urlLocator = null;

        string tpe = "";

        internal void Reload(API api, ref double maxValue)
        {
            tpe = api.ReadString("Type", "");

            switch (tpe)
            {
                case "AutoConnector": ReloadAutoConnector(api); return;
                case "Main": ReloadMain(api); return;
            }
        }

        internal void ReloadAutoConnector(API api)
        {
            if (api.ReadDouble("ConnectAutomatically", 0.0) != 1.0)
                return;

            if (urlLocator == null)
            {
                string webBrowser = api.ReadString("Browser", "").ToLower();
                switch (webBrowser)
                {
                    case "chrome":
                        urlLocator = new ChromeURLLocator();
                        break;
                    case "firefox":
                        // todo
                        return;
                    case "ie":
                        return;
                    default: return;
                }
            }
            var channel = urlLocator.TwitchChannel;
            if (channel != null)
                twitch.JoinChannel(channel);
        }

        internal void ReloadMain(API api)
        {
            if (twitch == null)
            {
                string user = api.ReadString("Username", "").ToLower();
                string ouath = api.ReadString("Ouath", "");
                string fontFace = api.ReadString("FontFace", "");
                string imageDir = api.ReadString("ImageDir", "");
                int width = api.ReadInt("Width", 500);
                int height = api.ReadInt("Height", 500);
                int fontSize = api.ReadInt("FontSize", 0);

                if (user == "" || ouath == "" || fontFace == "" || imageDir == "" || fontSize == 0)
                    return;

                var font = new Font(fontFace, fontSize);
                var imgDownloader = new ImageDownloader(imageDir);
                var messageParser = new MessageParser(width, height, font, imgDownloader);
                twitch = new TwitchClient(user, ouath, messageParser, imgDownloader);
            }

            string newChannel = api.ReadString("Channel", "").ToLower();

            if (newChannel == string.Empty)
            {
                twitch.LeaveChannel();
                return;
            }

            if (newChannel.IndexOfAny(new[] { ' ', ',', ':' }) != -1)
                return;

            if (!newChannel.StartsWith("#"))
                newChannel = "#" + newChannel;

            twitch.JoinChannel(newChannel);
        }

        internal void Cleanup()
        {
            twitch?.Disconnect();
            twitch = null;
        }

        internal double Update()    
        {
            if (twitch == null)
                return 0.0;

            if (tpe == "InChannel")
                 return twitch.IsInChannel ? 1.0 : 0.0;

            if (tpe == "TwitchImageWidth")
                return twitch.ImageWidth;

            if (tpe == "TwitchImageHeight")
                return twitch.ImageHeight;

            var imgInfo = ImageInfo();
            if (imgInfo == null)
                return 0.0;

            var variable = imgInfo.Item1;
            var img = imgInfo.Item2;
            if (img == null)
                return 0.0;

            switch (variable)
            {
                case "X": return img.X;
                case "Y": return img.Y;
                default: return 0.0;
            }

            return 0.0;
        }
        
#if DLLEXPORT_GETSTRING
        internal string GetString()
        {
            if (tpe == "Main")
                return twitch?.String ?? "";

            if (tpe == "ChannelName")
                return twitch.Channel;

            var imgInfo = ImageInfo();
            if (imgInfo == null)
                return null;

            var img = imgInfo.Item2;
            if (img == null)
                return null;

            var variable = imgInfo.Item1;
            if (variable == "Name")
                return img.Name;

            return null;
        }
#endif
        
#if DLLEXPORT_EXECUTEBANG
        internal void ExecuteBang(string args)
        {
            twitch.SendMessage(args);
        }
#endif

        // We need to replicate this logic in both GetString and update
        // since TwitchImageName is a string and TwitchImageX/Y is a double.
        internal Tuple<string, Image> ImageInfo()
        {
            if (tpe == "TwitchImageWidth")
                return null;

            if (tpe == "TwitchImageHeight")
                return null;

            if (!tpe.StartsWith("TwitchImage"))
                return null;

            var pattern = @"TwitchImage([^\d]*)(\d*)";
            var match = Regex.Match(tpe, pattern).Groups;

            if (match.Count < 3)
                return null;

            var variable = match[1].Value;
            var index = int.Parse(match[2].Value);

            var img = twitch.GetImage(index);
            if (img == null)
                return null;

            return new Tuple<string, Image>(variable, img);
        }

    }

    public static class Plugin
    {
#if DLLEXPORT_GETSTRING
        static IntPtr StringBuffer = IntPtr.Zero;
#endif

        [DllExport]
        public static void Initialize(ref IntPtr data, IntPtr rm)
        {
            data = GCHandle.ToIntPtr(GCHandle.Alloc(new Measure()));
        }

        [DllExport]
        public static void Finalize(IntPtr data)
        {
            Measure measure = (Measure)GCHandle.FromIntPtr(data).Target;
            measure.Cleanup();
            GCHandle.FromIntPtr(data).Free();
#if DLLEXPORT_GETSTRING
            if (StringBuffer != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(StringBuffer);
                StringBuffer = IntPtr.Zero;
            }
#endif
        }

        [DllExport]
        public static void Reload(IntPtr data, IntPtr rm, ref double maxValue)
        {
            Measure measure = (Measure)GCHandle.FromIntPtr(data).Target;
            measure.Reload(new Rainmeter.API(rm), ref maxValue);
        }

        [DllExport]
        public static double Update(IntPtr data)
        {
            Measure measure = (Measure)GCHandle.FromIntPtr(data).Target;
            return measure.Update();
        }
        
#if DLLEXPORT_GETSTRING
        [DllExport]
        public static IntPtr GetString(IntPtr data)
        {
            Measure measure = (Measure)GCHandle.FromIntPtr(data).Target;
            if (StringBuffer != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(StringBuffer);
                StringBuffer = IntPtr.Zero;
            }

            string stringValue = measure.GetString();
            if (stringValue != null)
            {
                StringBuffer = Marshal.StringToHGlobalUni(stringValue);
            }

            return StringBuffer;
        }
#endif

#if DLLEXPORT_EXECUTEBANG
        [DllExport]
        public static void ExecuteBang(IntPtr data, IntPtr args)
        {
            Measure measure = (Measure)GCHandle.FromIntPtr(data).Target;
            measure.ExecuteBang(Marshal.PtrToStringUni(args));
        }
#endif
    }
}
