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
using System.Web.Script.Serialization;
using Rainmeter;

namespace PluginTwitchChat
{

    public class ImageDownloader
    {

        private readonly string GlobalBadgesUrl = @"https://badges.twitch.tv/v1/badges/global/display?language=en";
        private readonly string ChannelBadgesUrl = @"https://badges.twitch.tv/v1/badges/channels/{0}/display?language=en";
        private readonly string EmoteUrl = @"http://static-cdn.jtvnw.net/emoticons/v1/{0}/{1}";
        private readonly string CheerUrl = @"http://static-cdn.jtvnw.net/bits/dark/animated/{0}/{1}";
        private readonly string ChannelUrl = @"https://api.twitch.tv/kraken/channels/{0}";
        private readonly string BetterTTVGlobalUrl = @"https://api.betterttv.net/2/emotes";
        private readonly string BetterTTVChannelUrl = @"https://api.betterttv.net/2/channels/{0}";
        private readonly string BetterTTVEmoteUrl = @"https://cdn.betterttv.net/emote/{0}/{1}";

        private readonly string ClientID = "qr09gnapzzef6vgdat883tgank82y4h";

        private JavaScriptSerializer jsonConverter;
        private ISet<string> beingDownloaded;
        private WebClient webClient;
        private int imageQuality;
        private string imagePath;
        private Dictionary<string, BetterTTVEmote> betterTTVGlobalEmotes;
        private Dictionary<string, BetterTTVEmote> betterTTVChannelEmotes;

        public class BetterTTVEmote
        {
            public string id;
            public string name;
            public FileEnding fileEnding;
            public string url;
        }

        public enum FileEnding { PNG, GIF } 

        public ImageDownloader(string imagePath, int imageQuality)
        {
            this.imageQuality = imageQuality;
            jsonConverter = new JavaScriptSerializer();
            jsonConverter.RegisterConverters(new[] { new DynamicJsonConverter() });

            webClient = new WebClient();
            webClient.Headers["Client-ID"] = ClientID;
            beingDownloaded = new HashSet<string>();
            this.imagePath = imagePath;

            DownloadGlobalBadges();
            betterTTVGlobalEmotes = GetBetterTTWEmotes(BetterTTVGlobalUrl);
            betterTTVChannelEmotes = new Dictionary<string, BetterTTVEmote>();
        }

        public void SetChannel(string channel)
        {
            channel = channel.Replace("#", "");
            var betterTTVurl = string.Format(BetterTTVChannelUrl, channel);
            betterTTVChannelEmotes = GetBetterTTWEmotes(betterTTVurl);

            var channelID = GetChannelID(channel);

            if (channelID == -1)
                return;

            var url = string.Format(ChannelBadgesUrl, channelID);
            var badgeUrls = GetBadgeUrls(url);
            var name = "subscriber1";

            if (!badgeUrls.ContainsKey(name))
                return;

            DownloadImage(badgeUrls[name], name, replaceExistingFile: true);
        }
        
        public BetterTTVEmote GetBetterTTVEmote(string word)
        {
            BetterTTVEmote emote = null;
            foreach (var map in new[] { betterTTVGlobalEmotes, betterTTVChannelEmotes })
            {
                if (map.ContainsKey(word))
                {
                    emote = map[word];
                    break;
                }
            }

            if (emote == null)
                return null;

            emote.url = DownloadBetterTTVEmote(emote);
            return emote;
        }

        public string DownloadEmote(string id)
        {
            string quality = EmoteQuality(imageQuality);
            var url = string.Format(EmoteUrl, id, quality);
            return DownloadImage(url, id, replaceExistingFile: false);
        }

        public string GetChannelStatus(string channel)
        {
            dynamic parsed = GetChannelJSON(channel);
            return parsed?["status"] ?? "";
        }

        private string DownloadBetterTTVEmote(BetterTTVEmote emote)
        {
            string quality = BetterTTVEmoteQuality(imageQuality);
            var url = string.Format(BetterTTVEmoteUrl, emote.id, quality);
            return DownloadImage(url, emote.name, replaceExistingFile: false, fileEnding: emote.fileEnding);
        }

