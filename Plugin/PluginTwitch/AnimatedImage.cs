using System;
using System.Collections.Generic;
using System.IO;
using System.Drawing.Imaging;

namespace PluginTwitchChat
{
    public class AnimatedImage : Image
    {
        public override string Name
        {
            get
            {
                if (!finished)
                {
                    AdvanceFrameCounter();
                }
                return string.Format("{0}-{1}", _name, frameIndex);
            }
        }

        private List<int> durations;
        private int frameIndex;
        private long currentTime;
        private bool finished;
        private bool repeat;
        private string path;

        private static Dictionary<string, List<int>> durationCache = new Dictionary<string, List<int>>();

        public AnimatedImage(string name, string displayName, string imageString, string path, bool repeat) : base(name, displayName, imageString)
        {
            this.repeat = repeat;
            this.path = path;
            currentTime = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
            SetDurations();
        }

        private void SetDurations()
        {
            if (durations != null)
            {
                return;
            }
            if (durationCache.ContainsKey(path))
            {
                durations = durationCache[path];
                return;
            }

            try
            {
                durations = ReadDurations();
            }
            catch (System.IO.IOException)
            {
                return;
            }
            durationCache[path] = durations;
        }

        private List<int> ReadDurations()
        {
            var durations = new List<int>();

            using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read))
            {
                try
                {
                    using (var gif = System.Drawing.Image.FromStream(fs))
                    {
                        var frameCount = gif.GetFrameCount(FrameDimension.Time);
                        var times = gif.GetPropertyItem(0x5100).Value;
                        for (var frame = 0; frame < frameCount; frame++)
                        {
                            var duration = BitConverter.ToInt32(times, 4 * frame) * 10; // to ms
                            durations.Add(duration);
                        }
                    }
                }
                catch (System.ArgumentException)
                {
                    return null;
                }
            }

            return durations;
        }

        private void AdvanceFrameCounter()
        {
            SetDurations();
            if (durations == null)
            {
                return;
            }

            var time = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
            while (currentTime + durations[frameIndex] < time && frameIndex < durations.Count - 1)
            {
                currentTime += durations[frameIndex++];
            }

            if (frameIndex >= durations.Count - 1)
            {
                if (repeat)
                {
                    frameIndex = 0;
                }
                else
                {
                    finished = true;
                }
            }
        }
    }
}
