# karayote
Karaoke queue manager allowing Discord and Telegram users to manage their selections from their phones using a bot, with a Windows-based administration interface.


## Requirements
* Windows 7+
* Karafun for Windows software with an active subscription
* Bot(s) set up with Discord and/or Telegram
* A Google API key for use with YouTube


## Legal note
If you are not hosting karaoke in a private place, you technically need to have the professional Karafun subscription to use Karafun songs and to be sure your venue has the correct legal arrangements with your country's recording industry to use copyrighted songs from YouTube.


## Setup
1. Install the Karafun player from https://www.karafun.com/apps/. Set up an account and buy a subscription. Log into this account in the Karafun software. This enables the websocket server within the software (Windows version only)

2. Create bots with Discord and/or Telegram according to their bot creation procedures. For Discord, make sure it can have admin rights and register slash commands. Make note of the bot keys but keep them safe.

3. Set up any channels you may need. This program will output to a "Log" channel and a "Status" channel if they are specified. The log channel is an admin channel for you to have a record of things as they happen. The status channel is meant to be for your users to join to have a constantly updated view of the queue.

In Telegram, technically we don't use channels we use supergroups. This is because how channels work in Telegram is too different from how they work in Discord, and I haven't worked around that yet. When you create a group to function as your log or status channel, make sure to upgrade it to a supergroup! Failing to do so will mean it'll probably automatically transition to a supergroup at some point while you're using it. When this happens the bot will lose permissions and everything will fail spectatularly.

In Discord, make note of the channel IDs by enabling Developer Mode, right clicking the channel and selecting Show ID. In Telegram, use the web version, select the group, and note the ID in the URL. For supergroups, add "100" to the beginning of the number shown (after the negative sign if any). 

In either case, I recommend setting permissions to prevent users from typing in the status channel.

4. Set up a Google API project and API key: go to https://console.cloud.google.com/ and either click "select a project" then "create a project" if you've never made one before, or click your currently selected project and then "create a project" otherwise. Make note of the name exactly.

Go to API & Services > Enabled APIs and Services and click "Enable APIs and Services". Find "YouTube Data API v3" and enable it.

Then go to API & Services > Credentials and click Create Credentials, then API Key. If you want, click the name of the key afterward to edit the name and to restrict it to the YouTube API. Safely note the key string.

5. Now you can set up the appsettings.json and botsettings.json. appsettings.json is included in the download, and the only thing to change would be if you had to set up your Karafun websocket server on a different address from the default. 

Create botsettings.json from this template:
```
{
    "Discord": {
        "DiscordBotToken": "",
        "DiscordLogChannel": 0,
        "DiscordStatusChannel": 0,
        "DiscordStatusChannelInvite": "",
        "DiscordAdminAllowlist": [
            ""
        ]
    },
    "Telegram": {
        "TelegramBotToken": "",
        "TelegramLogChannel": 0,
        "TelegramStatusChannel": 0,
        "TelegramStatusChannelInvite": "",
        "TelegramAdminAllowlist": [
            ""
        ]
    },
    "Youtube": {
        "YoutubeAPIKey": "",
        "GoogleAPIAppName":  ""
    }        
}

```
Copy in any tokens, keys, channel IDs, and the GoogleAPIAppName. Add your discord and/or telegram username to the admin lists, and include invite links to the status channels if you want. 

6. It's now ready to go!


## How to use
1. Make sure Karafun is running with an active subscription

2. Start the program (note, you can get logs by starting it from a command line with `> filename.log`). At this point the bot responds to basic commands like /help

3. DM the bot in Discord or Telegram and use the /openqueue command. This allows users to queue up songs by talking to the bot. You will see users' songs added to the admin interface and can rearrange the songs that aren't the first two if needed. Rearranging the current and next up songs isn't allowed because while singing is happening users in those positions will have received DMs confirming their positions.

You will also see activity in the Status channel and/or Log channel, if those were set up.

4. At this point you can use the /searchforuser and /youtubeforuser bot commands to add songs for people who aren't using the bot or don't have their phones on them. With this method you will still be able to load the Karafun or Youtube song with a button click in the UI. 

Alternatively, you can add a placeholder song in the UI itself using the fields on the right side of the window. But this just adds the text to the queue to hold that spot, it does not yet tie in to any Karafun or Youtube song that can be loaded. It would need to be loaded manually when that song's turn comes up

5. When it's time for people to start singing, use the /startsession bot command. Messenger bot users will get DMs informing them when they are next up and when it's their turn.

6. Use the Load button to load the current song into Karafun or the browser, then hit play in Karafun or on Youtube. 

Youtube videos first try to load as embeds, which can be set to not autoplay. Most karaoke videos don't work in embed mode though and you have to click through to the actual page for that video. For me that allows more control than having the video autoplay though, which is why I've kept this quirky behavior for now.

7. If you need to remove a song, you can select it in the queue and click delete, or click the remove button underneath the next up song. Note that if that user has any songs in reserve, their first reserved song will be slot into that position! This is to err on the side of people not losing their spot in line. To completely remove them, keep deleting until their reserved songs are exhausted. Right now it's only set up to allow two reserved songs.

There is no similar removal functionality for the current song, just "Done" followed by selecting whether they sang or not. If they have a reserved song, they will automatically be added to the back of the queue.

Indicating that a song was sung will keep it locked so others can't sing the same song in the same session. If they did not sing it, then it will become available for other users to select again.

8. When you are no longer accepting song submissions, use /closequeue in the bot to stop allowing new songs in the queue

9. When the last person has sung, use /endsession in the bot to close off the night with a message in the status channel


## Known Issues
There was a hard deadline for when I would need to use this program, and now development time has run out for a while. So some simple issues still remain.

* Adding a placeholder song in the UI requires you click into another text field before the button will enable.

* The situation where someone's messenger username is the same as a username added manually by an admin has not been tested. The program may act unpredictably.

* The /closequeue command has not been tested.

* When using Telegram, if using a normal group for the status or log channel, if something happens to convert it to a supergroup the bot will lose permissions and eventually the program will crash. There's no way to automatically reassign the bot's permissions, so it's important to set up the group as a supergroup to begin with to avoid this problem. (But eventually I'll make it handle this issue more gracefully)

* If the program crashes, there is no automatic recovery yet. A database is in the works but not ready. It's recommended you set up a log channel (see Setup above) to have a record of all the queue additions in chronological order in case of emergency (note: users' reserved songs that didn't hit the queue yet are not preserved). When I used it live on three very busy nights, the only source of crashes was the supergroup issue above though.

* If you're using both Discord and Telegram, sneaky people can have two accounts running and be in the queue twice. For now I recommend only using one or the other. Eventually there will be a way to tie accounts on different services into one user for the purposes of this program.
