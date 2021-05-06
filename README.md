<!-- PROJECT LOGO -->
<br />
<p align="center">
  <a href="https://github.com/MuseMusicBot/MuseBot">
    <img src="https://i.imgur.com/mnmuuvZ.png" alt="Logo" width="160" height="160">
  </a>

  <h3 align="center">:headphones: Muse Bot</h3>

  <p align="center">
  A self hosted alternative of Hydra written in C#!
	<br>
	<a href="https://github.com/MuseMusicBot/MuseBot/releases">
		<img src="https://img.shields.io/github/downloads/MuseMusicBot/MuseBot/total?style=for-the-badge" />
	</a>
	<a href="https://github.com/MuseMusicBot/MuseBot/issues">
		<img src="https://img.shields.io/github/issues/MuseMusicBot/MuseBot?style=for-the-badge" />
	</a>
	<a href="https://github.com/MuseMusicBot/MuseBot/network/members">
		<img src="https://img.shields.io/github/forks/MuseMusicBot/MuseBot?style=for-the-badge" />
	</a>
	<a href="https://github.com/MuseMusicBot/MuseBot/stargazers">
	    <img src="https://img.shields.io/github/stars/MuseMusicBot/MuseBot?style=for-the-badge" />
	</a>
</p>



<!-- TABLE OF CONTENTS -->
<details open="open">
  <summary>Table of Contents</summary>
  <ol>
    <li>
      <a href="#about-the-project">About The Project</a>
      <ul>
        <li><a href="#built-with">Built With</a></li>
      </ul>
    </li>
    <li>
      <a href="#getting-started">Getting Started</a>
      <ul>
        <li><a href="#lavalink-prerequisites">Lavalink Prerequisites</a></li>
        <li><a href="#spotify-api-prerequisites">Spotify API Prerequisites</a></li>
        <li><a href="#discord-token-prerequisites">Discord Token Prerequisites</a></li>
        <li><a href="#bot-installation">Bot Installation</a></li>
      </ul>
    </li>
    <li><a href="#usage">Usage</a></li>
    <li><a href="#contributing">Contributing</a></li>
    <li><a href="#acknowledgements">Acknowledgements</a></li>
  </ol>
</details>



<!-- ABOUT THE PROJECT -->
## About The Project

<img src="https://i.imgur.com/CGhAndo.png" alt="Lavalink" height="900">

We have created a self hosted alternative for Hydra music bot.

Features:
* Supports YouTube, Spotify, SoundCloud, Bandcamp, Twitch, and Vimeo links
* Music requester channel
* Volume command
* Equalizer

This was mostly a hobby project so please do not expect any feature requests. We will try and fix bugs if they are major enough, but we recommend just issuing a pull request or use this bot as a template to make your own.

Also, this project was heavily inspired by Hydra so huge thanks to them.

### Built With

