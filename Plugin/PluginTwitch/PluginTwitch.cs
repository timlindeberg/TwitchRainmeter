using System;
using System.Runtime.InteropServices;
using Rainmeter;
using System.Text.RegularExpressions;
using System.Drawing;
using System.Diagnostics;
using System.Windows.Automation;


namespace PluginTwitchChat
{

    internal class Measure
    {
        static TwitchClient twitch = null;
        static MessageHandler messageHandler = null;
        static WebBrowserURLLocator urlLocator = null;

        string tpe = "";
        string channelString;

        internal void Reload(API api, ref double maxValue)
        {
            tpe = api.ReadString("Type", "");
            channelString = api.ReadString("DefaultChannelInputString", "");

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
                    default: return; // TODO: Support other browsers
                }
            }
            var channel = urlLocator.TwitchChannel;
            if (channel != null)
                twitch.JoinChannel(channel);
        }

        internal void ReloadMain(API api)
        {
            if (twitch != null)
                return;

            string user = api.ReadString("Username", "").ToLower();
            string ouath = api.ReadString("Ouath", "");
            string fontFace = api.ReadString("FontFace", "");
            string imageDir = api.ReadString("ImageDir", "");
            string seperatorSign = api.ReadString("SeperatorSign", "");
            int width = api.ReadInt("Width", 500);
            int height = api.ReadInt("Height", 500);
            int fontSize = api.ReadInt("FontSize", 0);
            
            if (user == "" || ouath == "" || fontFace == "" || imageDir == "" || fontSize == 0)
                return;

            var font = new Font(fontFace, fontSize);
            var imgDownloader = new ImageDownloader(imageDir);
            var stringMeasurer = new StringMeasurer(font);
            messageHandler = new MessageHandler(width, height, stringMeasurer, seperatorSign, imgDownloader);
            twitch = new TwitchClient(user, ouath, messageHandler, imgDownloader);
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

            if(tpe == "Main")
            {
                messageHandler.Update();
                return 0.0; ;
            }

            if (tpe == "IsInChannel")
                return twitch.IsInChannel ? 1.0 : 0.0;

            var imgInfo = GetImageInfo();
            if (imgInfo == null)
                return 0.0;

            var type = imgInfo.Type;
            var img = twitch.GetImage(imgInfo.Index);
            
            switch (type)
            {
                case "Width": return messageHandler.ImageWidth;
                case "Height": return messageHandler.ImageHeight;
                case "X": return img?.X ?? 0.0;
                case "Y": return img?.Y ?? 0.0;
                default: return 0.0;
            }
        }

        internal string GetString()
        {
            if (tpe == "Main")
                return messageHandler?.String ?? "";

            if (tpe == "ChannelName")
            {
                var str = channelString ?? "";
                if (twitch == null)
                    return str;

                return twitch.IsInChannel ? twitch.Channel : str;
            }

            var imgInfo = GetImageInfo();
            if (imgInfo == null)
                return null;

            var variable = imgInfo.Type;
            var img = twitch?.GetImage(imgInfo.Index);
            switch (variable)
            {
                case "Name": return img?.Name ?? "empty";
                case "ToolTip": return img?.DisplayName;
                default: return null;
            }
        }

        internal void ExecuteBang(string args)
        {
            if (twitch == null || tpe != "Main")
                return;

            if(args.StartsWith("SendMessage "))
            {
                twitch.SendMessage(args.Replace("SendMessage ", ""));
                return;
            }

            if(args.StartsWith("JoinChannel "))
            {
                string channel = args.Replace("JoinChannel ", "").ToLower();

                if (channel == string.Empty)
                {
                    twitch.LeaveChannel();
                    return;
                }

                if (channel.IndexOfAny(new[] { ' ', ',', ':' }) != -1)
                    return;

                if (!channel.StartsWith("#"))
                    channel = "#" + channel;

                twitch.JoinChannel(channel);
            }
        }

        internal static Regex imageInfoRegex = new Regex(@"TwitchImage([^\d]*)(\d*)?");
        internal class ImageInfo
        {
            public string Type;
            public int Index;
            public ImageInfo(string var, int index) { this.Type = var; this.Index = index; }
        }

        internal ImageInfo GetImageInfo()
        {
            var match = imageInfoRegex.Match(tpe).Groups;

            if (match.Count < 2)
                return null;

            var type = match[1].Value;
            var index = -1;
            if(match.Count >= 3 && match[2].Value != string.Empty) 
                index = int.Parse(match[2].Value);

            return new ImageInfo(type, index);
        }

    }

    public static class Plugin
    {
        static IntPtr StringBuffer = IntPtr.Zero;

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
            if (StringBuffer != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(StringBuffer);
                StringBuffer = IntPtr.Zero;
            }
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

        [DllExport]
        public static void ExecuteBang(IntPtr data, IntPtr args)
        {
            Measure measure = (Measure)GCHandle.FromIntPtr(data).Target;
            measure.ExecuteBang(Marshal.PtrToStringUni(args));
        }
    }
}
