using System;
using System.Runtime.InteropServices;
using Rainmeter;
using System.Text.RegularExpressions;
using System.Drawing;

namespace PluginTwitchChat
{
    internal class Measure
    {
        static readonly string MissingImage = "_empty";

        static TwitchClient TwitchClient = null;
        static MessageHandler MessageHandler = null;
        static WebBrowserURLLocator UrlLocator = null;
        static StringMeasurer StringMeasurer = null;

        string StringValue;
        string measureType = "";
        Func<double> update;

        internal void Reload(API api, ref double maxValue)
        {
            measureType = api.ReadString("Type", "");
            switch (measureType)
            {
                case "Main":
                    StringValue = "";
                    ReloadMain(api);
                    break;
                case "AutoConnector":
                    ReloadAutoConnector(api);
                    break;
            }

            update = GetUpdateFunction();
        }

        internal void ReloadAutoConnector(API api)
        {
            if (api.ReadDouble("ConnectAutomatically", 0.0) != 1.0)
            {
                return;
            }

            if (UrlLocator == null)
            {
                var webBrowser = api.ReadString("Browser", "").ToLower();
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

        internal void ReloadMain(API api)
        {
            if (TwitchClient != null)
            {
                return;
            }

            var settings = new Settings(api);

            if (settings.ErrorMessage != null)
            {
                StringValue = settings.ErrorMessage;
                return;
            }

            var font = new Font(settings.FontFace, settings.FontSize);
            var twitchDownloader = new TwitchDownloader(settings);
            StringMeasurer = new StringMeasurer(font);
            MessageHandler = new MessageHandler(settings, StringMeasurer, twitchDownloader);
            TwitchClient = new TwitchClient(settings, MessageHandler, twitchDownloader);
        }

        internal void Cleanup()
        {
            if (measureType != "Main")
            {
                return;
            }

            TwitchClient?.Disconnect();
            TwitchClient = null;
            StringMeasurer?.Dispose();
        }

        internal double Update()
        {
            return TwitchClient == null ? 0.0 : update();
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
                case "ViewerCount": return () => { return TwitchClient.ViewerCount; };
                case "IsInChannel": return () => { return TwitchClient.IsInChannel ? 1.0 : 0.0; };
                case "Main":
                    return StringValueSetter(() =>
                    {
                        TwitchClient.Update();
                        MessageHandler.Update();
                        return MessageHandler.String;
                    });
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

            return () => { return 0.0; };
        }

        internal Func<double> GetImageUpdateFunction(MeasureInfo info)
        {
            var i = info.Index;
            switch (info.Type)
            {
                case "Width": return () => { return MessageHandler.ImageSize.Width; };
                case "Height": return () => { return MessageHandler.ImageSize.Height; };
                case "X": return () => { return MessageHandler.GetImage(i)?.X ?? 0.0; };
                case "Y": return () => { return MessageHandler.GetImage(i)?.Y ?? 0.0; };
                case "Name": return StringValueSetter(() => MessageHandler.GetImage(i)?.Name ?? MissingImage);
                case "ToolTip": return StringValueSetter(() => MessageHandler.GetImage(i)?.DisplayName ?? MissingImage);
                default: return () => { return 0.0; };
            }
        }

        internal Func<double> GetGifUpdateFunction(MeasureInfo info)
        {
            var i = info.Index;
            switch (info.Type)
            {
                case "X": return () => { return MessageHandler.GetGif(i)?.X ?? 0.0; };
                case "Y": return () => { return MessageHandler.GetGif(i)?.Y ?? 0.0; };
                case "Name": return StringValueSetter(() => MessageHandler.GetGif(i)?.Name ?? MissingImage);
                case "ToolTip": return StringValueSetter(() => MessageHandler.GetGif(i)?.DisplayName ?? MissingImage);
                default: return () => { return 0.0; };
            }
        }

        internal Func<double> GetLinkUpdateFunction(MeasureInfo info)
        {
            var i = info.Index;
            switch (info.Type)
            {
                case "X": return () => { return MessageHandler.GetLink(i)?.X ?? 0.0; };
                case "Y": return () => { return MessageHandler.GetLink(i)?.Y ?? 0.0; };
                case "Width": return () => { return MessageHandler.GetLink(i)?.Width ?? 0.0; };
                case "Height": return () => { return MessageHandler.GetLink(i)?.Height ?? 0.0; };
                case "Name": return StringValueSetter(() => MessageHandler.GetLink(i) ?? "");
                default: return () => { return 0.0; };
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
