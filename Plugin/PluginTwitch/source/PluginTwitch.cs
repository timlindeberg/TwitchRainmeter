using System;
using System.Runtime.InteropServices;
using Rainmeter;
using System.Text.RegularExpressions;
using System.Drawing;
using System.Diagnostics;

namespace PluginTwitchChat
{
    internal class TwitchPlugin
    {
        static readonly string MissingImage = "_empty";

        static TwitchClient TwitchClient = null;
        static MessageFormatter MessageFormatter = null;
        static TwitchChat TwitchChat = null;
        static WebBrowserURLLocator UrlLocator = null;
        static StringMeasurer StringMeasurer = null;

        string StringValue;
        string measureType = "";
        static int i = 0;
        Func<double> update;

        internal void Reload(API rm, ref double maxValue)
        {
            measureType = rm.ReadString("Type", "");
            switch (measureType)
            {
                case "Main":
                    ReloadMain(rm);
                    break;
                case "AutoConnector":
                    ReloadAutoConnector(rm);
                    break;
            }

            update = GetUpdateFunction();
        }

        internal void ReloadAutoConnector(API rm)
        {
            if (rm.ReadDouble("ConnectAutomatically", 0.0) != 1.0)
            {
                return;
            }

            if (UrlLocator == null)
            {
                var webBrowser = rm.ReadString("Browser", "").ToLower();
                switch (webBrowser)
                {
                    case "chrome":
                        UrlLocator = new ChromeURLLocator();
                        break;
                    default: return; // TODO: Support other browsers
                }
            }
            var channel = UrlLocator.TwitchChannel;
            if (channel != null)
            {
                TwitchClient?.JoinChannel(channel);
            }
        }

        internal void ReloadMain(API rm)
        {
            if (TwitchClient != null)
            {
                return;
            }

            var settings = new Settings(rm);
            if (settings.ErrorMessage != null)
            {
                StringValue = settings.ErrorMessage;
                return;
            }

            var font = new Font(settings.FontFace, settings.FontSize);
            var twitchDownloader = new TwitchDownloader(settings);
            StringMeasurer = new StringMeasurer(font);
            MessageFormatter = new MessageFormatter(settings, StringMeasurer, twitchDownloader);
            TwitchChat = new TwitchChat(settings, StringMeasurer, MessageFormatter);
            TwitchClient = new TwitchClient(settings, TwitchChat, twitchDownloader);
        }

        internal void Cleanup()
        {
            if (measureType != "Main")
            {
                return;
            }

            TwitchClient?.Disconnect();
            TwitchClient = null;
        }

        internal double Update()
        {
            if(TwitchClient == null)
            {
                return 0.0;
            }
            return update();
        }

        internal Func<double> StringValueSetter(Func<string> f)
        {
            StringValue = "";
            return () =>
            {
                StringValue = f();
                return 0.0;
            };
        }

        internal Func<double> GetUpdateFunction()
        {
            switch (measureType)
            {
                case "ChannelName": return StringValueSetter(() => TwitchClient.IsInChannel ? TwitchClient.Channel : "");
                case "ChannelStatus": return StringValueSetter(() => TwitchClient.IsInChannel ? TwitchClient.ChannelStatus : "");
                case "Viewers": return StringValueSetter(() => TwitchClient.IsInChannel ? TwitchClient.Viewers : "");
                case "ViewerCount": return () => TwitchClient.ViewerCount;
                case "IsInChannel": return () => TwitchClient.IsInChannel ? 1.0 : 0.0;
                case "Main":
                    return () =>
                    {
                        TwitchClient.Update();
                        TwitchChat.Update();
                        StringValue = TwitchChat.GetContent();
                        return 0.0;
                    };
            }

            var info = GetInfo(MeasureInfo.Image);
            if (info != null)
            {
                return GetImageUpdateFunction(info);
            }
            info = GetInfo(MeasureInfo.Gif);
            if (info != null)
            {
                return GetGifUpdateFunction(info);
            }
            info = GetInfo(MeasureInfo.Link);
            if (info != null)
            {
                return GetLinkUpdateFunction(info);
            }

            return () => 0.0;
        }

