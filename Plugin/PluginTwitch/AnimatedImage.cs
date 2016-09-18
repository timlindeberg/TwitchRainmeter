using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing.Imaging;
using Rainmeter;

namespace PluginTwitchChat
{
    public class AnimatedImage : Image
    {

        public override string Name
        {
            get
            {
                if(!finished)
                    AdvanceFrameCounter();
                return string.Format("{0}-{1}", _name, frameIndex);
            }
        }

        private List<int> durations;
        private int frameIndex;
        private long currentTime;
        private bool finished;
        private bool repeat;

        private static Dictionary<string, List<int>> durationCache = new Dictionary<string, List<int>>();

        public AnimatedImage(string name, string displayName, string imageString, string path, bool repeat) : base(name, displayName, imageString)
        {
            this.repeat = repeat;
            currentTime = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
            durations = GetDurations(path);
        }

        private List<int> GetDurations(string path)
        {
            if (durationCache.ContainsKey(path))
                return durationCache[path];

            var durations = new List<int>();
            using (var gif = System.Drawing.Image.FromFile(path))
            {
                int frameCount = gif.GetFrameCount(FrameDimension.Time);
                byte[] times = gif.GetPropertyItem(0x5100).Value;
                for (int frame = 0; frame < frameCount; frame++)
                {
                    int duration = BitConverter.ToInt32(times, 4 * frame) * 10; // to ms
                    durations.Add(duration);
                }
            }
            API.Log(API.LogType.Notice, "Durations: " + string.Join(", ", durations));
            durationCache[path] = durations;
            return durations;
        }

        private void AdvanceFrameCounter()
        {
            long time = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
            while (currentTime + durations[frameIndex] < time && frameIndex < durations.Count - 1)
                currentTime += durations[frameIndex++];

            if (frameIndex >= durations.Count - 1)
            {
                if (repeat)
                    frameIndex = 0;
                else
                   finished = true;
            }
        }
    }
}
