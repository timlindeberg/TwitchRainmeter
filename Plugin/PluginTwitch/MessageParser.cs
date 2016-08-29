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
        private int numSpaces;
        private string placeHolder;
        private string spaceReplacement;
        private Graphics graphics;
        private StringFormat format;


        public MessageParser(int lines, int width, Font font)
        {
            queue = new FixedSizedQueue<Line>(lines);
            beingDownloaded = new HashSet<int>();
            Emotes = new List<Emote>();
            webClient = new WebClient();

            this.width = width;
            this.font = font;

            var bitmap = new Bitmap(1, 1);
            graphics = Graphics.FromImage(bitmap);
            format = new StringFormat(StringFormat.GenericTypographic) { FormatFlags = StringFormatFlags.MeasureTrailingSpaces };

            var spaceSize = MeasureString(" ").Width;
            var size = 25.0; // An emote is generally around 25 pixels
            var emoteSize = 0.0;
            while(emoteSize < size)
                emoteSize += spaceSize;

            emoteSize -= spaceSize;
            int numSpaces = Convert.ToInt32(emoteSize / spaceSize);
            for (int i = 0; i <= numSpaces; i++)
            {
                placeHolder += "^";
                spaceReplacement += " ";
            }
            EmoteSize = Convert.ToInt32(emoteSize);
        }

        public void Reset()
        {
            Emotes = new List<Emote>();
            queue = new FixedSizedQueue<Line>(queue.Size);
        }
        
        public String AddMessage(string user, string message, string tags)
        {
            var prefix = string.Format("{0}: ", user);
            var emotes = GetEmotes(prefix.Length, tags);

            string fullString = prefix + message;

            var replaceEmotes = ReplaceEmotes(fullString, emotes);
            var wrapped = WordWrap(replaceEmotes, emotes);
            var replacedPlaceholders = wrapped.Replace(placeHolder, spaceReplacement);
            var lines = MakeLines(replacedPlaceholders, emotes);

            Debug.WriteLine("Lines:\n" + string.Join("\n", lines));

            foreach (var l in lines)
                queue.Enqueue(l);

            StringBuilder sb = new StringBuilder();
            lock (Emotes)
            {
                Emotes = new List<Emote>();
                int lineNum = 0;
                foreach (var line in queue)
                {
                    for (int i = 0; i < line.emotes.Count; i++)
                    {
                        var e = line.emotes[i];
                        var y = MeasureString(sb.ToString()).Height;
                        e.Y = Convert.ToInt32(y);
                        Emotes.Add(e);
                    }
                    sb.AppendLine(line.text);
                    lineNum++;
                }
                return sb.ToString();
            }
        }


        private string DebugPositions(string s, List<Emote> emotes)
        {
            var sb = new StringBuilder();
            var pos = 0;
            var emote = 0;
            while (pos < s.Length)
            {
                if (emote < emotes.Count)
                {
                    var e = emotes[emote];
                    if (pos == e.start)
                    {
                        sb.Append("@");
                        emote++;
                    }
                }
                sb.Append(s[pos]);
                pos++;
            }
            return sb.ToString();
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
            return graphics.MeasureString(s, font, 10000, format);
        }

        private string ReplaceEmotes(string s, IList<Emote> emotes)
        {
            var sb = new StringBuilder(s);
            var removedChars = 0;
            for (int i = 0; i < emotes.Count; i++)
            {
                var e = emotes[i];
                var start = e.start - removedChars;
                var end = e.end - removedChars;
                var len = end - start + 1;
                sb.Remove(start, len);
                sb.Insert(start, placeHolder);
                var removedNow = len - placeHolder.Length;
                removedChars += removedNow;
                e.start = start;
            }
            return sb.ToString();
        }

        private string WordWrap(string str, IList<Emote> emotes)
        {
            StringBuilder sb = new StringBuilder();
            int pos = 0;
            var eol = str.Length;

            var newLine = "\r\n";
            var emotePos = 0;
            // Copy this line of text, breaking into smaller lines as needed
            do
            {
                int len = eol - pos;

                if (len > width)
                    len = BreakLine(str, pos, width);

                sb.Append(str, pos, len);
                sb.Append(newLine);

                pos += len;
                var lineBreak = pos;

                // Trim whitespace following break
                var whiteSpaces = 0;
                while (pos < eol && Char.IsWhiteSpace(str[pos]))
                {
                    pos++;
                    whiteSpaces++;
                }

                // Update the starting indexes of the emotes
                // Advance index to next emote after the line break
                while (emotePos < emotes.Count && emotes[emotePos].start <= lineBreak)
                    emotePos++;

                // Add the length of the added new line minus the removed whitespaces to the
                // emotes following the line break
                for (int i = emotePos; i < emotes.Count; i++)
                    emotes[i].start += (newLine.Length - whiteSpaces);

            } while (eol > pos);
            return sb.ToString();
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
                    var lineText = wrapped.Substring(startOfLine, pos - startOfLine);
                    var line = new Line(lineText, currentEmotes);

                    lines.Add(line);
                    startOfLine = pos + 2;
                    numLines++;
                    currentEmotes = new List<Emote>();
                    pos++;
                }
                else if (emoteIndex < emotes.Count)
                {
                    var e = emotes[emoteIndex];
                    if (e.start == pos)
                    {
                        var upToEmote = wrapped.Substring(startOfLine, pos - startOfLine);
                        var size = MeasureString(upToEmote);
                        e.X = Convert.ToInt32(size.Width + (EmoteSize / 2.0));
                        currentEmotes.Add(e);
                        emoteIndex++;
                    }
                }
                pos++;
            }
            return lines;
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

    }
}