        internal Func<double> GetImageUpdateFunction(MeasureInfo info)
        {
            Func<Image> image = () => TwitchChat.GetImage(info.Index);
            switch (info.Type)
            {
                case "Width": return () => MessageFormatter.ImageSize.Width;
                case "Height": return () => MessageFormatter.ImageSize.Height;
                case "X": return () => image()?.X ?? 0.0;
                case "Y": return () => image()?.Y ?? 0.0;
                case "Name": return StringValueSetter(() => image()?.Name ?? MissingImage);
                case "ToolTip": return StringValueSetter(() => image()?.DisplayName ?? MissingImage);
                default: return () => 0.0;
            }
        }

        internal Func<double> GetGifUpdateFunction(MeasureInfo info)
        {
            Func<AnimatedImage> gif = () => TwitchChat.GetGif(info.Index);
            switch (info.Type)
            {
                case "X": return () => gif()?.X ?? 0.0;
                case "Y": return () => gif()?.Y ?? 0.0;
                case "Name": return StringValueSetter(() => gif()?.Name ?? MissingImage);
                case "ToolTip": return StringValueSetter(() => gif()?.DisplayName ?? MissingImage);
                default: return () => 0.0;
            }
        }

        internal Func<double> GetLinkUpdateFunction(MeasureInfo info)
        {
            Func<Link> link = () => TwitchChat.GetLink(info.Index);
            switch (info.Type)
            {
                case "X": return () => link()?.X ?? 0.0;
                case "Y": return () => link()?.Y ?? 0.0;
                case "Width": return () => link()?.Width ?? 0.0;
                case "Height": return () => link()?.Height ?? 0.0;
                case "Name": return StringValueSetter(() => link() ?? "");
                default: return () => 0.0;
            }
        }

        internal string GetString()
        {
            return StringValue;
        }

        internal void ExecuteBang(string args)
        {
            if (TwitchClient == null || measureType != "Main")
            {
                return;
            }

            if (args.StartsWith("SendMessage"))
            {
                TwitchClient.SendMessage(args.Replace("SendMessage ", ""));
                return;
            }

            if (args.StartsWith("JoinChannel"))
            {
                var channel = args.Replace("JoinChannel ", "").ToLower();
                if (channel == string.Empty)
                {
                    TwitchClient.LeaveChannel();
                    return;
                }

                if (channel.IndexOfAny(new[] { ' ', ',', ':' }) != -1)
                {
                    return;
                }

                if (!channel.StartsWith("#"))
                {
                    channel = "#" + channel;
                }

                TwitchClient.JoinChannel(channel);
            }
        }

        internal class MeasureInfo
        {
            private static readonly string regex = @"([^\d]*)(\d*)?";
            public static readonly Regex Image = new Regex("Image" + regex);
            public static readonly Regex Gif = new Regex("Gif" + regex);
            public static readonly Regex Link = new Regex("Link" + regex);

            public string Type;
            public int Index;
        }

        internal MeasureInfo GetInfo(Regex regex)
        {
            var match = regex.Match(measureType).Groups;

            if (match.Count < 2)
            {
                return null;
            }

            var type = match[1].Value;
            var index = -1;
            if (match.Count >= 3 && match[2].Value != string.Empty)
            {
                index = int.Parse(match[2].Value);
            }

            return new MeasureInfo() { Type = type, Index = index };
        }

    }

    public static class Plugin
    {
        static IntPtr StringBuffer = IntPtr.Zero;

        [DllExport]
        public static void Initialize(ref IntPtr data, IntPtr rm)
        {
            data = GCHandle.ToIntPtr(GCHandle.Alloc(new TwitchPlugin()));
        }

        [DllExport]
        public static void Finalize(IntPtr data)
        {
            TwitchPlugin measure = (TwitchPlugin)GCHandle.FromIntPtr(data).Target;
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
            TwitchPlugin measure = (TwitchPlugin)GCHandle.FromIntPtr(data).Target;
            measure.Reload(new Rainmeter.API(rm), ref maxValue);
        }

        [DllExport]
        public static double Update(IntPtr data)
        {
            TwitchPlugin measure = (TwitchPlugin)GCHandle.FromIntPtr(data).Target;
            return measure.Update();
        }

        [DllExport]
        public static IntPtr GetString(IntPtr data)
        {
            TwitchPlugin measure = (TwitchPlugin)GCHandle.FromIntPtr(data).Target;
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
            TwitchPlugin measure = (TwitchPlugin)GCHandle.FromIntPtr(data).Target;
            measure.ExecuteBang(Marshal.PtrToStringUni(args));
        }
    }
}
