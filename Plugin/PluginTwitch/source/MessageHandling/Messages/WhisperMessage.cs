using System.Collections.Generic;

namespace PluginTwitchChat
{
    public class WhisperMessage : IMessage
    {
        private static readonly string WhisperPrefix = "|Whisper|";
        private readonly string message;
        private readonly string sender;
        private readonly Tags tags;

        public WhisperMessage(string sender, string message, string tags)
        {
            this.message = message;
            this.sender = sender;
            this.tags = new Tags(tags);
        }

        public List<Line> GetLines(MessageFormatter messageFormatter)
        {
            var user = tags.DisplayName ?? sender;
            var words = messageFormatter.GetWords(user, message, tags);
            words.Insert(0, new Word(WhisperPrefix));
            return messageFormatter.WordWrap(words);
        }
    }
}
