using System;
using System.Linq;
using System.Text;
using IrcDotNet;
using System.Threading;
using System.Diagnostics;

using System.Collections.Generic;
using System.IO;
using System.Net;

namespace PluginTwitchChat
{
    public class TwitchClient
    {

        public String Channel { get; private set; }
        public bool IsInChannel { get; private set; }

        private const string Server = "irc.twitch.tv";

        private TwitchIrcClient client;
        private TwitchIrcClient senderClient;
        private MessageHandler messageHandler;
        private ImageDownloader imgDownloader;
        private String username;
        private String ouath;
        private bool isConnected;

        public TwitchClient(string username, string ouath, MessageHandler messageHandler, ImageDownloader imgDownloader)
        {
            isConnected = false;
            client = new TwitchIrcClient();
            senderClient = new TwitchIrcClient();
            Channel = "";
            this.username = username;
            this.ouath = ouath;
            this.messageHandler = messageHandler;
            this.imgDownloader = imgDownloader;
        }

        public void Connect()
        {
            if (isConnected)
                return;

            client.FloodPreventer = new IrcStandardFloodPreventer(4, 2000);
            client.Registered += ClientRegistered;
            // Wait until connection has succeeded or timed out.
            var waitTime = 2000;
            var ircRegistrationInfo = new IrcUserRegistrationInfo() { NickName = username, Password = ouath, UserName = username };
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
            imgDownloader.SetChannel(newChannel);
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
            messageHandler.Reset();
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
                messageHandler.AddMessage(new PrivMessage(e.Text, e.Tags));
        }

        private void ChannelNoticeReceived(object sender, IrcMessageEventArgs e)
        {
              messageHandler.AddMessage(new Notice(e.Text));
        }

    }
}
