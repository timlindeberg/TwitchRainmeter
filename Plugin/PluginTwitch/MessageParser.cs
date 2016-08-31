using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Diagnostics;

namespace PluginTwitch
{
    public class MessageParser
    {
        public int ImageWidth { get; private set; }
        public int ImageHeight { get; private set; }
        public List<Image> Images { get; private set; }

        private Queue<Line> queue;
        private Font font;
        private int maxWidth;
        private int maxHeight;
        private int numSpaces;
        private Graphics graphics;
        private StringFormat format;
        private ImageDownloader imgDownloader;


        public MessageParser(int width, int height, Font font, ImageDownloader imgDownloader)
        {
            queue = new Queue<Line>();
            Images = new List<Image>();
            this.imgDownloader = imgDownloader;

            this.maxWidth = width;
            this.maxHeight = height;
            this.font = font;

            var bitmap = new Bitmap(1, 1);
            graphics = Graphics.FromImage(bitmap);
            graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;
            format = new StringFormat(StringFormat.GenericTypographic) { FormatFlags = StringFormatFlags.MeasureTrailingSpaces };

            Image.ImageString = CalculateImageString();
            var size = MeasureString(Image.ImageString);
            ImageWidth = Convert.ToInt32(size.Width);
            ImageHeight = Convert.ToInt32(size.Height);
        }

        public string CalculateImageString()
        {
            // Calculates the number of spaces that has the most similiar width and height.
            var s = " ";
            var size = MeasureString(s);
            double height = size.Height;
            double width = size.Width;
            double previousWidth = height;
            while(width < height)
            {
                s += " ";
                previousWidth = width;
                width = GetWidth(s);
            }
            if (Math.Abs(previousWidth - height) <= Math.Abs(width - height))
                return s.Substring(1);
            else
                return s;
        }

        public void Reset()
        {
            Images = new List<Image>();
            queue = new Queue<Line>();
        }

        public String AddMessage(string user, string message, string tags)
        {
            var words = GetWords(user, message, tags);
            var lines = WordWrap(words);

            return BuildNewString(lines);
        }

        private string BuildNewString(List<Line> lines)
        {
            lock (queue)
            {
                lines.ForEach(l => queue.Enqueue(l));
                ResizeQueue();

                // Calculate image positions and build final string
                var sb = new StringBuilder();
                lock (Images)
                {
                    Images = new List<Image>();
                    var currentHeight = 0.0;
                    foreach (var line in queue)
                    {
                        foreach (var img in line.Images)
                        {
                            img.Y = Convert.ToInt32(currentHeight);
                            Images.Add(img);
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

        private List<Word> GetWords(string user, string msg, string tags)
        {
            var tagMap = GetTagMap(tags);

            var badges = GetBadges(tagMap);
            var emotes = GetEmotes(tagMap);

            var prefix = new Word(string.Format("<{0}>:", user));

            List<Word> words = new List<Word>();
            words.AddRange(badges);
            words.Add(prefix);

            var emoteIndex = 0;
            var lastWord = 0;
            for (int pos = 0; pos < msg.Length; pos++)
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

                if (Char.IsWhiteSpace(msg[pos]))
                {
                    words.Add(new Word(msg.Substring(lastWord, pos - lastWord)));
                    lastWord = pos + 1;
                }
            }
            if (lastWord < msg.Length)
                words.Add(new Word(msg.Substring(lastWord, msg.Length - lastWord)));
            return words;
        }

        private List<Image> GetBadges(IDictionary<string, string> tagMap)
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
                    badges.Add(new Image(fileName, 0, 0));
                }
            }
            return badges;
        }

        private List<Image> GetEmotes(IDictionary<string, string> tagMap)
        {
            var emotes = new List<Image>();

            if (!tagMap.ContainsKey("emotes"))
                return emotes;

            foreach (var emote in tagMap["emotes"].Split('/'))
            {
                var s = emote.Split(':');
                var id = s[0];

                imgDownloader.DownloadEmote(id);

                foreach (var index in s[1].Split(','))
                {
                    var i = index.Split('-');
                    var start = int.Parse(i[0]);
                    var end = int.Parse(i[1]);
                    emotes.Add(new Image(id, start, end));
                }
            }
            emotes.Sort((e1, e2) => e1.start - e2.start);
            return emotes;
        }

        private List<Line> WordWrap(List<Word> words)
        {
            var lines = new List<Line>();
            var currentLength = 0.0;
            var currentLine = new Line();
            while (true)
            {
                var currentWord = words[0];
                if(currentWord is Image)
                {
                    var length = currentLength;
                    if (currentLine.Text != string.Empty)
                        length = GetWidth(currentLine.Text + " ");
                    var img = currentWord as Image;
                    img.X = Convert.ToInt32(currentLength);
                }
                
                string newString = (currentLine.Text + ' ') + currentWord.String;
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
            for (breakPoint = 1; breakPoint <= str.Length; breakPoint++)
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
