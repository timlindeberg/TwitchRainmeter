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

        public String String { get; private set; }
        public String Channel { get; private set; }
        public int ImageWidth { get { return messageParser.ImageWidth; } }
        public int ImageHeight { get { return messageParser.ImageHeight; } }
        public bool IsInChannel { get; private set; }

        private const string Server = "irc.twitch.tv";

        private TwitchIrcClient client;
        private TwitchIrcClient senderClient;
        private MessageParser messageParser;
        private ImageDownloader imgDownloader;
        private String username;
        private String ouath;
        private bool isConnected;

        public TwitchClient(string username, string ouath, MessageParser messageParser, ImageDownloader imgDownloader)
        {
            isConnected = false;
            client = new TwitchIrcClient();
            senderClient = new TwitchIrcClient();
            Channel = "";
            this.username = username;
            this.ouath = ouath;
            this.messageParser = messageParser;
            this.imgDownloader = imgDownloader;
        }

        public void Connect()
        {
            if (isConnected)
                return;

            String = string.Format("Starting to connect to twitch as {0}.", username);

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
                        String = "Connection to Twitch timed out.";
                        return;
                    }
                }
                if (!registeredEvent.Wait(waitTime))
                {
                    String = "Could not register to Twitch. Did provide a user name and Ouath?";
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
            imgDownloader.DownloadBadges(newChannel);
        }

        public void LeaveChannel()
        {
            if (!isConnected || Channel == "")
                return;

            client.Channels.Leave(Channel);
            messageParser.Reset();
            Channel = "";
            String = "";
        }

        public void Disconnect()
        {
            if (!isConnected)
                return;

            client.Disconnect();
            senderClient.Disconnect();
            isConnected = false;
        }

        public Image GetImage(int index)
        {
            var images = messageParser.Images;
            if (index < 0 || index >= images.Count)
                return null;

            return images[index];
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
            String = string.Format("Joined the channel {0}.", e.Channel.Name);
            IsInChannel = true;
        }

        private void LeftChannel(object sender, IrcChannelEventArgs e)
        {
            IsInChannel = false;
        }

        private void ChannelMessageReceived(object sender, IrcMessageEventArgs e)
        {
            lock (String)
                String = messageParser.AddMessage(e.Source.Name, e.Text, e.Tags);
        }

        private void ChannelNoticeReceived(object sender, IrcMessageEventArgs e)
        {
            lock (String)
                String = messageParser.AddNotice(e.Text);
        }

    }
}
