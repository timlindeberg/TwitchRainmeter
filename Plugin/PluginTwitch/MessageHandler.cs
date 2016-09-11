﻿using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;

namespace PluginTwitchChat
{
    public class MessageHandler
    {
        public string String { get; set; }
        public Size ImageSize { get; private set; }
        public List<Image> Images { get; private set; }
        public List<AnimatedImage> Gifs { get; private set; }
        public List<Link> Links { get; private set; }
        public Line Seperator;
        public string ImageString;

        private static readonly Regex URLRegex = new Regex(@"(https?:\/\/(?:www\.|(?!www))[^\s\.]+\.[^\s]{2,}|www\.[^\s]+\.[^\s]{2,})");
        private static readonly Regex CheerRegex = new Regex(@"(?:^|\s)cheer(\d+)(?:\s|$)");
        private static readonly string SeperatorSign = "─";

        private Queue<Line> lineQueue;
        private Queue<Message> msgQueue;

        private Size max;
        private double spaceWidth;
        private ImageDownloader imgDownloader;
        private StringMeasurer measurer;


        public MessageHandler(Size maxSize, StringMeasurer measurer, bool useSeperator, ImageDownloader imgDownloader)
        {
            lineQueue = new Queue<Line>();
            msgQueue = new Queue<Message>();
            Images = new List<Image>();
            Gifs = new List<AnimatedImage>();
            Links = new List<Link>();
            String = "";
            this.max = maxSize;
            this.measurer = measurer;
            this.imgDownloader = imgDownloader;
            this.measurer = measurer;

            ImageString = CalculateImageString();
            var width = measurer.GetWidth(ImageString);
            var height = measurer.GetHeight("A");
            if(useSeperator)
                CalculateSeperator();
            ImageSize = new Size { Width = Convert.ToInt32(width), Height = Convert.ToInt32(height) };
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

        public Image GetImage(int index)
        {
            if (index < 0 || index >= Images.Count)
                return null;

            return Images[index];
        }

        public AnimatedImage GetGif(int index)
        {
            if (index < 0 || index >= Gifs.Count)
                return null;

            return Gifs[index];
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

        public List<Word> GetWords(string msg, Tags tags)
        {
            var badges = new List<Image>();
            foreach(var badge in tags.Badges)
            {
                var displayName = char.ToUpper(badge[0]) + badge.Substring(1);
                badges.Add(new Image(badge, displayName, ImageString));
            }
            var emotes = tags.Emotes;

            var user = tags["display-name"];
            var prefix = new Word(string.Format("<{0}>:", user));

            var words = new List<Word>();
            words.AddRange(badges);
            words.Add(prefix);
            words.AddRange(GetWords(msg, emotes, tags.Bits));
            return words;
        }

        public List<Word> GetWords(string msg, List<EmoteInfo> emotes = null, int bits = -1)
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
                        var img = new Image(emote.ID, displayName, ImageString);
                        words.Add(img);
                        pos = emote.End + 1;
                        lastWord = pos + 1;
                        emoteIndex++;
                        continue;
                    }
                }

