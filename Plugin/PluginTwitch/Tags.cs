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


        public String DisplayName
        {
            get { return tagMap.ContainsKey("display-name") ? tagMap["display-name"] : null; }
        }

        public string this[string s]
        {
            get { return tagMap[s]; }
        }

        public int Bits
        {
            get { return tagMap.ContainsKey("bits") ? int.Parse(tagMap["bits"]) : -1; }
        }

        public List<string> Badges
        {
            get
            {
                var badges = new List<string>();

                if (tagMap.ContainsKey("badges"))
                {
                    foreach (var badge in tagMap["badges"].Split(','))
                    {
                        var fileName = badge.Replace("/", "");
                        badges.Add(fileName);
                    }
                }
                return badges;
            }
        }

        public List<EmoteInfo> Emotes
        {
            get
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
}
