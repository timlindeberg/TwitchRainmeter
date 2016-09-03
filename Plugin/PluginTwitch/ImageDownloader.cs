using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.IO;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace PluginTwitchChat
{
    public class ImageDownloader
    {
        public string ImagePath { get; private set; }
        private ISet<string> beingDownloaded;
        private WebClient webClient;

        private readonly string GlobalBadgeUrl   = @"https://static-cdn.jtvnw.net/chat-badges/{0}.png";
        private readonly string ChannelBadgesUrl = @"https://api.twitch.tv/kraken/chat/{0}/badges";
        private readonly string EmoteUrl         = @"http://static-cdn.jtvnw.net/emoticons/v1/{0}/1.0";

        private readonly string[] GlobalBadges = new [] { "globalmod", "admin", "broadcaster", "mod", "staff", "turbo" };

        // This is needed since naming is inconsistent. For instance, the
        // globalmod image is called "globalmod.png" but the tag is called global_mod.
        private Dictionary<string, string> GlobalBadgesFileNames;

        public ImageDownloader(string imagePath)
        {
            GlobalBadgesFileNames = new Dictionary<string, string>();
            foreach (var badge in GlobalBadges)
                GlobalBadgesFileNames[badge] = badge;
            GlobalBadgesFileNames["globalmod"] = "global_mod";
            GlobalBadgesFileNames["mod"] = "moderator";

            ImagePath = imagePath;
            beingDownloaded = new HashSet<string>();
            webClient = new WebClient();
        }

        public void DownloadBadges(string channel)
        {
            foreach(var badge in GlobalBadges)
            {
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
            catch (WebException)
            {
                // Channel doesn't exist, do nothing
            }
        }

        public void DownloadEmote(string id)
        {
            var url = string.Format(EmoteUrl, id);
            DownloadImage(url, id, replaceExistingFile: false);
        }

        private static Regex SubscriberRegex = new Regex("\"subscriber\":{(.+?)}");
        private static Regex ImgUrlRegex = new Regex("\"image\":\"(.+?)\"");
        private string GetSubscriberBadgeUrl(string json)
        {
            var subscriber = SubscriberRegex.Match(json).Groups[1].Value;
            var imgUrl = ImgUrlRegex.Match(subscriber).Groups[1].Value;
            return imgUrl;
        }

        private void DownloadImage(string url, string fileName, bool replaceExistingFile)
        {
            if (beingDownloaded.Contains(fileName))
                return;

            lock (beingDownloaded)
                beingDownloaded.Add(fileName);

            var file = string.Format("{0}\\{1}.png", ImagePath, fileName);
            if (replaceExistingFile || !File.Exists(file))
                webClient.DownloadFile(url, file);

            lock (beingDownloaded)
                beingDownloaded.Remove(fileName);
        }
    }
}
