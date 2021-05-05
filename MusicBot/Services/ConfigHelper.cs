using Discord;
using Newtonsoft.Json;
using System;
using System.IO;

namespace MusicBot.Services
{
    public class ConfigHelper
    {
        /// <summary>
        /// Default volume
        /// </summary>
        public static ushort DefaultVolume { get; } = 10;

        /// <summary>
        /// Struct for Bot Config
        /// </summary>
        public struct Config
        {
            public ulong GuildId { get; set; }
            public ulong ChannelId { get; set; }
            public ulong MessageId { get; set; }
            public string Token { get; set; }
            public string Prefix { get; set; }
            public string LavalinkHost { get; set; }
            public int LavalinkPort { get; set; }
            public string LavalinkPassword { get; set; }
            public string SpotifyClientId { get; set; }
            public string SpotifySecret { get; set; }

            [JsonIgnore]
            public IUserMessage BotEmbedMessage { get; set; }

            [JsonIgnore]
            public ushort Volume { get; set; }
        }

        /// <summary>
        /// Name of config file
        /// </summary>
        public const string ConfigName = "appConfig.json";

        /// <summary>
        /// Loads config file
        /// </summary>
        /// <returns>Config struct with file values</returns>
        public static Config LoadConfigFile()
        {
            string json = File.ReadAllText(ConfigName);
            var config = JsonConvert.DeserializeObject<Config>(json);
            config.Volume = DefaultVolume;
            return config;
        }

        /// <summary>
        /// Creates config file
        /// </summary>
        public static void CreateConfigFile()
        {
            string token;
            do
            {
                Console.Write("Enter bot token: ");
                token = Console.ReadLine().Trim();
                Console.WriteLine();
            }
            while (string.IsNullOrWhiteSpace(token));

            Console.Write("Enter prefix (default is \"m?\"): ");
            string prefix = Console.ReadLine().Trim();
            Console.Write("Enter SpotifyClientID: ");
            string spotifyClientId = Console.ReadLine().Trim();
            Console.Write("Enter SpotifySecret: ");
            string spotifySecret = Console.ReadLine().Trim();
            Console.Write("Enter LavaLink Host: ");
            string lavaLinkHost = Console.ReadLine().Trim();
            Console.Write("Enter LavaLink Port (default = 2333): ");
            string lavaLinkPort = Console.ReadLine().Trim();
            Console.Write("Enter LavaLink Password: ");
            string lavaLinkPassword = Console.ReadLine().Trim();


            Config config = new Config
            {
                Prefix = string.IsNullOrWhiteSpace(prefix) ? "m?" : prefix,
                Token = token,
                SpotifyClientId = spotifyClientId,
                SpotifySecret = spotifySecret,
                LavalinkHost = lavaLinkHost,
                LavalinkPassword = lavaLinkPassword,
                LavalinkPort = string.IsNullOrWhiteSpace(lavaLinkPort) ? 2333 : int.Parse(lavaLinkPort)
            };

            File.WriteAllText(ConfigName, JsonConvert.SerializeObject(config, Formatting.Indented));
        }

        /// <summary>
        /// Updates config file
        /// </summary>
        /// <param name="config">Config struct to be used to update</param>
        public static void UpdateConfigFile(Config config)
        {
            File.WriteAllText(ConfigName, JsonConvert.SerializeObject(config, Formatting.Indented));
        }

    }
}
