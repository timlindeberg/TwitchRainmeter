using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing.Imaging;

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

        public AnimatedImage(string name, string displayName, string imageString, string gifPath) : base(name, displayName, imageString)
        {
            currentTime = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
            durations = new List<int>();
            using (var gif = System.Drawing.Image.FromFile(gifPath))
            {
                int frameCount = gif.GetFrameCount(FrameDimension.Time);
                byte[] times = gif.GetPropertyItem(0x5100).Value;
                for (int frame = 0; frame < frameCount; frame++)
                {
                    int duration = BitConverter.ToInt32(times, 4 * frame) * 10; // to ms
                    durations.Add(duration);
                }
            }
        }

        private void AdvanceFrameCounter()
        {
            long time = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
            while (currentTime < time && frameIndex < durations.Count - 1)
                currentTime += durations[frameIndex++];

            if (frameIndex == durations.Count - 1)
                finished = true;
        }
    }
}
