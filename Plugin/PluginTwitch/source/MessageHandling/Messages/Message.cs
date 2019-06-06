using System.Collections.Generic;

namespace PluginTwitchChat
{
    public interface IMessage
    {
        List<Line> GetLines(MessageFormatter messageFormatter);
    }
}
