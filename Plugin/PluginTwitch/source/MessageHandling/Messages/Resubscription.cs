using System;
using System.Collections.Generic;
using System.Linq;

namespace PluginTwitchChat
{
    public class Resubscription : IMessage
    {
        private readonly string Message;
        private readonly Tags Tags;

        public Resubscription(string message, string tags)
        {
            Message = message;
            Tags = new Tags(tags);
        }

        public List<Line> GetLines(MessageFormatter messageFormatter)
        {
            // the resubscription message, eg: Timsan90 has resubscribed for 6 months!
            var resubWords = Tags["system-msg"].Split(new string[] { "\\s" }, StringSplitOptions.None).Select(s => new Word(s)).ToList();
            var lines = new List<Line>();

            lines.Add(messageFormatter.Seperator);
            messageFormatter.WordWrap(resubWords, lines);
            if (Message != null)
            {
                // Message can be null in resub messages
                var msgWords = messageFormatter.GetWords(Tags.DisplayName, Message, Tags);
                messageFormatter.WordWrap(msgWords, lines);
            }
            lines.Add(messageFormatter.Seperator);
            return lines;
        }
    }
}
