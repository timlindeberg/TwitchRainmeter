using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PluginTwitchChat
{
    public class PrivMessage : Message
    {
        private string message;
        private string sender;
        private Tags tags;

        public PrivMessage(string sender, string message, string tags)
        {
            this.message = message;
            this.sender = sender;
            this.tags = new Tags(tags);
        }

        public void AddLines(MessageHandler msgHandler)
        {
            var user = tags.DisplayName ?? sender;
            var words = msgHandler.GetWords(user, message, tags);
            var lines = msgHandler.WordWrap(words);
            msgHandler.AddLines(lines);
        }
    }
}
