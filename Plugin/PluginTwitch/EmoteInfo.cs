namespace PluginTwitchChat
{
    public struct EmoteInfo
    {
        public string ID;
        public int Start;
        public int End;
        public int Length { get { return End - Start; } }

        public EmoteInfo(string id, int start, int end)
        {
            ID = id;
            Start = start;
            End = end;
        }
    }
}
