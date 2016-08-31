using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.IO;
using System.Diagnostics;
using System.Json;
using System.Text.RegularExpressions;

namespace PluginTwitch
{
    public class ImageDownloader
    {
        public string ImagePath { get; private set; }
        private ISet<string> beingDownloaded;
        private WebClient webClient;

        private readonly string GlobalBadgeUrl   = @"https://static-cdn.jtvnw.net/chat-badges/{0}.png";
        private readonly string ChannelBadgesUrl = @"https://api.twitch.tv/kraken/chat/{0}/badges";
        private readonly string EmoteUrl         = @"http://static-cdn.jtvnw.net/emoticons/v1/{0}/1.0";

        private readonly string[] GlobalBadges = new string[] { "globalmod", "admin", "broadcaster", "mod", "staff", "turbo" };

        // This is needed since naming is inconsistent at Twitch. For instance, the
        // globalmod image is called "globalmod.png" but the tag is called global_mod.
        private readonly Dictionary<string, string> GlobalBadgesFileNames = new Dictionary<string, string> {
            { "globalmod", "global_mod"},
            { "admin", "admin" },
            { "broadcaster", "broadcaster" },
            { "mod", "moderator" },
            { "staff",  "staff" },
            { "turbo" , "turbo" }
        };

        public ImageDownloader(string imagePath)
        {
            ImagePath = imagePath;
            beingDownloaded = new HashSet<string>();
            webClient = new WebClient();
        }

        public void DownloadBadges(string channel)
        {
            foreach(var badge in GlobalBadges)
            {
                // Ugh, why can't the file be name global_mod.png?
                string fileName = GlobalBadgesFileNames[badge];
                var url = string.Format(GlobalBadgeUrl, badge);
                DownloadImage(url, fileName, replaceExistingFile: false);
            }

            string channelBadgesUrl = string.Format(ChannelBadgesUrl, channel.Replace("#", ""));
            try
            {
                string json = webClient.DownloadString(channelBadgesUrl);
                var subscriberBadgeUrl = GetSubscriberBadgeUrl(json);
                if (subscriberBadgeUrl == string.Empty)
                    return;
                DownloadImage(subscriberBadgeUrl, "subscriber", replaceExistingFile: true);
            }
            catch (WebException e)
            {
                // Channel doesn't exist, do nothing
            }
            
        }

        public void DownloadEmote(string id)
        {
            var url = string.Format(EmoteUrl, id);
            DownloadImage(url, id, replaceExistingFile: false);
        }

        private string GetSubscriberBadgeUrl(string json)
        {
            var subscriber = Regex.Match(json, "\"subscriber\":{(.+?)}").Groups[1].Value;
            var imgUrl = Regex.Match(subscriber, "\"image\":\"(.+?)\"").Groups[1].Value;
            return imgUrl;
        }

        private void DownloadImage(string url, string fileName, bool replaceExistingFile)
        {
            lock (beingDownloaded)
            {
                if (beingDownloaded.Contains(fileName))
                    return;

                beingDownloaded.Add(fileName);
            }

            var file = string.Format("{0}\\{1}.png", ImagePath, fileName);
            if (replaceExistingFile || !File.Exists(file))
            {
                webClient.DownloadFile(url, file);
            }

            lock (beingDownloaded)
            {
                beingDownloaded.Remove(fileName);
            }
        }
    }
}
