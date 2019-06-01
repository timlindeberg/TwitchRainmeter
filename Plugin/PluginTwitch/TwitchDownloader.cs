using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Net;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;
using System.Web.Script.Serialization;
using Rainmeter;

namespace PluginTwitchChat
{
    public class TwitchDownloader
    {
        private const string GlobalBadgesUrl = @"https://badges.twitch.tv/v1/badges/global/display?language=en";
        private const string ChannelBadgesUrl = @"https://badges.twitch.tv/v1/badges/channels/{0}/display?language=en";

        private const string EmoteUrl = @"http://static-cdn.jtvnw.net/emoticons/v1/{0}/{1}";
        private const string CheerUrl = @"http://static-cdn.jtvnw.net/bits/dark/animated/{0}/{1}";

        private const string ChannelUrl = @"https://api.twitch.tv/kraken/channels/{0}";
        private const string ChattersUrl = @"https://tmi.twitch.tv/group/user/{0}/chatters";

        private const string BetterTTVGlobalUrl = @"https://api.betterttv.net/2/emotes";
        private const string BetterTTVChannelUrl = @"https://api.betterttv.net/2/channels/{0}";
        private const string BetterTTVEmoteUrl = @"https://cdn.betterttv.net/emote/{0}/{1}";

        private const string FrankerFacezGlobalUrl = @"https://api.frankerfacez.com/v1/set/global";
        private const string FrankerFacezChannelUrl = @"https://api.frankerfacez.com/v1/room/{0}";

        private const string ClientID = "qr09gnapzzef6vgdat883tgank82y4h";
        private readonly string[] ViewerTypes = new[] { "moderators", "staff", "admins", "global_mods", "viewers" };

        private readonly Settings settings;

        private readonly JavaScriptSerializer jsonConverter;
        private readonly ISet<string> beingDownloaded;

        private Dictionary<string, NamedEmote> globalEmotes;
        private Dictionary<string, NamedEmote> channelEmotes;

        private Dictionary<string, string> badgeDescriptions;


        public class NamedEmote
        {
            public string Name;
            public FileEnding FileEnding;
            public string Url;
            public string Path;
            public string Source;

            public string Description
            {
                get { return Name + " [ " + Source + " ]"; }
            }
        }

        private class BadgeInfo
        {
            public string Name;
            public string Url;
            public string Description;
        }

        public enum FileEnding { PNG, GIF }

        public TwitchDownloader(Settings settings)
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            this.settings = settings;

            jsonConverter = new JavaScriptSerializer();
            beingDownloaded = new HashSet<string>();
            badgeDescriptions = new Dictionary<string, string>();
            globalEmotes = new Dictionary<string, NamedEmote>();
            channelEmotes = new Dictionary<string, NamedEmote>();

            DownloadBadges(GlobalBadgesUrl);
            AddBetterTTVEmotes(globalEmotes, BetterTTVGlobalUrl);
            AddFrankerFacezEmotes(globalEmotes, FrankerFacezGlobalUrl);
        }

        public void SetChannel(string channel)
        {
            channel = channel.Replace("#", "");
            channelEmotes = new Dictionary<string, NamedEmote>();

            AddBetterTTVEmotes(channelEmotes, string.Format(BetterTTVChannelUrl, channel));
            AddFrankerFacezEmotes(channelEmotes, string.Format(FrankerFacezChannelUrl, channel));

            var channelID = GetChannelID(channel);

            if (channelID == -1)
                return;

            var url = string.Format(ChannelBadgesUrl, channelID);
            DownloadBadges(url);
        }

        public NamedEmote GetNamedEmote(string word)
        {
            NamedEmote emote =
                channelEmotes.ContainsKey(word) ? channelEmotes[word] :
                globalEmotes.ContainsKey(word) ? globalEmotes[word] :
                null;

            if (emote == null)
                return null;

            emote.Path = DownloadImage(emote.Url, emote.Name, replaceExistingFile: false, fileEnding: emote.FileEnding);
            emote.Name = CleanFileName(emote.Name);
            return emote;
        }

