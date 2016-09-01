using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PluginTwitch
{
    public struct EmoteInfo
    {
        public string ID;
        public int Start;
        public int End;
        public int Length { get { return End - Start; } }

        public EmoteInfo(string id, int start, int end)
        {
            this.ID = id;
            this.Start = start;
            this.End = end;
        }
    }
}
