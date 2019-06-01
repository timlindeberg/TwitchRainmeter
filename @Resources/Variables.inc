[Variables]
;=============================================================================;
;----------------------------- General settings ------------------------------;
;=============================================================================;

; The width of the skin including chat and input fields
Width=1020

; If set to 1 the plugin will automatically connect to the twitch channel that
; is being focused by the browser. Set to 0 to disable this feature. If a
; channel is input using the text field this feature will be disabled until the
; skin is reloaded.
; Enabling automatic connection will make Rainmeter start slower and cause some
; lag each time time the browser is opened. Will not work with multiple browser
; windows.
ConnectAutomatically=1

; The browser being used. Automatic connection currently only works with
; Chrome.
Browser=Chrome

; The location of your browser on the harddrive. If this is not set you won't
; be able to click links in the chat.
BrowserLocation=C:\Program Files (x86)\Google\Chrome\Application\chrome.exe

; The string to display over the channel input when you're not connected to a 
; channel. If you're using automatic connection you might want to set it to 
; nothing to completely hide the channel input when not connected to a channel.
; It will still be clickable.
DefaultChannelInputString=""

; Show a seperator to make notices and resubscriptions stand out more in chat.
; Example:
; ───────────────────────────────────────
; The chat is now in slow mode.
; ───────────────────────────────────────
; 1 to enable and 0 to disable.
UseSeperator=1

; Toggle whether BetterTTV emotes are enabled.
; 1 to enable and 0 to disable.
UseBetterTTVEmotes=1

; Toggle whether FrankerFacez emotes are enabled.
; 1 to enable and 0 to disable.
UseFrankerFacezEmotes=1

; The maximum number of image that can be shown. A higher number can cost in 
; performance. Adjust this value according to how many image can appear in your
; chat window. The "correct" value will depend on the width and height of the 
; chat window as well as your font and font size. Also how spammy the current 
; chat is ;)
NumImages=200

; The maximum number of gifs that can be shown. Same principle as NumImages.
; Currently only works for cheers. These have a higher update frequency than
; images to support animations and are therefore more resource intensive.
NumGifs=100

; The maximum number of links that are clickable in the chat window. Same 
; principle as NumImages. A link that wraps over two two lines will count as
; two links.
NumLinks=10

; Which quality image to download. Can be 1, 2 or 3 where 1 is the lowest
; quality and 3 is the highest. Larger text size might require higher image
; quality but for small text 1 should be enough.
ImageQuality=2

; The scale of the images. Between 0.0 and 1.0 where 1.0 means that they are as
; tall as a line. 
ImageScale=0.9

; A path to a directory where images will be downloaded. Must have read and 
; write access rights so don't put in e.g. Program Files where you need 
; administrators rights to access.
; NOTE: Don't remove the '_empty_X.png' images from this folder.
ImageDir=#@#images

; Determines how often channel information (viewers, viewer count, channel 
; status) is updated. The value is given in milliseconds (e.g. 10000, the 
; values are updated every 10 seconds). There can be quite a lot of data to
; to fetch so updating these values often will cause lag.
; Set to -1 to disable channel information (can increase performance).
; Setting it to 0 will fetch the data each time the skin is updated.
ChannelUpdateTime=10000

;=============================================================================;
;-------------------------- Channel input settings ---------------------------;
;=============================================================================;

; The font to use for the channel input field
ChannelFont=Nexa Bold

; The font size to use for the channel input field
ChannelFontSize=30

; The font color to use for the channel input field
ChannelFontColor=A7D2FB

; The height of the channel input field
ChannelHeight=55

; The background color used for the channel input field
; Having 1 alpha makes it so the background is still clickable.
ChannelBackgroundColor=00000001

; The color of the channel input field when it's active.
ChannelActiveBackgroundColor=2F2F2F

;=============================================================================;
;-------------------------- Message input settings ---------------------------;
;=============================================================================;

; The font to use for the message input field
InputFont=Nexa Bold

; The font size to use for the message input field
InputFontSize=20

; The font color to use for the message input field
InputFontColor=A7D2FB

; The height of the message input field
InputHeight=40

; The background color used for the message input field when not active.
InputBackgroundColor=00000050

; The background color used for the message input field when active.
InputActiveBackgroundColor=2F2F2F

; Padding between the chat window and the input field
InputPadding=5

; The width of the border around the input field. Set to 0 for no border.
InputBorderWidth=4

; The color of the border around the input field.
InputBorderColor=A7D2FBAA

; The corner radius of the border around the input field. A higher number means
; more rounding.
InputBorderCornerRadius=5

;=============================================================================;
;------------------------------- Chat settings -------------------------------;
;=============================================================================;

; The font used in the chat. THIS FONT MUST BE INSTALLED ON YOUR COMPUTER.
ChatFont=Nexa Bold

; The size of the chat font
ChatFontSize=14

; The color of regular messages in the chat.
ChatMainFontColor=FFFFFF

; The color of regular user names in the chat.
ChatUserFontColor=A7D2FB

; The color of your own user name in the chat.
ChatSelfFontColor=A7FBC3

; The color of links.
LinkColor=D0E8FE

; The height of the chat window.
ChatHeight=800

;=============================================================================;
;------------------------------ Viewers Settings -----------------------------;
;=============================================================================;

; The font used in the header of the viewers skin. 
ViewersHeaderFont=Nexa Bold

; The font used in the text of the viewers skin. 
ViewersTextFont=Nexa Bold

; The height of the header of the viewers skin
ViewersHeaderHeight=55

; The size of the font in the header of the viewers skin
ViewersHeaderFontSize=30

; The size of the font in the text of the viewers skin
ViewersTextFontSize=10

; The color of the the header of the viewers skin
ViewersHeaderFontColor=A7D2FB

; The color of the text of the viewers skin
ViewersTextFontColor=FFFFFF

; The height of the viewers window.
ViewersHeight=600

; The width of the viewers window.
ViewersWidth=300

; The maximum number of viewers that will be shown in [Viewers]. A too high
; number will cause significant lag in channels with many users.
MaxViewerNames=200