        public string DownloadEmote(string id)
        {
            string quality = EmoteQuality();
            var url = string.Format(EmoteUrl, id, quality);
            return DownloadImage(url, id, replaceExistingFile: false);
        }

        public string GetChannelStatus(string channel)
        {
            dynamic parsed = GetChannelJSON(channel);
            return parsed?["status"] ?? "";
        }

        public List<string> GetViewers(string channel)
        {
            List<string> viewers = new List<string>();

            channel = channel.Replace("#", "");
            var url = string.Format(ChattersUrl, channel);
            string json = DownloadString(url);

            if (json == string.Empty)
                return viewers;

            dynamic data = jsonConverter.DeserializeObject(json);
            var chatters = data["chatters"];
            foreach (var type in ViewerTypes)
                foreach (var viewer in chatters[type])
                    viewers.Add(viewer);
            return viewers;
        }

        public string DownloadCheer(int roundedBits)
        {
            var color = CheerColor(roundedBits);
            var quality = CheerQuality();
            var name = "cheer" + roundedBits;

            var url = string.Format(CheerUrl, color, quality);
            return DownloadImage(url, name, replaceExistingFile: false, fileEnding: FileEnding.GIF);
        }

        public string GetDescription(string badgeName)
        {
            return badgeName.StartsWith("bits") ?
                   "cheer " + badgeName.Replace("bits", "") :
                   badgeDescriptions[badgeName];
        }

        private void DownloadBadges(string badgeUrl)
        {
            foreach (var badgeInfo in GetBadgeInfo(badgeUrl))
            {
                var name = badgeInfo.Name;
                badgeDescriptions[name] = badgeInfo.Description;
                DownloadImage(badgeInfo.Url, name, replaceExistingFile: true);
            }
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

            return jsonConverter.DeserializeObject(json);
        }

