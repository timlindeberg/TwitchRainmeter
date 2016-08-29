using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PluginTwitch
{
    public struct Line
    {
        public string text;
        public IList<Emote> emotes;

        public Line(string text, IList<Emote> emotes)
        {
            this.text = text;
            this.emotes = emotes;
        }

        public override string ToString()
        {
            var sb = new StringBuilder(text);
            foreach (var e in emotes)
                sb.AppendLine("\t" + e);
            return sb.ToString();
        }
    }
}
