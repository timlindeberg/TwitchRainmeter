using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PluginTwitchChat
{
    public interface Message
    {
        void AddLines(MessageHandler msgHandler);
    }
}
