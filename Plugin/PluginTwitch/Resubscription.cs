using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PluginTwitchChat
{
    public class Resubscription : Message
    {
        private string Message;
        private Tags Tags;

        public Resubscription(string message, string tags)
        {
            this.Message = message;
            this.Tags = new Tags(tags);
        }

        public void AddLines(MessageHandler msgHandler)
        {
            // the resubscription message, eg: Timsan90 has resubscribed for 6 months!
            var resubWords = Tags["system-msg"].Split(new string[] { "\\s" }, StringSplitOptions.None).Select(s => new Word(s)).ToList();
            var lines = new List<Line>();

            msgHandler.AddSeperator(lines);
            msgHandler.WordWrap(resubWords, lines);
            if (Message != null)
            {
                // Message can be null in resub messages
                var msgWords = msgHandler.GetWords(Tags.DisplayName, Message, Tags);
                msgHandler.WordWrap(msgWords, lines);
            }
            msgHandler.AddSeperator(lines);
            msgHandler.AddLines(lines);
        }
    }
}
