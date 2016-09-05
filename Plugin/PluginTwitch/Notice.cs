using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
            var words = msgHandler.GetWords(Message, null);
            var lines = new List<Line>();
            msgHandler.AddSeperator(lines);
            msgHandler.WordWrap(words, lines);
            msgHandler.AddSeperator(lines);
            msgHandler.AddLines(lines);
        }
    }
}
