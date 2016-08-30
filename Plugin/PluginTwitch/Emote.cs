using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PluginTwitch
{
    public class Emote : Word
    {

        public int ID;
        public int X;
        public int Y;
        public int start;
        public int end;

        public Emote(string w, int id, int start, int end) : base(w)
        {
            this.ID = id;
            this.X = 0;
            this.Y = 0;
            this.start = start;
            this.end = end;
        }

        public override string ToString()
        {
            return string.Format("Emote({0})", ID);
        }

    }
}
