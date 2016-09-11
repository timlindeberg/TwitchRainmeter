using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.IO;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Text.RegularExpressions;
using Rainmeter;

namespace PluginTwitchChat
{


    public class ImageDownloader
    {
        public string ImagePath { get; private set; }
        private ISet<string> beingDownloaded;
        private WebClient webClient;
        private int imageQuality;
        private readonly string GlobalBadgesUrl  = @"https://badges.twitch.tv/v1/badges/global/display?language=en";
        private readonly string ChannelBadgesUrl = @"https://badges.twitch.tv/v1/badges/channels/{0}/display?language=en";
        private readonly string EmoteUrl         = @"http://static-cdn.jtvnw.net/emoticons/v1/{0}/{1}";
        private readonly string CheerUrl         = @"http://static-cdn.jtvnw.net/bits/dark/animated/{0}/{1}";
        private readonly string ChannelUrl       = @"https://api.twitch.tv/kraken/channels/{0}";

        public ImageDownloader(string imagePath, int imageQuality)
        {
            this.imageQuality = imageQuality;
            webClient = new WebClient();
            beingDownloaded = new HashSet<string>();
            ImagePath = imagePath;

            DownloadGlobalBadges();
        }

        public string DownloadSubscriberBadge(string channel)
        {
            var channelID = GetChannelID(channel);
            var url = string.Format(ChannelBadgesUrl, channelID);
            var badgeUrls = GetBadgeUrls(url);
            var name = "subscriber1";

            if (!badgeUrls.ContainsKey(name))
                return string.Empty;

            return DownloadImage(badgeUrls[name], name, replaceExistingFile: true);
        }
        
        public string DownloadEmote(string id)
        {
            string quality = EmoteQuality(imageQuality);
            var url = string.Format(EmoteUrl, id, quality);
            return DownloadImage(url, id, replaceExistingFile: false);
        }

        public string DownloadCheer(int roundedBits)
        {
            var color = CheerColor(roundedBits);
            var quality = CheerQuality(imageQuality);
            var name = "cheer" + roundedBits;

            var url = string.Format(CheerUrl, color, quality);
            return DownloadImage(url, name, replaceExistingFile: false, fileEnding: "gif");
        }

        private Dictionary<string, string> GetBadgeUrls(string badgeUrl)
        {
            Dictionary<string, string> urls = new Dictionary<string, string>();
            string json = DownloadString(badgeUrl);
            if (json == "")
                return urls;

            dynamic parsed = JsonParser.Parse(json);
            foreach (var kv1 in parsed["badge_sets"])
            {
                var name = kv1.Key;
                var v1 = kv1.Value;
                var versions = v1["versions"];
                foreach (var kv2 in versions)
                {
                    var version = kv2.Key;
                    var v2 = kv2.Value;
                    string url = v2[BadgeQuality(imageQuality)];
                    if (url == "null")
                        url = v2[BadgeQuality(1)];
                    urls[name + version] = url;
                }
            }
            return urls;
        }

        private string GetChannelID(string channel)
        {
            var url = string.Format(ChannelUrl, channel.Replace("#", ""));
            string json = DownloadString(url);
            API.Log(API.LogType.Notice, json);
            if (json == string.Empty)
                return string.Empty;

            dynamic parsed = JsonParser.Parse(json);
            return parsed["_id"];
        }

        private void DownloadGlobalBadges()
        {
            var globalBadgesFileNames = GetBadgeUrls(GlobalBadgesUrl);

            foreach (var kv in globalBadgesFileNames)
            {
                string fileName = kv.Key;
                string url = kv.Value;
                DownloadImage(url, fileName, replaceExistingFile: false);
            }
        }

        private string DownloadString(string url)
        {
            try
            {
                return webClient.DownloadString(url);
            }
            catch (WebException)
            {
                // Channel doesn't exist
                return string.Empty;
            }
        }

        private string GetFilePath(string name, string fileEnding = "png", int frame = -1)
        {
            return frame == -1 ? string.Format("{0}\\{1}_{2}.{3}", ImagePath, name, imageQuality, fileEnding) :
                                 string.Format("{0}\\{1}-{2}_{3}.{4}", ImagePath, name, frame, imageQuality, fileEnding);
        }

        private string DownloadImage(string url, string fileName, bool replaceExistingFile, string fileEnding = "png")
        {
            var file = GetFilePath(fileName, fileEnding);

            if (beingDownloaded.Contains(file))
                return file;

            lock (beingDownloaded)
                beingDownloaded.Add(fileName);

            var exists = File.Exists(file);
            if (replaceExistingFile || !exists)
            {
                try
                {
                    webClient.DownloadFile(url, file);
                    if (fileEnding == "gif")
                        SplitGifFrames(file, fileName);
                }
                catch (Exception ex) when (ex is WebException || ex is NotSupportedException)
                {
                    // an error occured when fetching the file, do nothing
                }
            }

            lock (beingDownloaded)
                beingDownloaded.Remove(file);

            return file;
        }

        private void SplitGifFrames(string gifPath, string name)
        {
            using (var gif = System.Drawing.Image.FromFile(gifPath))
            {
                int frameCount = gif.GetFrameCount(FrameDimension.Time);
                for (int frame = 0; ;)
                {
                    var file = GetFilePath(name, frame: frame);
                    new Bitmap(gif).Save(file);
                    if (++frame >= frameCount) break;
                    gif.SelectActiveFrame(FrameDimension.Time, frame);
                }
            }
        }


        private string CheerColor(int amount)
        {
            switch (amount)
            {
                case 10000: return "red";
                case 5000: return "blue";
                case 1000: return "green";
                case 100: return "purple";
                case 1: return "gray";
                default: return "";
            }
        }

        private string BadgeQuality(int imageQuality)
        {
            switch (imageQuality)
            {
                case 1: return "image_url_1x";
                case 2: return "image_url_2x";
                case 3: return "image_url_4x";
                default: return "";
            }
        }

        private string EmoteQuality(int imageQuality)
        {
            return imageQuality + ".0";
        }

        private string CheerQuality(int imageQuality)
        {
            switch (imageQuality)
            {
                case 1: return "1";
                case 2: return "2";
                case 3: return "4";
                default: return "";
            }
        }
    }
}
