using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PluginTwitch
{
    public class Word
    {

        public string String { get; private set; }

        public Word(string s)
        {
            this.String = s;
        }

        public override string ToString()
        {
            return String;
        }
    }
}
