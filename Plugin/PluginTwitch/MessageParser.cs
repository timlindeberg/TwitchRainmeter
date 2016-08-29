using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Diagnostics;
using System.IO;
using System.Net;

namespace PluginTwitch
{
    public class MessageParser
    {
        public int EmoteSize { get; private set; }
        public List<Emote> Emotes { get; private set; }

        private FixedSizedQueue<Line> queue;
        private HashSet<int> beingDownloaded;
        private Font font;
        private WebClient webClient;
        private int width;
        private double lineHeight;

        public MessageParser(int lines, int width, Font font)
        {
            queue = new FixedSizedQueue<Line>(lines);
            beingDownloaded = new HashSet<int>();
            Emotes = new List<Emote>();
            webClient = new WebClient();

            this.width = width;
            this.font = font;

            var size = MeasureString("\n<>\n<>\n<>\n<>");
            lineHeight = size.Height / 5.0;
            EmoteSize = Convert.ToInt32(size.Width);
        }

        public void Reset()
        {
            Emotes = new List<Emote>();
            queue = new FixedSizedQueue<Line>(queue.Size);
        }
        
        public String AddMessage(string user, string message, string tags)
        {
            var prefix = string.Format("{0}: ", user);
            var newEmotes = GetEmotes(prefix.Length, tags);

            string fullString = prefix + message;
            var noEmotes = ReplaceEmotes(fullString, newEmotes);
            var wrapped = WordWrap(noEmotes, newEmotes);
            var lines = MakeLines(wrapped, newEmotes);

            foreach (var l in lines)
                queue.Enqueue(l);


            StringBuilder sb = new StringBuilder();
            lock (Emotes)
            {
                Emotes = new List<Emote>();
                int lineNum = 0;
                foreach (var line in queue)
                {
                    sb.AppendLine(line.text);
                    for (int i = 0; i < line.emotes.Count; i++)
                    {
                        var e = line.emotes[i];
                        e.Y = Convert.ToInt32(lineNum * lineHeight);
                        Debug.WriteLine("Y: " + e.Y);
                    }
                    Emotes.AddRange(line.emotes);
                    lineNum++;
                }
                return sb.ToString();
            }
        }


        private List<Emote> GetEmotes(int prefixLen, string tags)
        {
            var tagMap = GetTagMap(tags);
            var newEmotes = new List<Emote>();

            if (!tagMap.ContainsKey("emotes"))
                return newEmotes;

            foreach (var emote in tagMap["emotes"].Split('/'))
            {
                var s = emote.Split(':');
                var id = int.Parse(s[0]);

                DownloadEmote(id);

                foreach (var index in s[1].Split(','))
                {
                    var i = index.Split('-');
                    var start = prefixLen + int.Parse(i[0]);
                    var end = prefixLen + int.Parse(i[1]);
                    newEmotes.Add(new Emote(id, start, end));
                }
            }
            newEmotes.Sort((e1, e2) => e1.start - e2.start);
            return newEmotes;
        }

        private SizeF MeasureString(string s)
        {
            SizeF result;
            using (var image = new Bitmap(1, 1))
            {
                using (var g = Graphics.FromImage(image))
                {
                    result = g.MeasureString(s, font);
                }
            }
            return result;
        }

        private void DownloadEmote(int id)
        {
            lock (beingDownloaded)
            {
                if (beingDownloaded.Contains(id))
                    return;

                beingDownloaded.Add(id);
            }

            var file = string.Format("C:\\Emotes\\{0}.png", id);
            if (!File.Exists(file))
            {
                var url = string.Format("http://static-cdn.jtvnw.net/emoticons/v1/{0}/1.0", id);
                webClient.DownloadFile(url, file);
            }

            lock (beingDownloaded)
            {
                beingDownloaded.Remove(id);
            }
        }

        private IList<Line> MakeLines(string wrapped, IList<Emote> emotes)
        {
            int pos = 0;
            var eol = wrapped.Length;

            var emoteIndex = 0;
            var startOfLine = 0;
            var numLines = 0;
            var currentEmotes = new List<Emote>();
            var lines = new List<Line>();
            while (pos < eol)
            {
                if (wrapped[pos] == '\r' && wrapped[pos + 1] == '\n')
                {
                    var t = wrapped.Substring(startOfLine, pos - startOfLine);
                    var line = new Line(t, currentEmotes);

                    lines.Add(line);
                    startOfLine = pos + 2;
                    numLines++;
                    currentEmotes = new List<Emote>();
                    pos++;
                }
                else if (wrapped[pos] == '<' && wrapped[pos + 1] == '>')
                {
                    var e = emotes[emoteIndex];
                    emoteIndex++;
                    var upToEmote = wrapped.Substring(startOfLine, pos - startOfLine);
                    var size = MeasureString(upToEmote);
                    e.X = Convert.ToInt32(size.Width);
                    e.height = size.Height;
                    currentEmotes.Add(e);
                    pos++;
                }
                pos++;
            }
            return lines;
        }

        private string ReplaceEmotes(string s, IList<Emote> emotes)
        {
            var sb = new StringBuilder(s);

            var removedChars = 0;
            var replacement = "<>";
            for (int i = 0; i < emotes.Count; i++)
            {
                var e = emotes[i];
                var start = e.start - removedChars;
                var end = e.end - removedChars;
                var len = end - start + 1;
                sb.Remove(start, len);
                sb.Insert(start, replacement);
                var removedNow = len - replacement.Length;
                removedChars += removedNow;
            }
            return sb.ToString();
        }

        private IDictionary<string, string> GetTagMap(string tags)
        {
            var dict = new Dictionary<string, string>();
            if (string.IsNullOrEmpty(tags))
                return dict;

            foreach (var pair in tags.Split(';'))
            {
                var s = pair.Split('=');
                if (s[1] != "")
                    dict[s[0]] = s[1];
            }
            return dict;
        }

        private string WordWrap(string the_string, IList<Emote> emotes)
        {
            StringBuilder sb = new StringBuilder();
            int pos = 0;
            var eol = the_string.Length;

            // Copy this line of text, breaking into smaller lines as needed
            do
            {
                int len = eol - pos;

                if (len > width)
                    len = BreakLine(the_string, pos, width);

                sb.Append(the_string, pos, len);
                sb.Append(Environment.NewLine);

                // Trim whitespace following break
                pos += len;

                while (pos < eol && Char.IsWhiteSpace(the_string[pos]))
                    pos++;

            } while (eol > pos);
            return sb.ToString();
        }

        /// <summary>
        /// Locates position to break the given line so as to avoid
        /// breaking words.
        /// </summary>
        /// <param name="text">String that contains line of text</param>
        /// <param name="pos">Index where line of text starts</param>
        /// <param name="max">Maximum line length</param>
        /// <returns>The modified line length</returns>
        private int BreakLine(string text, int pos, int max)
        {
            // Find last whitespace in line
            int i = max - 1;
            while (i >= 0 && !Char.IsWhiteSpace(text[pos + i]))
                i--;
            if (i < 0)
                return max; // No whitespace found; break at maximum length
                            // Find start of whitespace
            while (i >= 0 && Char.IsWhiteSpace(text[pos + i]))
                i--;
            // Return length of text before whitespace
            return i + 1;
        }

    }
}
