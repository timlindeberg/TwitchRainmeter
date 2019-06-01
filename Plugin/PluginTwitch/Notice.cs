using System.Collections.Generic;

namespace PluginTwitchChat
{
    public class Notice : Message
    {
        private string Message;

        public Notice(string message)
        {
            this.Message = message;
        }

        public void AddLines(MessageHandler msgHandler)
        {
            var words = msgHandler.GetWords(Message);
            var lines = new List<Line>();
                msgHandler.AddSeperator(lines);
            msgHandler.WordWrap(words, lines);
            msgHandler.AddSeperator(lines);
            msgHandler.AddLines(lines);
        }
    }
}
