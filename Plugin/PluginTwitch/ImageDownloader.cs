using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.IO;
using System.Diagnostics;
using System.Drawing;
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
        private readonly string BadgesUrl        = @"https://badges.twitch.tv/v1/badges/global/display?language=en";
        private readonly string GlobalBadgeUrl   = @"https://static-cdn.jtvnw.net/chat-badges/{0}.png";
        private readonly string ChannelBadgesUrl = @"https://api.twitch.tv/kraken/chat/{0}/badges";
        private readonly string EmoteUrl         = @"http://static-cdn.jtvnw.net/emoticons/v1/{0}/{1}";
        private readonly string CheerUrl      = @"http://static-cdn.jtvnw.net/bits/light/animated/{0}/{1}";

        // This is needed since naming is inconsistent. For instance, the
        // globalmod image is called "globalmod.png" but the tag is called global_mod.
        private Dictionary<string, string> GlobalBadgesFileNames;

        public ImageDownloader(string imagePath, int imageQuality)
        {
            
            this.imageQuality = imageQuality;
            webClient = new WebClient();
            beingDownloaded = new HashSet<string>();
            ImagePath = imagePath;

            DownloadGlobalBadges();
        }

        public void DownloadSubscriberBadge(string channel)
        {
            var subscriberBadgeUrl = GetSubscriberBadgeUrl(channel);
            if (subscriberBadgeUrl == string.Empty)
                return;
            DownloadImage(subscriberBadgeUrl, "subscriber1", replaceExistingFile: true);
        }
        
        public void DownloadEmote(string id)
        {
            string quality = EmoteQuality();
            var url = string.Format(EmoteUrl, id, quality);
            DownloadImage(url, id, replaceExistingFile: false);
        }

        public void DownloadCheer(int amount)
        {
            var color = CheerColor(amount);
            var quality = CheerQuality();
            var url = string.Format(CheerUrl, color, quality);
            byte[] imageBytes = webClient.DownloadData(url);

            HttpWebRequest httpWebRequest = (HttpWebRequest)HttpWebRequest.Create(url);
            HttpWebResponse httpWebReponse = (HttpWebResponse)httpWebRequest.GetResponse();
            Stream stream = httpWebReponse.GetResponseStream();


            MemoryStream memoryStream = new MemoryStream();
            stream.CopyTo(memoryStream);
            memoryStream.Position = 0;
            stream = memoryStream;

            var img = System.Drawing.Image.FromStream(stream);
            var file = string.Format("{0}\\{1}.png", ImagePath, "cheer" + amount);
            img.Save(file, System.Drawing.Imaging.ImageFormat.Gif);
        }

        private Dictionary<string, string> GetGlobalBadgeUrls()
        {
            Dictionary<string, string> urls = new Dictionary<string, string>();
            string json = DownloadString(BadgesUrl);
            if (json == "")
                return urls;

            dynamic parsed = JsonParser.Parse(json);
            foreach (var kv1 in parsed["badge_sets"])
            {
                var name = kv1.Key;
                if (name == "subscriber")
                    continue; // parse this seperately
                var v1 = kv1.Value;
                var versions = v1["versions"];
                foreach (var kv2 in versions)
                {
                    var version = kv2.Key;
                    var v2 = kv2.Value;
                    var quality = BadgeQuality();
                    string url = v2[quality];
                    urls[name + version] = url;
                }
            }
            return urls;
        }


        private string GetSubscriberBadgeUrl(string channel)
        {
            string channelBadgesUrl = string.Format(ChannelBadgesUrl, channel.Replace("#", ""));
            string json = DownloadString(channelBadgesUrl);
            if (json == string.Empty)
                return string.Empty;
            dynamic parsed = JsonParser.Parse(json);
            string url = parsed["subscriber"]["image"];
            int lastSlash = url.LastIndexOf('/');
            url = url.Substring(0, lastSlash) + "/" + SubscriberQuality();
            return url;
        }

        private void DownloadGlobalBadges()
        {
            var globalBadgesFileNames = GetGlobalBadgeUrls();

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

        private void DownloadImage(string url, string fileName, bool replaceExistingFile)
        {
            var name = fileName + "_" + imageQuality;
            if (beingDownloaded.Contains(name))
                return;

            lock (beingDownloaded)
                beingDownloaded.Add(name);

            var file = string.Format("{0}\\{1}.png", ImagePath, name);
            if (replaceExistingFile || !File.Exists(file))
            {
                try
                {
                    webClient.DownloadFile(url, file);
                }
                catch (Exception ex) when (ex is WebException || ex is NotSupportedException)
                {
                    // an error occured when fetching the file, do nothing
                }
            }

            lock (beingDownloaded)
                beingDownloaded.Remove(name);
        }

        private string CheerColor(int amount)
        {
            if (amount >= 10000)
                return "red";
            if (amount >= 5000)
                return "blue";
            if (amount >= 1000)
                return "green";
            if (amount >= 100)
                return "purple";
            return "gray";
        }

        private string BadgeQuality()
        {
            switch (imageQuality)
            {
                case 1: return "image_url_1x";
                case 2: return "image_url_2x";
                case 3: return "image_url_4x";
                default: return "";
            }
        }

        private string SubscriberQuality()
        {
            switch (imageQuality)
            {
                case 1: return "18x18.png";
                case 2: return "36x36.png";
                case 3: return "72x72.png";
                default: return "";
            }
        }

        private string EmoteQuality()
        {
            return imageQuality + ".0";
        }

        private string CheerQuality()
        {
            switch (imageQuality)
            {
                case 1: return "1";
                case 2: return "2";
                case 3: return "4"; // wtf twitch
                default: return "";
            }
        }
    }
}
