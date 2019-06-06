using System.Collections.Generic;

namespace PluginTwitchChat
{
    public class Notice : IMessage
    {
        private readonly string Message;

        public Notice(string message)
        {
            Message = message;
        }

        public List<Line> GetLines(MessageFormatter messageFormatter)
        {
            var words = messageFormatter.GetWords(Message);
            var lines = new List<Line>();
            lines.Add(messageFormatter.Seperator);
            messageFormatter.WordWrap(words, lines);
            lines.Add(messageFormatter.Seperator);
            return lines;
        }
    }
}
