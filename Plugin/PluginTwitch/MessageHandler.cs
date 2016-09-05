using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
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
        public Line Seperator;

        private string SeperatorSign;
        private Queue<Line> lineQueue;
        private Queue<Message> msgQueue;

        private int maxWidth;
        private int maxHeight;
        private ImageDownloader imgDownloader;
        private StringMeasurer measurer;


        public MessageHandler(int width, int height, StringMeasurer measurer, String seperatorSign, ImageDownloader imgDownloader)
        {
            lineQueue = new Queue<Line>();
            msgQueue = new Queue<Message>();
            Images = new List<Image>();
            this.maxWidth = width;
            this.maxHeight = height;
            this.measurer = measurer;
            this.imgDownloader = imgDownloader;
            this.measurer = measurer;
            this.SeperatorSign = seperatorSign;

            Image.ImageString = CalculateImageString();
            var size = measurer.MeasureString(Image.ImageString);
            if(SeperatorSign != "")
            {
                Seperator = new Line();
                Seperator.Add(CalculateSeperator());
            }
            ImageWidth = Convert.ToInt32(size.Width);
            ImageHeight = Convert.ToInt32(size.Height);
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

            ResizeQueue();

            // Calculate image Y positions and build final string
            var sb = new StringBuilder();
            Images = new List<Image>();
            var currentHeight = 0.0;
            foreach (var line in lineQueue)
            {
                foreach (var img in line.Images)
                {
                    img.Y = Convert.ToInt32(currentHeight);
                    Images.Add(img);
                }
                sb.AppendLine(line.Text);
                currentHeight = measurer.GetHeight(sb.ToString());
            }
            String = sb.ToString();
        }

        public void Reset()
        {
            Images = new List<Image>();
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
            {
                msgQueue.Enqueue(msg);
            }
        }

        public List<Word> GetWords(string user, string msg, Tags tags)
        {
            var badges = tags.GetBadges();
            var emotes = tags.GetEmotes();

            var prefix = new Word(string.Format("<{0}>:", user));

            List<Word> words = new List<Word>();
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
                    words.Add(new Word(msg, lastWord, pos - lastWord));
                    lastWord = pos + 1;
                }
            }
            if (lastWord < msg.Length)
                words.Add(new Word(msg, lastWord, msg.Length - lastWord));
            return words;
        }


        public List<Line> WordWrap(List<Word> words)
        {
            return WordWrap(words, new List<Line>());
        }

        public List<Line> WordWrap(List<Word> words, List<Line> lines)
        {
            var currentLength = 0.0;
            var currentLine = new Line();
            var spaceWidth = measurer.GetWidth(" ");
            for (int i = 0; i < words.Count; i++)
            {
                var currentWord = words[i];
                if(currentWord is Image)
                {
                    var length = (currentLine.Text == "") ? currentLength : currentLength + spaceWidth;
                    var img = currentWord as Image;
                    img.X = Convert.ToInt32(currentLength);
                }


                string newString;
                if (currentLine.Text == string.Empty)
                    newString = currentWord.String;
                else
                    newString = (currentLine.Text + ' ') + currentWord.String;

                var len = measurer.GetWidth(newString);
                if(len <= maxWidth)
                {
                    currentLine.Add(currentWord);
                    currentLength = len;
                    continue;
                }

                // Word no longer fits in line.
                if (currentLine.Text == string.Empty)
                {
                    // Word is longer than a line, find break point.
                    int breakPoint = FindBreakpoint(newString);
                    currentLine.Add(new Word(newString.Substring(0, breakPoint)));
                    words[i] = new Word(newString.Substring(breakPoint, newString.Length - breakPoint));
                }
                lines.Add(currentLine);
                currentLine = new Line();
                currentLength = 0.0;
                i--; // revisit this word
            }

            if(!currentLine.IsEmpty)
                lines.Add(currentLine);

            return lines;
        }

        private void ResizeQueue()
        {
            // Resize the queue to fit the maximum height
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
            // This is very slow but maybe it doesnt matter since words longer than 
            // the width should be fairly uncommon.
            // One could use binary search to find the breakpoint faster
            int breakPoint;
            for (breakPoint = 1; breakPoint <= str.Length; breakPoint++)
            {
                var wordLen = measurer.GetWidth(str.Substring(0, breakPoint));
                if (wordLen >= maxWidth)
                    return breakPoint - 1;
            }
            return -1;
        }

        private string CalculateSeperator()
        {
            var c = SeperatorSign[0];
            string s = "";
            double w;
            do
            {
                s += c;
                w = measurer.GetWidth(s);
            } while (w < maxWidth);

            return s.Substring(1);
        }

        // Calculates the number of spaces that has the most similiar width and height.
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
