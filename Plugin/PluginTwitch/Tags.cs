using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PluginTwitchChat
{
    public class Tags
    {

        private IDictionary<string, string> tagMap;
        public Tags(string tags)
        {
            tagMap = new Dictionary<string, string>();
            if (tags == null)
                return;

            foreach (var pair in tags.Split(';'))
            {
                var s = pair.Split('=');
                if (s[1] != "")
                    tagMap[s[0]] = s[1];
            }
        }

        public string this[string s]
        {
            get { return tagMap[s]; }
        }

        public List<Image> GetBadges()
        {
            var badges = new List<Image>();

            if (tagMap.ContainsKey("badges"))
            {
                foreach (var badge in tagMap["badges"].Split(','))
                {
                    // Ignore cheer badges for now since they're not officialy supported
                    if (badge.StartsWith("bits"))
                        continue;

                    var fileName = badge.Replace("/1", "");
                    var displayName = char.ToUpper(fileName[0]) + fileName.Substring(1);
                    badges.Add(new Image(fileName, displayName));
                }
            }
            return badges;
        }

        public List<EmoteInfo> GetEmotes()
        {
            var emotes = new List<EmoteInfo>();

            if (!tagMap.ContainsKey("emotes"))
                return emotes;

            foreach (var emote in tagMap["emotes"].Split('/'))
            {
                var s = emote.Split(':');
                var id = s[0];

                foreach (var index in s[1].Split(','))
                {
                    var i = index.Split('-');
                    var start = int.Parse(i[0]);
                    var end = int.Parse(i[1]);
                    emotes.Add(new EmoteInfo(id, start, end));
                }
            }
            emotes.Sort((e1, e2) => e1.Start - e2.Start);
            return emotes;
        }
    }
}
