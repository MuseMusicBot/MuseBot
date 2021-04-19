using Discord;
using Newtonsoft.Json;
using System;
using System.IO;

namespace MusicBot.Services
{
    public class ConfigHelper
    {
        public struct Config
        {
            public ulong GuildId { get; set; }
            public ulong ChannelId { get; set; }
            public ulong MessageId { get; set; }
            public string Token { get; set; }
            public string Prefix { get; set; }

            [JsonIgnore]
            public IMessage BotEmbedMessage { get; set; }

            [JsonIgnore]
            public ushort Volume { get; set; }
        }

        public const string ConfigName = "appConfig.json";

        public static Config LoadConfigFile()
        {
            string json = File.ReadAllText(ConfigName);
            return JsonConvert.DeserializeObject<Config>(json);
        }

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


            Config config = new Config
            {
                Prefix = string.IsNullOrWhiteSpace(prefix) ? "m?" : prefix,
                Token = token
            };

            File.WriteAllText(ConfigName, JsonConvert.SerializeObject(config, Formatting.Indented));
        }

        public static void UpdateConfigFile(Config config)
        {
            File.WriteAllText(ConfigName, JsonConvert.SerializeObject(config, Formatting.Indented));
        }

    }
}