        public string DownloadCheer(int roundedBits)
        {
            var color = CheerColor(roundedBits);
            var quality = CheerQuality(imageQuality);
            var name = "cheer" + roundedBits;

            var url = string.Format(CheerUrl, color, quality);
            return DownloadImage(url, name, replaceExistingFile: false, fileEnding: FileEnding.GIF);
        }

        private Dictionary<string, string> GetBadgeUrls(string badgeUrl)
        {
            Dictionary<string, string> urls = new Dictionary<string, string>();
            string json = DownloadString(badgeUrl);
            if (json == "")
                return urls;

            dynamic data = ParseJson(json);
            try
            {
                foreach (var kv1 in data["badge_sets"])
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
            }
            catch
            {
                API.Log(API.LogType.Warning, "Could not parse badges Json from Twitch: " + json);
            }

            return urls;
        }

        private dynamic ParseJson(string json)
        {
            return jsonConverter.Deserialize(json, typeof(object));
        }

        private int GetChannelID(string channel)
        {
            dynamic parsed = GetChannelJSON(channel);
            return parsed?["_id"] ?? -1;
        }

        private dynamic GetChannelJSON(string channel)
        {
            channel = channel.Replace("#", "");
            var url = string.Format(ChannelUrl, channel);
            string json = DownloadString(url);
            if (json == string.Empty)
                return null;
            
            return ParseJson(json);
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

        private Dictionary<string, BetterTTVEmote> GetBetterTTWEmotes(string url)
        {
            Dictionary<string, BetterTTVEmote> emotes = new Dictionary<string, BetterTTVEmote>();
            string json = DownloadString(url);

            if (json == "")
                return emotes;

            dynamic data = ParseJson(json);
            try
            {
                foreach (var e in data["emotes"])
                {
                    var code = e["code"];
                    emotes[code] = new BetterTTVEmote
                    {
                        id = e["id"],
                        name = code,
                        fileEnding = ParseEnum<FileEnding>(e["imageType"])
                    };
                }
            }
            catch
            {
                API.Log(API.LogType.Warning, "Could not parse emote Json from BetterTTW: " + json);
            }
            return emotes;
        }

        private T ParseEnum<T>(string value)
        {
            return (T)Enum.Parse(typeof(T), value, ignoreCase:true);
        }

        private string DownloadString(string url)
        {
            try
            {
                return webClient.DownloadString(url);
            }
            catch (Exception ex) when (ex is WebException || ex is NotSupportedException)
            {
                API.Log(API.LogType.Warning, "Could not download string from " + url);
                return string.Empty;
            }
        }

        private string GetFilePath(string name, FileEnding fileEnding = FileEnding.PNG, int frame = -1)
        {
            var f = Enum.GetName(typeof(FileEnding), fileEnding).ToLower();
            return frame == -1 ? string.Format("{0}\\{1}_{2}.{3}", imagePath, name, imageQuality, f) :
                                 string.Format("{0}\\{1}-{2}_{3}.{4}", imagePath, name, frame, imageQuality, f);
        }

        private string DownloadImage(string url, string fileName, bool replaceExistingFile, FileEnding fileEnding = FileEnding.PNG)
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
                    if (fileEnding == FileEnding.GIF)
                        SplitGifFrames(file, fileName);
                }
                catch (Exception ex) when (ex is WebException || ex is NotSupportedException)
                {
                    API.Log(API.LogType.Warning, "Could not download image from " + url);
                }
            }

            lock (beingDownloaded)
                beingDownloaded.Remove(file);

            return file;
        }

        private void SplitGifFrames(string path, string name)
        {
            using (var gif = System.Drawing.Image.FromFile(path))
            {
                int frameCount = gif.GetFrameCount(FrameDimension.Time);
                for (int frame = 0; ;)
                {
                    var file = GetFilePath(name, frame: frame);
                    using (var bmp = new Bitmap(gif))
                        bmp.Save(file);

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

        private string BetterTTVEmoteQuality(int imageQuality)
        {
            return imageQuality + "x";
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
