using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Diagnostics;

namespace PluginTwitchChat
{
    public class MessageHandler
    {
        public string String { get; set; }
        public int ImageWidth { get; private set; }
        public int ImageHeight { get; private set; }
        public List<Image> Images { get; private set; }
        public List<Link> Links { get; private set; }
        public Line Seperator;

        private string SeperatorSign;
        private Queue<Line> lineQueue;
        private Queue<Message> msgQueue;

        private int maxWidth;
        private int maxHeight;
        private double spaceWidth;
        private ImageDownloader imgDownloader;
        private StringMeasurer measurer;

        private static Regex URLRegex = new Regex(@"(https?:\/\/(?:www\.|(?!www))[^\s\.]+\.[^\s]{2,}|www\.[^\s]+\.[^\s]{2,})");

        public MessageHandler(int width, int height, StringMeasurer measurer, String seperatorSign, ImageDownloader imgDownloader)
        {
            lineQueue = new Queue<Line>();
            msgQueue = new Queue<Message>();
            Images = new List<Image>();
            Links = new List<Link>();
            String = "";
            this.maxWidth = width;
            this.maxHeight = height;
            this.measurer = measurer;
            this.imgDownloader = imgDownloader;
            this.measurer = measurer;
            this.SeperatorSign = seperatorSign;

            Image.ImageString = CalculateImageString();
            var size = measurer.MeasureString(Image.ImageString);
            CalculateSeperator();
            ImageWidth = Convert.ToInt32(size.Width);
            ImageHeight = Convert.ToInt32(size.Height);
            spaceWidth = measurer.GetWidth(" ");
        }

        public void Update()
        {
            if (msgQueue.Count == 0)
                return;

            lock (msgQueue)
            {
                while (msgQueue.Count > 0)
                    msgQueue.Dequeue().AddLines(this);
            }

            ResizeLineQueue();
            UpdateData();
        }

        // Calculate image Y positions and build final string
        public void UpdateData()
        {
            var sb = new StringBuilder();
            Images = new List<Image>();
            Links = new List<Link>();
            var currentHeight = 0.0;
            foreach (var line in lineQueue)
            {
                var prevHeight = currentHeight;
                sb.AppendLine(line.Text);
                currentHeight = measurer.GetHeight(sb.ToString());

                foreach (var img in line.Images)
                {
                    img.Y = Convert.ToInt32(prevHeight);
                    Images.Add(img);
                }
                foreach (var link in line.Links)
                {
                    link.Y = Convert.ToInt32(prevHeight);
                    link.Height = Convert.ToInt32(currentHeight - prevHeight);
                    Links.Add(link);
                }
            }
            String = sb.ToString();
        }

        public Image GetImage(int index)
        {
            if (index < 0 || index >= Images.Count)
                return null;

            return Images[index];
        }

        public Link GetLink(int index)
        {
            if (index < 0 || index >= Links.Count)
                return null;

            return Links[index];
        }

        public void Reset()
        {
            Images = new List<Image>();
            Links = new List<Link>();
            String = "";
            lineQueue = new Queue<Line>();
            msgQueue = new Queue<Message>();
        }

        public void AddSeperator(List<Line> lines)
        {
            if (Seperator != null)
                lines.Add(Seperator);
        }

        public void AddLines(List<Line> lines)
        {
            foreach (var l in lines)
                lineQueue.Enqueue(l);
        }

        public void AddMessage(Message msg)
        {
            lock (msgQueue)
                msgQueue.Enqueue(msg);
        }

        public List<Word> GetWords(string user, string msg, Tags tags)
        {
            var badges = tags.GetBadges();
            var emotes = tags.GetEmotes();

            var prefix = new Word(string.Format("<{0}>:", user));

            var words = new List<Word>();
            words.AddRange(badges);
            words.Add(prefix);
            words.AddRange(GetWords(msg, emotes));
            return words;
        }

        public List<Word> GetWords(string msg, List<EmoteInfo> emotes)
        {
            var emoteIndex = 0;
            var lastWord = 0;
            List<Word> words = new List<Word>();
            for (int pos = 0; pos < msg.Length; pos++)
            {
                if (emotes != null && emoteIndex < emotes.Count)
                {
                    var emote = emotes[emoteIndex];
                    if (pos == emote.Start)
                    {
                        imgDownloader.DownloadEmote(emote.ID);
                        var displayName = msg.Substring(emote.Start, emote.Length + 1);
                        var img = new Image(emote.ID, displayName);
                        words.Add(img);
                        pos = emote.End + 1;
                        lastWord = pos + 1;
                        emoteIndex++;
                        continue;
                    }
                }

                if (Char.IsWhiteSpace(msg[pos]))
                {
                    AddWord(words, msg, lastWord, pos - lastWord);
                    lastWord = pos + 1;
                }
            }
            if (lastWord < msg.Length)
                AddWord(words, msg, lastWord, msg.Length - lastWord);
            return words;
        }

