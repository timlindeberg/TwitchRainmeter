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

        public void AddLines(MessageHandler msgHandler)
        {
            var user = tags.DisplayName ?? sender;
            var words = msgHandler.GetWords(user, message, tags);
            words.Insert(0, new Word(WhisperPrefix));
            var lines = msgHandler.WordWrap(words);
            msgHandler.AddLines(lines);
        }
    }
}
