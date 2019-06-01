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

        public void AddLines(MessageHandler msgHandler)
        {
            var user = tags.DisplayName ?? sender;
            var words = msgHandler.GetWords(user, message, tags);
            var lines = msgHandler.WordWrap(words);
            msgHandler.AddLines(lines);
        }
    }
}