        public void AddWord(List<Word> words, string msg, int start, int len)
        {
            var s = msg.Substring(start, len);
            var match = URLRegex.Match(s);
            if(!match.Success)
            {
                words.Add(new Word(s));
                return;
            }

            var index = match.Index;
            if(index == 0)
            {
                words.Add(new Link(s));
                return;
            }

            words.Add(new Word(s.Substring(0, index - 1)));
            words.Add(new Link(s.Substring(index, s.Length - index)));
        }

        public List<Line> WordWrap(List<Word> words)
        {
            return WordWrap(words, new List<Line>());
        }

        public List<Line> WordWrap(List<Word> words, List<Line> lines)
        {
            var len = 0.0;
            var line = new Line();
            for (int i = 0; i < words.Count; i++)
            {
                bool isEmpty = line.Text == string.Empty;
                var word = words[i];

                string newString = isEmpty ? word : (line.Text + ' ' + word);
                var newLen = measurer.GetWidth(newString);

                if(word is Image)
                {
                    var x = isEmpty ? len : len + spaceWidth;
                    var img = word as Image;
                    img.X = Convert.ToInt32(x);
                }
                else if(word is Link)
                {
                    var x = isEmpty ? len : len + spaceWidth;
                    var link = word as Link;
                    link.X = Convert.ToInt32(x);
                    link.Width = Convert.ToInt32(newLen - x);
                }

                if(newLen <= maxWidth)
                {
                    line.Add(word);
                    len = newLen;
                    continue;
                }

                // Word no longer fits in line.
                if (isEmpty || measurer.GetWidth(word) > maxWidth)
                {
                    // Either the current line is empty and the word doesn't fit on one line
                    // or the line is not empty but the word won't fit in itself either.
                    int breakPoint = FindBreakpoint(newString);
                    var start = isEmpty ? 0 : line.Text.Length + 1;
                    var split = SplitWord(word, start, breakPoint, newString);
                    line.Add(split.Item1);
                    words[i] = split.Item2; // revisit the part that didn't get added
                }
                lines.Add(line);
                line = new Line();
                len = 0.0;
                i--; // revisit this word
            }

            if(!line.IsEmpty)
                lines.Add(line);

            return lines;
        }

        private Tuple<Word, Word> SplitWord(Word w, int start, int breakPoint, string newString)
        {
            var s1 = newString.Substring(start, breakPoint - start);
            var s2 = newString.Substring(breakPoint, newString.Length - breakPoint);
            if (w is Link)
            {
                var link = w as Link;
                var url = link.Url;
                var l1 = new Link(link.Url, s1);
                l1.X = link.X;
                l1.Width = maxWidth - link.X;
                var l2 = new Link(link.Url, s2);
                // l2 will get positional information later.
                return new Tuple<Word, Word>(l1, l2);
            }
            return new Tuple<Word, Word>(new Word(s1), new Word(s2));
        }

        // Resize the line queue to fit the maximum height
        private void ResizeLineQueue()
        {
            StringBuilder sb = new StringBuilder();
            foreach (var line in lineQueue.ToList())
            {
                sb.AppendLine(line.Text);
                var str = sb.ToString();
                var height = measurer.GetHeight(str);
                while (height > maxHeight && lineQueue.Count > 1) // Keep at least one line in the queue
                {
                    var firstLine = lineQueue.Dequeue();
                    sb.Remove(0, firstLine.Text.Length + Environment.NewLine.Length);
                    str = sb.ToString();
                    height = measurer.GetHeight(str);
                }
            }
        }

        private int FindBreakpoint(string str)
        {
            int breakPoint;
            int start = 1;
            int end = str.Length;
            while (start < end)
            {
                int mid = (end + start) / 2;
                var wordLen = measurer.GetWidth(str.Substring(0, mid));
                if (wordLen <= maxWidth)
                    start = mid + 1;
                else
                    end = mid;
            }
            return start - 1;
        }

        private void CalculateSeperator()
        {
            if (SeperatorSign == "")
                return;

            Seperator = new Line();
            var c = SeperatorSign[0];
            string s = "";
            double w;
            do
            {
                s += c;
                w = measurer.GetWidth(s);
            } while (w < maxWidth);

            Seperator.Add(s.Substring(1));
        }

        // Calculates the number of spaces that has the most similiar width and height.
        // Used in place of images within the string
        private string CalculateImageString()
        {
            var spaces = " ";
            var size = measurer.MeasureString(spaces);
            double height = size.Height;
            double width = size.Width;
            double previousWidth = height;
            while (width < height)
            {
                spaces += " ";
                previousWidth = width;
                width = measurer.GetWidth(spaces);
            }
            return Math.Abs(previousWidth - height) <= Math.Abs(width - height) ? spaces.Substring(1) : spaces;
        }
    }
}
