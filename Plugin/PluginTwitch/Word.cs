namespace PluginTwitchChat
{
    public class Word
    {
        private readonly string String;

        public Word(string s)
        {
            String = s;
        }

        public Word(string s, int start, int length)
        {
            String = s.Substring(start, length);
        }

        public override string ToString()
        {
            return String;
        }

        public static implicit operator string(Word w)
        {
            return w.String;
        }
    }
}