        private List<BadgeInfo> GetBadgeInfo(string badgeUrl)
        {
            List<BadgeInfo> urls = new List<BadgeInfo>();
            string json = DownloadString(badgeUrl);
            if (json == "")
                return urls;

            dynamic data = jsonConverter.DeserializeObject(json);
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
                        string url = v2[BadgeQuality(settings.ImageQuality)] ?? v2[BadgeQuality(1)];
                        string description = v2["description"];
                        urls.Add(new BadgeInfo
                        {
                            Name = name + version,
                            Url = url,
                            Description = description
                        });
                    }
                }
            }
            catch
            {
                API.Log(API.LogType.Warning, "Could not parse badges Json from Twitch: " + json);
            }

            return urls;
        }

        private void AddBetterTTVEmotes(Dictionary<string, NamedEmote> emotes, string url)
        {
            if (!settings.UseBetterTTV)
                return;

            string json = DownloadString(url);

            if (json == "")
                return;

            string quality = BetterTTVEmoteQuality();
            dynamic data = jsonConverter.DeserializeObject(json);
            try
            {
                foreach (var e in data["emotes"])
                {
                    var name = e["code"];
                    var id = e["id"];

                    emotes[name] = new NamedEmote
                    {
                        Name = name,
                        Url = string.Format(BetterTTVEmoteUrl, id, quality),
                        FileEnding = ParseEnum<FileEnding>(e["imageType"]),
                        Source = "BetterTTV"
                    };
                }
            }
            catch
            {
                API.Log(API.LogType.Warning, "Could not parse emote Json from BetterTTW: " + json);
            }
        }

        private void AddFrankerFacezEmotes(Dictionary<string, NamedEmote> emotes, string infoUrl)
        {
            if (!settings.UseFrankerFacez)
                return;

            string json = DownloadString(infoUrl);

            if (json == "")
                return;

            var quality = FrankenFacezQuality(settings.ImageQuality);
            dynamic data = jsonConverter.DeserializeObject(json);
            try
            {
                foreach (var set in data["sets"])
                {
                    foreach (var emoticon in set.Value["emoticons"])
                    {
                        var name = emoticon["name"];
                        var urls = emoticon["urls"];
                        var url = urls.ContainsKey(quality) ? urls[quality] : urls[FrankenFacezQuality(1)];
                        emotes[name] = new NamedEmote
                        {
                            Name = name,
                            Url = "https:" + url,
                            FileEnding = FileEnding.PNG,
                            Source = "FrankerFacez"
                        };
                    }
                }
            }
            catch
            {
                API.Log(API.LogType.Warning, "Could not parse emote Json from FrankenFacez: " + json);
            }
        }

        private T ParseEnum<T>(string value)
        {
            return (T)Enum.Parse(typeof(T), value, ignoreCase: true);
        }

        private WebClient CreateWebClient()
        {
            var webClient = new WebClient();
            webClient.Headers["Client-ID"] = ClientID;
            return webClient;
        }

        private string DownloadString(string url)
        {
            try
            {
                return CreateWebClient().DownloadString(url);
            }
            catch (Exception ex) when (ex is WebException || ex is NotSupportedException)
            {
                API.Log(API.LogType.Warning, "Could not download string from " + url);
                return string.Empty;
            }
        }

        private string GetFilePath(string name, FileEnding fileEnding = FileEnding.PNG, int frame = -1)
        {
            var ending = Enum.GetName(typeof(FileEnding), fileEnding).ToLower();
            return frame == -1 ? string.Format("{0}\\{1}_{2}.{3}", settings.ImageDir, name, settings.ImageQuality, ending) :
                                 string.Format("{0}\\{1}-{2}_{3}.{4}", settings.ImageDir, name, frame, settings.ImageQuality, ending);
        }

        private string DownloadImage(string url, string fileName, bool replaceExistingFile, FileEnding fileEnding = FileEnding.PNG)
        {
            fileName = CleanFileName(fileName);
            var path = GetFilePath(fileName, fileEnding);

            if (beingDownloaded.Contains(path))
                return path;

            if (File.Exists(path) && !replaceExistingFile)
                return path;

            lock (beingDownloaded)
                beingDownloaded.Add(fileName);

            Task.Run(() =>
            {
                try
                {
                    var uri = new Uri(url);
                    CreateWebClient().DownloadFile(uri, path);
                    if (fileEnding == FileEnding.GIF)
                        SplitGifFrames(path, fileName);
                }
                catch (Exception ex) when (ex is WebException || ex is NotSupportedException)
                {
                    API.Log(API.LogType.Warning, "Could not download image from " + url);
                }
                lock (beingDownloaded)
                    beingDownloaded.Remove(path);
            });

            return path;
        }

        private string CleanFileName(string fileName)
        {
            return Path.GetInvalidFileNameChars().Aggregate(fileName, (current, c) => current.Replace(c.ToString(), ((int)c).ToString()));
        }

        private void SplitGifFrames(string path, string name)
        {
            using (var gif = System.Drawing.Image.FromFile(path))
            {
                int frameCount = gif.GetFrameCount(FrameDimension.Time);
                for (int frame = 0; ;)
                {
                    var fileName = GetFilePath(name, frame: frame);
                    using (var bmp = new Bitmap(gif))
                        bmp.Save(fileName);
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

        private string EmoteQuality()
        {
            return settings.ImageQuality + ".0";
        }

        private string BetterTTVEmoteQuality()
        {
            return settings.ImageQuality + "x";
        }

        private string CheerQuality()
        {
            switch (settings.ImageQuality)
            {
                case 1: return "1";
                case 2: return "2";
                case 3: return "4";
                default: return "";
            }
        }
        private string FrankenFacezQuality(int imageQuality)
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
