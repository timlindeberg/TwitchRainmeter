using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PluginTwitch
{
    class Line
    {
        public List<Emote> Emotes;
        public string Text { get ; private set; }

        public Line()
        {
            Text = string.Empty;
            Emotes = new List<Emote>();
        }

        public void Add(Word w)
        {
            if (Text == string.Empty)
                Text = w.String;
            else
                Text += " " + w.String;
            if(w is Emote)
                Emotes.Add(w as Emote);
        }

        public override string ToString()
        {
            return Text;
        }
    }
}