Thanks to the following libraries that we've used in our project to get this bot working.
* [Discord.Net](https://github.com/discord-net/Discord.Net)
* [Victoria](https://github.com/Yucked/Victoria)
* [SpotifyAPI-NET](https://github.com/JohnnyCrazy/SpotifyAPI-NET)



<!-- GETTING STARTED -->
## Getting Started

The easiest way to run this bot is on Windows as we have built an exe file for you to run. If you know nothing about building this project or do not want to, please use that.
<br><br>[Lavalink](https://github.com/freyacodes/Lavalink) is **required** in order to run this bot! (Doesn't matter if you run the exe or build the project)
<br><br>We will explain it as simplified as possible for those without any knowledge. If you know what you are doing, just run Lavalink and run this bot, make sure you have an *application.yml* for Lavalink. You will need a Spotify Credentials. [Get it here](https://developer.spotify.com/dashboard/).

### Lavalink Prerequisites

This guide assumes you are running Windows.
As mentioned [Lavalink](https://github.com/freyacodes/Lavalink) is required to run this bot.
* Download the latest jar from the Lavalink page [here](https://github.com/freyacodes/Lavalink/releases)
* Extract the jar to a folder somewhere. In the same folder where your Lavalink.jar is, make a file called *application.yml* and make sure it looks exactly like [this](https://github.com/freyacodes/Lavalink/blob/master/LavalinkServer/application.yml.example)
* Make sure you have Java installed. You can get it from [here](https://github.com/AdoptOpenJDK/openjdk13-binaries/releases/download/jdk-13.0.2%2B8/OpenJDK13U-jdk_x64_windows_hotspot_13.0.2_8.msi). (Java 13 is recommended, but works with anything greater than Java 11)
* When installing Java, make sure to tick "Add to PATH" so that you will have less of hard time trying to figure out what went wrong.
* Now we need to run the Lavalink.jar file. Opening the jar file by itself will do nothing so we will need to launch it through the command prompt.
* Type the following in command prompt if you are certain you have Java in your PATH.
  ```bat
  java -jar "C:\LOCATION\TO\Lavalink.jar"
  ```
* If that did not work, or you know that Java is not in your PATH, please specify where your Java is. It is most likely in the following path...
  ```bat
  "C:\Program Files\AdoptOpenJDK\jre-VERSION_NUMBER-hotspot\bin\java.exe" -jar "C:\LOCATION\TO\Lavalink.jar"
  ```
* It should look like this.
<img src="https://i.imgur.com/t933IU4.png" alt="Lavalink" width="900">


### Spotify API Prerequisites

At the current state, the bot requires a Spotify API key to function.
This is required for requesting the Spotify links.
* Head to Spotify's [dashboard](https://developer.spotify.com/dashboard/) and create a new application.
* Name it whatever you want and add a description.
* Now grab the Client ID and Client Secret. **Do not share this with anyone.**
* Keep this safe for when you first run the bot as it will ask you for it!


### Discord Token Prerequisites

We need a Discord bot token to run the bot.
* Head to Discord's [portal](https://discord.com/developers/applications) and create a new application.
* Name it whatever you want.
* Now grab the token from the Bot section on the left side. **Do not share this with anyone.**
* Keep this safe for when you first run the bot as it will ask you for it!


### Bot Installation

For the easiest way possible, do the following:
1. Download the bot from the [Releases](https://github.com/MuseMusicBot/MuseBot/releases) page.
2. Run the executable file and follow the prompt.

If you want to run the bot from master, do the following:
1. Download the project zip.
2. Download and install .NET Runtime from [here](https://dotnet.microsoft.com/download).
3. Extract and navigate to the bot's folder. In the File Explorer's address bar, type "CMD".
4. This should open a command prompt. Now just type the following:
  ```bat
  dotnet run
  ```



<!-- USAGE EXAMPLES -->
## Usage
Commands exist, but we do recommend just using the reaction the bot has to make things easier. Here are some of the commands:
| Command | Outcome |
| ------ | ------ |
| setup | creates the requester channel |
| help | shows all the commands |
| play | followed with a link to play a video |
| leave | leaves the voice channel |
| pause | pauses the music |
| resume | resumes the music |
| stop | clears the queue and stops the music |
| skip | skips the music |
| clear | clears the queue |
| volume | changes the volume |
| remove | remove a track from the queue |
| seek | seeks to a position in the song |



<!-- CONTRIBUTING -->
## Contributing

If you want to improve this bot, feel free to make a pull request or fork this project. At this state, the bot is considered mostly finished and will not see much commits from us. If you do decide to use this bot as a template for your own, we will appreciate if you could credit us somewhere.
The bot is not perfect and we are aware there are some issues with it, but since they function we are considering this to be good enough.

Some issues we are aware of:
* Spotify playlist is quite slow
* Thumbnail doesn't show in rare instances
* You need to run the setup command to start requesting any music


<!-- ACKNOWLEDGEMENTS -->
## Acknowledgements
* [Hydra](https://hydra.bot/)
* [Musii](https://github.com/encodeous/musii)
* [Discord API Channel](https://discord.gg/discord-api)
