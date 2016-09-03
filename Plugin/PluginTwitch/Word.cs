using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PluginTwitchChat
{
    public class Word
    {

        public string String { get; private set; }

        public Word(string s)
        {
            this.String = s;
        }

        public Word(string s, int start, int length)
        {
            this.String = s.Substring(start, length);
        }

        public override string ToString()
        {
            return String;
        }
    }
}
