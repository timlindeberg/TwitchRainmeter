using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Diagnostics;

namespace PluginTwitch
{
    public abstract class WebBrowserURLLocator
    {
        public abstract string GetActiveUrl();

        private static readonly Regex twitchChannelRegex = new Regex(@"https:\/\/www\.twitch\.tv\/(.*)");
        private static readonly String[] notChannels = new[] { "directory", "store", "jobs", "settings", "subscriptions" };

        public string TwitchChannel
        {
            get
            {
                var s = GetActiveUrl();
                if (s == null)
                    return null;

                var matchGroups = twitchChannelRegex.Match(s).Groups;
                if (matchGroups.Count < 2)
                    return null;

                var ch = matchGroups[1].Value;
                if (NotAChannel(ch))
                    return null;

                return "#" + ch;
            }
        }

        // This is kind of a hack. Instead of doing a request to see wether the url is an actual
        // channel we just assume that it is if it's not one of the given adresses. There might
        // be more pages that should not count as a channel.
        private bool NotAChannel(string s)
        {
            return s.Contains('/') || notChannels.Contains(s);
        }
    }
}
