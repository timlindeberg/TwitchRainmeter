# Twitch Chat for Rainmeter

Twitch Chat for Rainmeter is a plugin for Rainmeter used to view the Twitch chat
right on your desktop with full emote and messaging support!

![Twitch for Rainmeter in action](http://i.imgur.com/dk7S4Pm.png)

![Twitch for Rainmeter in action](http://i.imgur.com/jsdgrxB.png)

Please let me know of any issues or missing features you would like added!

**Features**:
* Fully customizable look including width, height, colors etc.
* Emotes and badges.
* Automatically connects to the correct channel when viewed in Chrome.
* Messages can be posted directly from the Rainmeter skin.
* Highlights your own name in chat.
* Hover over images to see their names.

**Missing features**:
* Does not support BetterTTV emotes.
* Automatic connection only works in the Chrome browser.
* Some notifications are missing such as the message recieved when you type too fast 
when a channel is in slow mode.
* Resubscriptions will not be shown in the chat.
* Other such messages might be missing.

**Known issues**:
* Emotes will displayed at the wrong location when Unicode symbols of different
sizes are posted in chat.
* I've seen the application crash when weird characters (see https://twitter.com/glitchr_)
are posted in chat. Hopefully this issue is resolved.

## Installation

1. Install the skin using the .rmskin package.
2. Install the font you want to use for the skin by placing them in the Windows Font folder.
3. Generate an Ouath code at http://www.twitchapps.com/tmi/.
4. Enter your username and the Ouath code in the @Resources\Variables.inc file.
5. Customize the settings in the Variables.inc file to your liking and make sure to specify the font you installed.
6. You're all set!

## Usage

To join a channel enter the name of the channel (www.twitch.tv/<channel>) in the channel input bar.
Alternatively you can used the automatic connection feature which is enabled by default (currently only works with Chrome).
Just navigate to the Twitch channel you want to view and the Skin will automatically connect to the channel.
Manually entering a channel will override the automatic connection feature until the skin is reset.

Once you've joined the channel you can enter messages in the input field below the chat.

## Credits

Thanks to Malody Hoe, the author of the skin Monstercat Visualizer, on which the meter generation code is based.

Thanks to the authors of the IrcDotNet library which is used to connect to the chat.

## License

Creative Commons Attribution-Non-Commercial-Share Alike 3.0