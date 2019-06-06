using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace PluginTwitchChat
{
    public class TwitchChat
    {
        private class ChatData
        {
            public List<Image> Images = new List<Image>();
            public List<AnimatedImage> Gifs = new List<AnimatedImage>();
            public List<Link> Links = new List<Link>();
            public string Content = "";
        }

        private readonly StringMeasurer measurer;
        private readonly Settings settings;
        private readonly MessageFormatter messageFormatter;

        private Line lastLine;
        private Queue<Line> lineQueue;

        private ChatData currentData;
        private ChatData nextData;

        public TwitchChat(Settings settings, StringMeasurer measurer, MessageFormatter messageFormatter)
        {
            this.measurer = measurer;
            this.settings = settings;
            this.messageFormatter = messageFormatter;

            Reset();
        }

        public void Update()
        {
            currentData = nextData;
        }

        public Image GetImage(int index)
        {
            return currentData.Images.ElementAtOrDefault(index);
        }

        public AnimatedImage GetGif(int index)
        {
            return currentData.Gifs.ElementAtOrDefault(index);
        }

        public Link GetLink(int index)
        {
            return currentData.Links.ElementAtOrDefault(index);
        }

        public string GetContent()
        {
            return currentData.Content;
        }

        public void SetContent(string content)
        {
            currentData.Content = nextData.Content = content;
        }

        public void Reset()
        {
            lineQueue = new Queue<Line>();
            currentData = nextData = new ChatData();
        }

        public void AddMessage(IMessage msg)
        {
            AddLines(msg);
            ResizeLineQueue();
            nextData = GetNewChatData();
        }

        public void AddLines(IMessage msg)
        {
            foreach (var line in msg.GetLines(messageFormatter))
            {
                if (lastLine != messageFormatter.Seperator || line != messageFormatter.Seperator)
                {
                    lineQueue.Enqueue(line);
                    lastLine = line;
                }
            }
        }

        // Resize the line queue to fit the maximum height
        private void ResizeLineQueue()
        {
            var sb = new StringBuilder();
            foreach (var line in lineQueue.ToList())
            {
                sb.AppendLine(line.Text);
                var height = measurer.GetHeight(sb);
                while (height > settings.Height && lineQueue.Count > 1) // Keep at least one line in the queue
                {
                    var firstLine = lineQueue.Dequeue();
                    sb.Remove(0, firstLine.Text.Length + Environment.NewLine.Length);
                    height = measurer.GetHeight(sb);
                }
            }
        }

        // Calculate Y positions and calculate new chat data
        private ChatData GetNewChatData()
        {
            var sb = new StringBuilder();
            var data = new ChatData();

            var currentHeight = 0.0;
            foreach (var line in lineQueue)
            {
                var prevHeight = currentHeight;
                sb.AppendLine(line.Text);
                currentHeight = measurer.GetHeight(sb);

                foreach (var pos in line.Positioned)
                {
                    var height = currentHeight - prevHeight;
                    pos.Y = prevHeight + height * (1 - settings.ImageScale) / 2;
                    if (pos is AnimatedImage)
                    {
                        data.Gifs.Add(pos as AnimatedImage);
                    }
                    else if (pos is Image)
                    {
                        data.Images.Add(pos as Image);
                    }
                    else if (pos is Link)
                    {
                        data.Links.Add(pos as Link);
                    }
                }
            }
            data.Content = sb.ToString();
            return data;
        }
    }
}
