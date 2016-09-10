using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PluginTwitchChat
{
    public class PrivMessage : Message
    {
        private string Message;
        private Tags Tags;

        public PrivMessage(string message, string tags)
        {
            this.Message = message;
            this.Tags = new Tags(tags);
        }

        public void AddLines(MessageHandler msgHandler)
        {
            var words = msgHandler.GetWords(Message, Tags);
            var lines = msgHandler.WordWrap(words);
            msgHandler.AddLines(lines);
        }
    }
}
