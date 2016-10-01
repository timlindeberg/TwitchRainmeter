using System;
using System.Linq;
using System.Text;
using IrcDotNet;
using System.Threading;
using System.Diagnostics;
using System.Timers;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Rainmeter;

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
        private const int ChannelStatusInterval = 10000; // ms

        private readonly TwitchIrcClient client;
        private readonly TwitchIrcClient senderClient;
        private readonly MessageHandler messageHandler;
        private readonly TwitchDownloader twitchDownloader;

        private readonly string user;
        private readonly string ouath;
        private readonly int maxViewerNames;
        private readonly long updateTime;

        private Task updateChannelInfoTask;
        private bool isConnected;
        private long lastChannelUpdate;

        public TwitchClient(Settings settings, MessageHandler messageHandler, TwitchDownloader imgDownloader)
        {
            isConnected = false;
            client = new TwitchIrcClient();
            senderClient = new TwitchIrcClient();
            Channel = ChannelStatus = Viewers = "";
            this.user = settings.User;
            this.ouath = settings.Ouath;
            this.updateTime = settings.ChannelUpdateTime;
            this.maxViewerNames = settings.MaxViewerNames;

            this.messageHandler = messageHandler;
            this.twitchDownloader = imgDownloader;
            
            lastChannelUpdate = 0;
        }

        public void Connect()
        {
            if (isConnected)
                return;

            client.FloodPreventer = new IrcStandardFloodPreventer(4, 2000);
            client.Registered += ClientRegistered;
            // Wait until connection has succeeded or timed out.
            var waitTime = 2000;
            var ircRegistrationInfo = new IrcUserRegistrationInfo() { NickName = user, Password = ouath, UserName = user };
            using (var registeredEvent = new ManualResetEventSlim(false))
            {
                using (var connectedEvent = new ManualResetEventSlim(false))
                {
                    client.Connected += (sender2, e2) => connectedEvent.Set();
                    client.Registered += (sender2, e2) => registeredEvent.Set();
                    client.Connect(Server, false, ircRegistrationInfo);
                    if (!connectedEvent.Wait(waitTime))
                    {
                        messageHandler.String = "Connection to Twitch timed out.";
                        return;
                    }
                }
                if (!registeredEvent.Wait(waitTime))
                {
                    messageHandler.String = "Could not connect to Twitch. Did provide a user name and Ouath?";
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
                return;

            Connect();
            LeaveChannel();
            client.Channels.Join(newChannel);
            Channel = newChannel;
            twitchDownloader.SetChannel(newChannel);
        }

        public void LeaveChannel()
        {
            if (!isConnected || Channel == "")
                return;

            client.Channels.Leave(Channel);
            messageHandler.Reset();
            Channel = "";
        }

        public void Disconnect()
        {
            if (!isConnected)
                return;

            client.Disconnect();
            senderClient.Disconnect();
            isConnected = false;
        }

        public void Update()
        {
            if (!IsInChannel || updateTime == -1)
                return;

            var time = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
            if (time < lastChannelUpdate + updateTime)
                return;
            API.Log(API.LogType.Notice, "Updating channel info");

            lastChannelUpdate = time;
            if(updateChannelInfoTask == null || updateChannelInfoTask.IsCompleted)
                updateChannelInfoTask = UpdateChannelInfoAsync();
        }

        private Task UpdateChannelInfoAsync()
        {
            return Task.Run(() =>
            {
                ChannelStatus = twitchDownloader.GetChannelStatus(Channel);

                var viewers = twitchDownloader.GetViewers(Channel);
                ViewerCount = viewers.Count;
                var sb = new StringBuilder();
                for (int i = 0; i < maxViewerNames; i++)
                    sb.AppendLine(viewers[i]);
                Viewers = sb.ToString();
            });
        }

        public void SendMessage(string msg)
        {
            // We use another client with another connection to send the message.
            // This way the other client recieves a message with emote positions etc.
            // when the message is sent.
            senderClient.SendPrivateMessage(new String[] { Channel }, msg);
        }

        private void ClientRegistered(object sender, EventArgs e)
        {
            var client = (IrcClient)sender;

            client.LocalUser.JoinedChannel -= JoinedChannel;
            client.LocalUser.JoinedChannel += JoinedChannel;
            client.LocalUser.LeftChannel -= LeftChannel;
            client.LocalUser.LeftChannel += LeftChannel;
        }

        private void JoinedChannel(object sender, IrcChannelEventArgs e)
        {
            var localUser = (IrcLocalUser)sender;

            e.Channel.MessageReceived -= ChannelMessageReceived;
            e.Channel.MessageReceived += ChannelMessageReceived;
            e.Channel.NoticeReceived -= ChannelNoticeReceived;
            e.Channel.NoticeReceived += ChannelNoticeReceived;
            e.Channel.UserNoticeReceived -= UserNoticeMessageRecieved;
            e.Channel.UserNoticeReceived += UserNoticeMessageRecieved;
            IsInChannel = true;
        }

        private void LeftChannel(object sender, IrcChannelEventArgs e)
        {
            IsInChannel = false;
        }

        private void UserNoticeMessageRecieved(object sender, IrcMessageEventArgs e)
        {
            messageHandler.AddMessage(new Resubscription(e.Text, e.Tags));
        }

        private void ChannelMessageReceived(object sender, IrcMessageEventArgs e)
        {
            if (e.Source.Name == "twitchnotify")
                messageHandler.AddMessage(new Notice(e.Text));
            else
                messageHandler.AddMessage(new PrivMessage(e.Source.Name, e.Text, e.Tags));
        }

        private void ChannelNoticeReceived(object sender, IrcMessageEventArgs e)
        {
              messageHandler.AddMessage(new Notice(e.Text));
        }

    }
}
