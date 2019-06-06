using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static PluginTwitchChat.StringMeasurer;

namespace PluginTwitchChat
{
    public class MessageFormatter
    {
        public Line Seperator { get; private set; }
        public Size ImageSize { get; private set; }

        private string imageString;


        private static readonly Regex URLRegex = new Regex(@"(https?:\/\/(?:www\.|(?!www))[^\s\.]+\.[^\s]{2,}|www\.[^\s]+\.[^\s]{2,})");
        private static readonly Regex CheerRegex = new Regex(@"(?:^|\s)cheer(\d+)(?:\s|$)");
        private static readonly string SeperatorSign = "─";

        private readonly TwitchDownloader downloader;
        private readonly StringMeasurer measurer;
        private readonly Settings settings;

        private readonly float spaceWidth;

        public MessageFormatter(Settings settings, StringMeasurer measurer, TwitchDownloader downloader)
        {
            this.downloader = downloader;
            this.measurer = measurer;
            this.settings = settings;

            imageString = CalculateImageString();
            var imageWidth = measurer.GetWidth(imageString);
            var imageHeight = measurer.GetHeight("A");
            if (settings.UseSeperator)
            {
                CalculateSeperator();
            }
            ImageSize = new Size
            {
                Width = Convert.ToInt32(settings.ImageScale * imageWidth),
                Height = Convert.ToInt32(settings.ImageScale * imageHeight)
            };
            spaceWidth = measurer.GetWidth(" ");
        }


        // Calculates the number of spaces that has the most similiar width and height.
        // Used in place of images within the string
        private string CalculateImageString()
        {
            var spaces = " ";
            var height = measurer.GetHeight("A");
            var width = measurer.GetWidth(spaces);
            var previousWidth = height;
            while (width < height)
            {
                spaces += " ";
                previousWidth = width;
                width = measurer.GetWidth(spaces);
            }
            return Math.Abs(previousWidth - height) <= Math.Abs(width - height) ? spaces.Substring(1) : spaces;
        }


        public List<Word> GetWords(string user, string msg, Tags tags)
        {
            var badges = new List<Image>();
            foreach (var badge in tags.Badges)
            {
                var displayName = downloader.GetDescription(badge);
                badges.Add(new Image(badge, displayName, imageString));
            }
            var emotes = tags.Emotes;
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
            var words = new List<Word>();
            for (int pos = 0; pos < msg.Length; pos++)
            {
                if (emotes != null && emoteIndex < emotes.Count)
                {
                    var emote = emotes[emoteIndex];
                    if (pos == emote.Start)
                    {
                        downloader.DownloadEmote(emote.ID);
                        var displayName = msg.Substring(emote.Start, emote.Length + 1);
                        var img = new Image(emote.ID, displayName, imageString);
                        words.Add(img);
                        pos = emote.End + 1;
                        lastWord = pos + 1;
                        emoteIndex++;
                        continue;
                    }
                }

                if (char.IsWhiteSpace(msg[pos]))
                {
                    AddWord(words, msg, ref bits, lastWord, pos - lastWord);
                    lastWord = pos + 1;
                }
            }
            if (lastWord < msg.Length)
            {
                AddWord(words, msg, ref bits, lastWord, msg.Length - lastWord);
            }

            return words;
        }

        public void AddWord(List<Word> words, string msg, ref int bits, int start, int len)
        {
            var s = msg.Substring(start, len);
            var urlMatch = URLRegex.Match(s);
            if (!urlMatch.Success)
            {
                AddWord(words, s, ref bits);
                return;
            }

            var index = urlMatch.Index;
            if (index == 0)
            {
                words.Add(new Link(s, measurer));
                return;
            }

            var s2 = s.Substring(0, index - 1);
            AddWord(words, s2, ref bits);
            words.Add(new Link(s, index, s.Length - index, measurer));
        }

