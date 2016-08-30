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

        private Queue<Line> queue;
        private HashSet<int> beingDownloaded;
        private Font font;
        private WebClient webClient;
        private int maxWidth;
        private int maxHeight;
        private int numSpaces;
        private string spaceReplacement;
        private Graphics graphics;
        private StringFormat format;
        private string emoteDir;


        public MessageParser(int width, int height, string emoteDir, Font font)
        {
            queue = new Queue<Line>();
            beingDownloaded = new HashSet<int>();
            Emotes = new List<Emote>();
            webClient = new WebClient();

            this.maxWidth = width;
            this.maxHeight = height;
            this.font = font;
            this.emoteDir = emoteDir;

            var bitmap = new Bitmap(1, 1);
            graphics = Graphics.FromImage(bitmap);
            format = new StringFormat(StringFormat.GenericTypographic) { FormatFlags = StringFormatFlags.MeasureTrailingSpaces };

            var spaceSize = MeasureString(" ").Width;
            var size = 30.0; // An emote is generally around 25 pixels
            var emoteSize = 0.0;
            while(emoteSize < size)
                emoteSize += spaceSize;

            emoteSize -= spaceSize;
            int numSpaces = Convert.ToInt32(emoteSize / spaceSize);
            for (int i = 0; i < numSpaces; i++)
                spaceReplacement += " ";

            EmoteSize = Convert.ToInt32(emoteSize);
        }

        public void Reset()
        {
            Emotes = new List<Emote>();
            queue = new Queue<Line>();
        }
        
        public String AddMessage(string user, string message, string tags)
        {
            var prefix = string.Format("{0}: ", user);
            var emotes = GetEmotes(prefix.Length, tags);

            string fullString = prefix + message;

            var words = GetWords(fullString, emotes);
            var lines = WordWrap(words);

            return BuildNewString(lines):
        }

        private string BuildNewString(List<Line> lines)
        {
            lock (queue)
            {
                lines.ForEach(l => queue.Enqueue(l));
                ResizeQueue();

                // Calculate image positions and build final string
                var sb = new StringBuilder();
                lock (Emotes)
                {
                    Emotes = new List<Emote>();
                    var currentHeight = 0.0;
                    foreach (var line in queue)
                    {
                        foreach (var e in line.Emotes)
                        {
                            e.Y = Convert.ToInt32(currentHeight);
                            Emotes.Add(e);
                        }
                        sb.AppendLine(line.Text);
                        currentHeight = GetHeight(sb.ToString());
                    }
                    return sb.ToString();
                }
            }
        }

        private void ResizeQueue()
        {
            // Resize the queue to fit the maximum height
            StringBuilder sb = new StringBuilder();
            foreach (var line in queue.ToList())
            {
                sb.AppendLine(line.Text);
                var str = sb.ToString();
                var height = GetHeight(str);
                while (height > maxHeight)
                {
                    var firstLine = queue.Dequeue();
                    sb.Remove(0, firstLine.Text.Length + Environment.NewLine.Length);
                    str = sb.ToString();
                    height = GetHeight(str);
                }
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
                    newEmotes.Add(new Emote(spaceReplacement, id, start, end));
                }
            }
            newEmotes.Sort((e1, e2) => e1.start - e2.start);
            return newEmotes;
        }

        private List<Word> GetWords(string str, List<Emote> emotes)
        {
            List<Word> words = new List<Word>();
            var emoteIndex = 0;
            var lastWord = 0;
            for (int pos = 0; pos < str.Length; pos++)
            {
                if (emoteIndex < emotes.Count)
                {
                    var nextEmote = emotes[emoteIndex];
                    if (pos == nextEmote.start)
                    {
                        words.Add(nextEmote);
                        pos = nextEmote.end + 1;
                        lastWord = pos + 1;
                        emoteIndex++;
                        continue;
                    }
                }

                if (Char.IsWhiteSpace(str[pos]))
                {
                    words.Add(new Word(str.Substring(lastWord, pos - lastWord)));
                    lastWord = pos + 1;
                }
            }
            if (lastWord < str.Length)
                words.Add(new Word(str.Substring(lastWord, str.Length - lastWord)));
            return words;
        }

        private List<Line> WordWrap(List<Word> words)
        {
            var lines = new List<Line>();
            var currentLength = 0.0;
            string lastWord = "";
            var currentLine = new Line();
            while (true)
            {
                var currentWord = words[0];
                if(currentWord is Emote)
                {
                    var e = currentWord as Emote;
                    e.X = Convert.ToInt32(currentLength + EmoteSize / 2.0);
                }
                
                string newString = (currentLine.Text + ' ').TrimStart(' ') + currentWord.String;
                var len = GetWidth(newString);
                if(len > maxWidth)
                {
                    if(currentLine.Text == string.Empty)
                    {
                        // Word is longer than a line, find break point.
                        int breakPoint = FindBreakpoint(newString);
                        currentLine.Add(new Word(newString.Substring(0, breakPoint)));
                        words[0] = new Word(newString.Substring(breakPoint, newString.Length - breakPoint));
                    }
                    // Word no longer fits in line.
                    lines.Add(currentLine);
                    currentLine = new Line();
                    currentLength = 0.0;
                }
                else
                {
                    words.RemoveAt(0);
                    currentLine.Add(currentWord);
                    currentLength = len;
                }

                if (words.Count == 0)
                {
                    lines.Add(currentLine);
                    break;
                }
            }
            return lines;
        }

        private int FindBreakpoint(string str)
        {
            // This is very slow but maybe it doesnt matter since words longer than 
            // the width should be fairly uncommon.
            // One could use binary search to find the breakpoint faster
            int breakPoint;
            for (breakPoint = 1; breakPoint < str.Length; breakPoint++)
            {
                var wordLen = GetWidth(str.Substring(0, breakPoint));
                if (wordLen >= maxWidth)
                    return breakPoint - 1;
            }
            return -1;
        }

        private double GetWidth(string s)
        {
            return MeasureString(s).Width;
        }

        private double GetHeight(string s)
        {
            return MeasureString(s).Height;
        }

        private SizeF MeasureString(string s)
        {
            return graphics.MeasureString(s, font, 10000, format);
        }

        private void DownloadEmote(int id)
        {
            lock (beingDownloaded)
            {
                if (beingDownloaded.Contains(id))
                    return;

                beingDownloaded.Add(id);
            }

            var file = string.Format("{0}\\{1}.png", emoteDir, id);
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