                if (Char.IsWhiteSpace(msg[pos]))
                {
                    AddWord(words, msg, ref bits, lastWord, pos - lastWord);
                    lastWord = pos + 1;
                }
            }
            if (lastWord < msg.Length)
                AddWord(words, msg, ref bits, lastWord, msg.Length - lastWord);
            return words;
        }

        public void AddWord(List<Word> words, string msg, ref int bits, int start, int len)
        {
            var s = msg.Substring(start, len);
            var urlMatch = URLRegex.Match(s);
            if (!urlMatch.Success)
            {
                AddCheerOrWord(words, s, ref bits);
                return;
            }

            var index = urlMatch.Index;
            if(index == 0)
            {
                words.Add(new Link(s));
                return;
            }

            var s2 = s.Substring(0, index - 1);
            AddCheerOrWord(words, s2, ref bits);
            words.Add(new Link(s, index, s.Length - index));
        }

        public void AddCheerOrWord(List<Word> words, string s, ref int bits)
        {
            var cheerMatch = CheerRegex.Match(s);
            if (!cheerMatch.Success)
            {
                words.Add(new Word(s));
                return;
            }

            var bitsInCheer = int.Parse(cheerMatch.Groups[1].Value);
            if(bits < bitsInCheer)
            {
                words.Add(new Word(s));
                return;
            }

            bits -= bitsInCheer;
            var roundedBits = RoundBits(bitsInCheer);
            
            var gifPath = imgDownloader.DownloadCheer(roundedBits);
            var name = "cheer" + roundedBits;
            var displayName = "Cheer" + bitsInCheer;
            var imageString = ImageString + " " + bitsInCheer;
            var animatedImg = new AnimatedImage(name, displayName, imageString, gifPath);
            words.Add(animatedImg);
        }

        public List<Line> WordWrap(List<Word> words)
        {
            return WordWrap(words, new List<Line>());
        }

        public List<Line> WordWrap(List<Word> words, List<Line> lines)
        {
            var len = 0.0;
            var line = new Line(measurer);
            for (int i = 0; i < words.Count; i++)
            {
                bool isEmpty = line.Text == string.Empty;
                var word = words[i];

                string newString = isEmpty ? word : (line.Text + ' ' + word);
                var newLen = measurer.GetWidth(newString);

                var x = isEmpty ? len : len + spaceWidth;
                if(word is Positioned)
                {
                    var pos = word as Positioned;
                    pos.X = Convert.ToInt32(x);
                    pos.Width = Convert.ToInt32(newLen - x);
                }

                if(newLen <= max.Width)
                {
                    line.Add(word);
                    len = newLen;
                    continue;
                }

                // Word no longer fits in line.
                if (isEmpty || measurer.GetWidth(word) > max.Width)
                {
                    // Either the current line is empty and the word doesn't fit on one line
                    // or the line is not empty but the word won't fit in itself either.
                    var start = isEmpty ? 0 : line.Text.Length + 1;
                    var split = SplitWord(word, start, newString);
                    line.Add(split.Item1);
                    words[i] = split.Item2; // revisit the part that didn't get added
                }
                lines.Add(line);
                line = new Line(measurer);
                len = 0.0;
                i--; // revisit this word
            }

            if(!line.IsEmpty)
                lines.Add(line);

            return lines;
        }

        private Tuple<Word, Word> SplitWord(Word word, int start, string newString)
        {
            int breakPoint = FindBreakpoint(newString);
            var s1 = newString.Substring(start, breakPoint - start);
            var s2 = newString.Substring(breakPoint, newString.Length - breakPoint);
            if(!(word is Link))
                return new Tuple<Word, Word>(new Word(s1), new Word(s2));

            var link = word as Link;
            var l1 = new Link(link.Url, s1) { X = link.X, Width = max.Width - link.X };
            var l2 = new Link(link.Url, s2);
            // l2 will get positional information later.
            return new Tuple<Word, Word>(l1, l2);
        }

        private int FindBreakpoint(string str)
        {
            int start = 1;
            int end = str.Length;
            while (start < end)
            {
                int mid = (end + start) / 2;
                var wordLen = measurer.GetWidth(str.Substring(0, mid));
                if (wordLen <= max.Width)
                    start = mid + 1;
                else
                    end = mid;
            }
            return start - 1;
        }

        // Resize the line queue to fit the maximum height
        private void ResizeLineQueue()
        {
            StringBuilder sb = new StringBuilder();
            foreach (var line in lineQueue.ToList())
            {
                sb.AppendLine(line.Text);
                var height = measurer.GetHeight(sb);
                while (height > max.Height && lineQueue.Count > 1) // Keep at least one line in the queue
                {
                    var firstLine = lineQueue.Dequeue();
                    sb.Remove(0, firstLine.Text.Length + Environment.NewLine.Length);
                    height = measurer.GetHeight(sb);
                }
            }
        }

        // Calculate Y positions and build final string
        private void UpdateData()
        {
            var sb = new StringBuilder();

            var positioned = new List<Positioned>();
            var currentHeight = 0.0;
            foreach (var line in lineQueue)
            {
                var prevHeight = currentHeight;
                sb.AppendLine(line.Text);
                currentHeight = measurer.GetHeight(sb);

                foreach(var pos in line.Positioned)
                {
                    pos.Y = Convert.ToInt32(prevHeight);
                    pos.Height = Convert.ToInt32(currentHeight - prevHeight);
                    positioned.Add(pos);
                }
            }
            String = sb.ToString();
            UpdatePositionedVars(positioned);
        }

        private void UpdatePositionedVars(List<Positioned> positioned)
        {
            Images = new List<Image>();
            Gifs = new List<AnimatedImage>();
            Links = new List<Link>();
            foreach (var pos in positioned)
            {
                if (pos is AnimatedImage)
                    Gifs.Add(pos as AnimatedImage);
                else if (pos is Image)
                    Images.Add(pos as Image);
                else if (pos is Link)
                    Links.Add(pos as Link);
            }
        }

        private void CalculateSeperator()
        {
            Seperator = new Line(measurer);
            var sb = new StringBuilder("");
            double w;
            do
            {
                sb.Append(SeperatorSign);
                w = measurer.GetWidth(sb);
            } while (w < max.Width);
            sb.Remove(0, 1);
            Seperator.Add(sb.ToString());
        }

        // Calculates the number of spaces that has the most similiar width and height.
        // Used in place of images within the string
        private string CalculateImageString()
        {
            var spaces = " ";
            var size = measurer.MeasureString(spaces);
            double height = measurer.GetHeight("A");
            double width = measurer.GetWidth(spaces);
            double previousWidth = height;
            while (width < height)
            {
                spaces += " ";
                previousWidth = width;
                width = measurer.GetWidth(spaces);
            }
            return Math.Abs(previousWidth - height) <= Math.Abs(width - height) ? spaces.Substring(1) : spaces;
        }

        private int RoundBits(int amount)
        {
            if (amount >= 10000)
                return 10000;
            if (amount >= 5000)
                return 5000;
            if (amount >= 1000)
                return 1000;
            if (amount >= 100)
                return 100;
            return 1;
        }
    }
}