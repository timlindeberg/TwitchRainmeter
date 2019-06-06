using System.Collections.Generic;

namespace PluginTwitchChat
{
    public class PrivMessage : IMessage
    {
        private readonly string message;
        private readonly string sender;
        private readonly Tags tags;

        public PrivMessage(string sender, string message, string tags)
        {
            this.message = message;
            this.sender = sender;
            this.tags = new Tags(tags);
        }

        public List<Line> GetLines(MessageFormatter messageFormatter)
        {
            var user = tags.DisplayName ?? sender;
            var words = messageFormatter.GetWords(user, message, tags);
            return messageFormatter.WordWrap(words);
            
        }
    }
}