        public void AddWord(List<Word> words, string word, ref int bits)
        {
            var cheerMatch = CheerRegex.Match(word);
            if (cheerMatch.Success)
            {
                var bitsInCheer = int.Parse(cheerMatch.Groups[1].Value);
                if (bits < bitsInCheer)
                {
                    words.Add(new Word(word));
                    return;
                }

                bits -= bitsInCheer;
                var roundedBits = RoundBits(bitsInCheer);

                var gifPath = downloader.DownloadCheer(roundedBits);
                var name = "cheer" + roundedBits;
                var displayName = "Cheer" + bitsInCheer;
                var imageString = this.imageString + " " + bitsInCheer;
                var animatedImg = new AnimatedImage(name, displayName, imageString, gifPath, repeat: true);
                words.Add(animatedImg);
                return;
            }

            var emote = downloader.GetNamedEmote(word);
            if (emote != null)
            {
                words.Add(GetEmoteImage(emote));
                return;
            }

            words.Add(new Word(word));
        }

        private Image GetEmoteImage(TwitchDownloader.NamedEmote namedEmote)
        {
            var name = namedEmote.Name;
            var description = namedEmote.Description;
            switch (namedEmote.FileEnding)
            {
                case TwitchDownloader.FileEnding.PNG: return new Image(name, description, imageString);
                case TwitchDownloader.FileEnding.GIF: return new AnimatedImage(name, description, imageString, namedEmote.Path, repeat: true);
                default: return null;
            }
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
                var isEmpty = line.Text == string.Empty;
                var word = words[i];

                var newString = isEmpty ? word : (line.Text + ' ' + word);
                var newLen = measurer.GetWidth(newString);

                SetPosition(word, isEmpty ? len : len + spaceWidth, newLen);

                if (newLen <= settings.Width)
                {
                    line.Add(word);
                    len = newLen;
                    continue;
                }

                // Word no longer fits in line.
                if (isEmpty || measurer.GetWidth(word) > settings.Width)
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
                len = 0.0f;
                i--; // revisit this word
            }

            if (!line.IsEmpty)
            {
                lines.Add(line);
            }

            return lines;
        }

        private void SetPosition(Word word, double currentLength, double newLen)
        {
            if (!(word is IPositioned))
            {
                return;
            }

            var pos = word as IPositioned;
            pos.X = currentLength;
            if (word is Image)
            {
                var width = newLen - currentLength;
                pos.X += width * (1 - settings.ImageScale) / 2;
            }
        }

        private Tuple<Word, Word> SplitWord(Word word, int start, string newString)
        {
            var breakPoint = FindBreakpoint(newString);
            var s1 = newString.Substring(start, breakPoint - start);
            var s2 = newString.Substring(breakPoint, newString.Length - breakPoint);
            if (word is Link)
            {
                var link = word as Link;
                var l1 = new Link(link.Url, s1, measurer) { X = link.X, Width = settings.Width - link.X };
                var l2 = new Link(link.Url, s2, measurer); // l2 will get positional information later.
                return new Tuple<Word, Word>(l1, l2);
            }

            return new Tuple<Word, Word>(new Word(s1), new Word(s2));
        }

        private int FindBreakpoint(string str)
        {
            var start = 1;
            var end = str.Length;
            while (start < end)
            {
                var mid = (end + start) / 2;
                var wordLength = measurer.GetWidth(str.Substring(0, mid));
                if (wordLength > settings.Width)
                {
                    end = mid;
                }
                else
                {
                    start = mid + 1;
                }
            }
            return start - 1;
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

        private void CalculateSeperator()
        {
            Seperator = new Line(measurer);
            var sb = new StringBuilder("");
            double width;
            do
            {
                sb.Append(SeperatorSign);
                width = measurer.GetWidth(sb);
            } while (width < settings.Width);
            sb.Remove(0, 1);
            Seperator.Add(sb.ToString());
        }
    }
}
