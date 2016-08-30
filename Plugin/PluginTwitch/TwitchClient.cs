using System;
using System.Linq;
using System.Text;
using IrcDotNet;
using System.Threading;
using System.Diagnostics;

using System.Collections.Generic;
using System.IO;
using System.Net;

namespace PluginTwitch
{
    public class TwitchClient
    {


        public String String { get; private set;  }
        public String Channel { get; private set; }
        public int EmoteWidth { get { return messageParser.EmoteWidth; } }
        public int EmoteHeight { get { return messageParser.EmoteHeight; } }

        private TwitchIrcClient client;
        private MessageParser messageParser;
        private String username;
        private String ouath;
       
        private bool isConnected;

        public TwitchClient(string username, string ouath, MessageParser messageParser)
        {
            isConnected = false;
            client = new TwitchIrcClient();
            Channel = "";
            this.username = username;
            this.ouath = ouath;
            this.messageParser = messageParser;
        }

        public void Connect()
        {
            if (isConnected)
                return;

            String = string.Format("Starting to connect to twitch as {0}.", username);
            var server = "irc.twitch.tv";
            client.FloodPreventer = new IrcStandardFloodPreventer(4, 2000);
            client.Registered += IrcClient_Registered;
            // Wait until connection has succeeded or timed out.
            using (var registeredEvent = new ManualResetEventSlim(false))
            {
                using (var connectedEvent = new ManualResetEventSlim(false))
                {
                    client.Connected += (sender2, e2) => connectedEvent.Set();
                    client.Registered += (sender2, e2) => registeredEvent.Set();
                    client.Connect(server, false,
                        new IrcUserRegistrationInfo()
                        {
                            NickName = username,
                            Password = ouath,
                            UserName = username
                        });
                    if (!connectedEvent.Wait(10000))
                    {
                        String = string.Format("Connection to '{0}' timed out.", server);
                        return;
                    }
                }
                String = string.Format("Now connected to '{0}'.", server);
                if (!registeredEvent.Wait(10000))
                {
                    String = string.Format("Could not register to '{0}'.", server);
                    return;
                }
                isConnected = true;
                client.SendRawMessage("CAP REQ :twitch.tv/tags");
            }
        }

        public bool IsInChannel()
        {
            return Channel != "";
        }

        public void JoinChannel(string channel)
        {
            if (!isConnected)
                return;

            messageParser.Reset();
            String = "";
            Channel = channel;
            foreach (var c in client.Channels)
                client.Channels.Leave(c.Name);
            client.Channels.Join(Channel);
        }

        public void LeaveChannel()
        {
            if (!isConnected)
                return;

            messageParser.Reset();
            String = "";
            foreach(var c in client.Channels)
                client.Channels.Leave(c.Name);
            Channel = "";
        }

        public void Disconnect()
        {
            if (!isConnected)
                return;

            client.Disconnect();
            isConnected = false;
        }

        public Emote GetEmote(int index)
        {
            var emotes = messageParser.Emotes;
            if (index >= emotes.Count)
                return null;

            return emotes[index];
        }

        public void SendMessage(string msg)
        {
            client.SendPrivateMessage(new String[] { Channel }, msg);
        }

        private void IrcClient_Registered(object sender, EventArgs e)
        {
            var client = (IrcClient)sender;

            client.LocalUser.JoinedChannel += IrcClient_LocalUser_JoinedChannel;
        }

        private void IrcClient_LocalUser_JoinedChannel(object sender, IrcChannelEventArgs e)
        {
            var localUser = (IrcLocalUser)sender;

            e.Channel.MessageReceived += IrcClient_Channel_MessageReceived;
            String = string.Format("Joined the channel {0}.", e.Channel.Name);
        }

        private void IrcClient_Channel_MessageReceived(object sender, IrcMessageEventArgs e)
        {
            var channel = (IrcChannel)sender;
            lock (String)
            {
               String = messageParser.AddMessage(e.Source.Name, e.Text, e.Tags);
            }
        }

    }
}
