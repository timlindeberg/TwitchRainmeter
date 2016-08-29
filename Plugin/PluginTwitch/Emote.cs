using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PluginTwitch
{
    public class Emote
    {
        public int ID;
        public int X;
        public int Y;
        public double height;
        public int start;
        public int end;

        public Emote(int id, int start, int end)
        {
            this.ID = id;
            this.X = 0;
            this.Y = 0;
            this.height = 0;
            this.start = start;
            this.end = end;
        }

        public override string ToString()
        {
            return string.Format("ID: {0}, start: {1}, end: {2}", ID, start, end);
        }

    }
}
