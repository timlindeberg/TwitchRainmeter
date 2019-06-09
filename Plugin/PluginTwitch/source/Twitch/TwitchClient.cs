using System;
using System.Text;
using IrcDotNet;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;

namespace PluginTwitchChat
{
    public class TwitchClient
    {
        public string Channel { get; private set; }
        public string ChannelStatus { get; private set; }
        public string Viewers { get; private set; }
        public int ViewerCount { get; private set; }
        public bool IsInChannel { get; private set; }

        private const string Server = "irc.twitch.tv";

        private readonly TwitchIrcClient client;
        private readonly TwitchIrcClient senderClient;
        private readonly TwitchChat twitchChat;
        private readonly TwitchDownloader twitchDownloader;
        private readonly Settings settings;

        private Task updateChannelInfoTask;
        private bool isConnected;
        private long lastChannelUpdate;

        public TwitchClient(Settings settings, TwitchChat twitchChat, TwitchDownloader twitchDownloader)
        {
            isConnected = false;
            client = new TwitchIrcClient();
            senderClient = new TwitchIrcClient();
            Channel = ChannelStatus = Viewers = "";

            this.twitchChat = twitchChat;
            this.twitchDownloader = twitchDownloader;
            this.settings = settings;

            lastChannelUpdate = 0;
        }

        public void Connect()
        {
            if (isConnected)
            {
                return;
            }

            client.FloodPreventer = new IrcStandardFloodPreventer(4, 2000);
            client.Registered += ClientRegistered;
            // Wait until connection has succeeded or timed out.
            var waitTime = 2000;
            var ircRegistrationInfo = new IrcUserRegistrationInfo() { NickName = settings.User, Password = settings.Ouath, UserName = settings.User };
            using (var registeredEvent = new ManualResetEventSlim(false))
            {
                using (var connectedEvent = new ManualResetEventSlim(false))
                {
                    client.Connected += (sender2, e2) => connectedEvent.Set();
                    client.Registered += (sender2, e2) => registeredEvent.Set();
                    client.Connect(Server, false, ircRegistrationInfo);
                    if (!connectedEvent.Wait(waitTime))
                    {
                        twitchChat.SetContent("Connection to Twitch timed out.");
                        return;
                    }
                }
                if (!registeredEvent.Wait(waitTime))
                {
                    twitchChat.SetContent("Could not connect to Twitch. Did provide a user name and Ouath?");
                    return;
                }
                isConnected = true;
                client.SendRawMessage("CAP REQ :twitch.tv/tags");
                client.SendRawMessage("CAP REQ :twitch.tv/commands");
                senderClient.Connect(Server, false, ircRegistrationInfo);
            }
        }

        public void JoinChannel(string newChannel)
        {
            if (newChannel == Channel)
            {
                return;
            }

            twitchDownloader.SetChannel(newChannel);
            Connect();
            LeaveChannel();
            client.Channels.Join(newChannel);
            ChannelStatus = twitchDownloader.GetChannelStatus(newChannel);
            Channel = newChannel;
        }

        public void LeaveChannel()
        {
            if (!isConnected || Channel == "")
            {
                return;
            }

            client.Channels.Leave(Channel);
            twitchChat.Reset();
            Channel = "";
        }

        public void Disconnect()
        {
            if (!isConnected)
            {
                return;
            }

            client.Disconnect();
            senderClient.Disconnect();
            isConnected = false;
        }

        public void Update()
        {
            if (!IsInChannel || settings.ChannelStatusUpdateTime == -1)
            {
                return;
            }

            var time = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
            if (time < lastChannelUpdate + settings.ChannelStatusUpdateTime)
            {
                return;
            }

            lastChannelUpdate = time;
            if (updateChannelInfoTask == null || updateChannelInfoTask.IsCompleted)
            {
                updateChannelInfoTask = Task.Run(() => ChannelStatus = twitchDownloader.GetChannelStatus(Channel));
            }
        }

        public void SendMessage(string msg)
        {
            // We use another client with another connection to send the message.
            // This way the other client recieves a message with emote positions etc.
            // when the message is sent.
            senderClient.SendPrivateMessage(new string[] { Channel }, msg);
        }

        private void ClientRegistered(object sender, EventArgs e)
        {
            var localUser = ((IrcClient)sender).LocalUser;

            localUser.JoinedChannel -= JoinedChannel;
            localUser.JoinedChannel += JoinedChannel;
            localUser.LeftChannel -= LeftChannel;
            localUser.LeftChannel += LeftChannel;
            localUser.MessageReceived -= MessageRecieved;
            localUser.MessageReceived += MessageRecieved;
        }

        private void JoinedChannel(object sender, IrcChannelEventArgs e)
        {
            var channel = e.Channel;

            channel.MessageReceived -= ChannelMessageReceived;
            channel.MessageReceived += ChannelMessageReceived;
            channel.NoticeReceived -= ChannelNoticeReceived;
            channel.NoticeReceived += ChannelNoticeReceived;
            channel.UserNoticeReceived -= UserNoticeMessageRecieved;
            channel.UserNoticeReceived += UserNoticeMessageRecieved;
            IsInChannel = true;
        }

        private void LeftChannel(object sender, IrcChannelEventArgs e)
        {
            IsInChannel = false;
        }

        private void MessageRecieved(object o, IrcMessageEventArgs args)
        {
            AddMessage(new WhisperMessage(args.Source.Name, args.Text, args.Tags));
        }

        private void UserNoticeMessageRecieved(object sender, IrcMessageEventArgs e)
        {
            AddMessage(new Resubscription(e.Text, e.Tags));
        }

        private void ChannelMessageReceived(object sender, IrcMessageEventArgs e)
        {
            if (e.Source.Name == "twitchnotify")
            {
                AddMessage(new Notice(e.Text));
            }
            else
            {
                AddMessage(new PrivMessage(e.Source.Name, e.Text, e.Tags));
            }
        }

        private void ChannelNoticeReceived(object sender, IrcMessageEventArgs e)
        {
            AddMessage(new Notice(e.Text));
        }

        private void AddMessage(IMessage message)
        {
            if(Channel != "")
            {
                twitchChat.AddMessage(message);
            }
        }

    }
}
