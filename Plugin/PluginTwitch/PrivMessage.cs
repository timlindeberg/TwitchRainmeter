using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PluginTwitchChat
{
    public class PrivMessage : Message
    {
        private string User;
        private string Message;
        private string Tags;

        public PrivMessage(string user, string message, string tags)
        {
            this.User = user;
            this.Message = message;
            this.Tags = tags;
        }

        public void AddLines(MessageHandler msgHandler)
        {
            var tagMap = new Tags(Tags);
            var words = msgHandler.GetWords(User, Message, tagMap);
            var lines = msgHandler.WordWrap(words);
            msgHandler.AddLines(lines);
        }
    }
}
