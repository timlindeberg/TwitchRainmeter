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

        string String;
        static readonly string MissingImage = "_empty";
        string tpe = "";
        string channelString = "";
        Info imgInfo;
        Info gifInfo;
        Info linkInfo;

        internal void Reload(API api, ref double maxValue)
        {
            tpe = api.ReadString("Type", "");
            switch (tpe)
            {
                case "ChannelName":
                    channelString = api.ReadString("DefaultChannelInputString", "");
                    break;
                case "Main":
                    ReloadMain(api);
                    break;
                case "AutoConnector":
                    ReloadAutoConnector(api);
                    break;
                default:
                    imgInfo = GetInfo(ImageInfoRegex);
                    gifInfo = GetInfo(GifInfoRegex);
                    linkInfo = GetInfo(LinkInfoRegex);

                    if (imgInfo != null && imgInfo.Type == "Name")
                        String = MissingImage;

                    if (gifInfo != null && gifInfo.Type == "Name")
                        String = MissingImage;

                    break;
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
            bool useSeperator = api.ReadInt("UseSeperator", 0) == 1;
            int width = api.ReadInt("Width", 500);
            int height = api.ReadInt("Height", 500);
            int fontSize = api.ReadInt("FontSize", 0);
            int imageQuality = api.ReadInt("ImageQuality", 1);
            imageQuality = (imageQuality < 1) ? 1 : (imageQuality > 3) ? 3 : imageQuality;

            if (user == "" || ouath == "" || fontFace == "" || imageDir == "" || fontSize == 0)
                return;

            var size = new Size(width, height);
            var font = new Font(fontFace, fontSize);
            var imgDownloader = new ImageDownloader(imageDir, imageQuality);
            var stringMeasurer = new StringMeasurer(font);
            messageHandler = new MessageHandler(size, stringMeasurer, useSeperator, imgDownloader);
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

            if (gifInfo != null)
                return UpdateGif();

            if (imgInfo != null)
                return UpdateImage();

            if (linkInfo != null)
                return UpdateLink();

            switch (tpe)
            {
                case "Main":
                    messageHandler.Update();
                    String = messageHandler.String;
                    return 0.0;
                case "ChannelName":
                    String = twitch.IsInChannel ? twitch.Channel : channelString;
                    return 0.0;
                case "IsInChannel":
                    return twitch.IsInChannel ? 1.0 : 0.0;
            }

            return 0.0;
        }

        internal double UpdateImage()
        {
            switch (imgInfo.Type)
            {
                case "Width": return messageHandler.ImageSize.Width;
                case "Height": return messageHandler.ImageSize.Height;
            }

            var img = messageHandler.GetImage(imgInfo.Index);
            if (img == null)
            {
                if (imgInfo.Type == "Name")
                    String = MissingImage;
                return 0.0;
            }

            switch (imgInfo.Type)
            {
                case "X": return img.X;
                case "Y": return img.Y;
                case "Name": String = img.Name; break;
                case "ToolTip": String = img.DisplayName; break;
            }
            return 0.0;
        }

        internal double UpdateGif()
        {
            var gif = messageHandler.GetGif(gifInfo.Index);
            if (gif == null)
            {
                if (gifInfo.Type == "Name")
                    String = MissingImage;
                return 0.0;
            }

            switch (gifInfo.Type)
            {
                case "X": return gif.X;
                case "Y": return gif.Y;
                case "Name": String = gif.Name; break;
                case "ToolTip": String = gif.DisplayName; break;
            }
            return 0.0;
        }

        internal double UpdateLink()
        {
            var link = messageHandler.GetLink(linkInfo.Index);
            if (link == null)
            {
                if (linkInfo.Type == "Url" || linkInfo.Type == "Name")
                    String = "";
                return 0.0;
            }

            switch (linkInfo.Type)
            {
                case "X": return link.X;
                case "Y": return link.Y;
                case "Width": return link.Width;
                case "Height": return link.Height;
                case "Url": String = link.Url; break;
                case "Name": String = link; break;
            }
            return 0.0;
        }

        internal string GetString()
        {
            return String;
        }

        internal void ExecuteBang(string args)
        {
            if (twitch == null || tpe != "Main")
                return;

            if (args.StartsWith("SendMessage"))
            {
                twitch.SendMessage(args.Replace("SendMessage ", ""));
                return;
            }

            if (args.StartsWith("JoinChannel"))
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

        internal static readonly Regex ImageInfoRegex = new Regex(@"Image([^\d]*)(\d*)?");
        internal static readonly Regex GifInfoRegex = new Regex(@"Gif([^\d]*)(\d*)?");
        internal static readonly Regex LinkInfoRegex = new Regex(@"Link([^\d]*)(\d*)?");
        internal class Info
        {
            public string Type;
            public int Index;
        }

        internal Info GetInfo(Regex regex)
        {
            var match = regex.Match(tpe).Groups;

            if (match.Count < 2)
                return null;

            var type = match[1].Value;
            var index = -1;
            if (match.Count >= 3 && match[2].Value != string.Empty)
                index = int.Parse(match[2].Value);

            return new Info() { Type = type, Index = index };
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
