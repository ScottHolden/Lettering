using Discord;
using Discord.WebSocket;
using System;
using System.IO;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using System.Text;

namespace Lettering
{
    class Program
    {
        private static DiscordSocketClient _client;
        private static readonly IEmote _tickEmoji = new Emoji("\U00002705");
        private static readonly IEmote _crossEmoji = new Emoji("\U0000274C");
        private static readonly Dictionary<ulong, (char Current, int Length)> _serverCurrent = new Dictionary<ulong, (char Current, int Length)>();
        private static readonly string[] _validWords = File.ReadAllLines("words_alpha.txt").Select(x=>x.ToLower()).ToArray();
        public static async Task Main(string[] args)
        {
            bool wordsChallenge = true;
            bool wordLengthChallenge = true;

            _client = new DiscordSocketClient();
            _client.Log += (message) =>
            {
                Console.WriteLine(message.Message);
                return Task.CompletedTask;
            };
            _client.MessageReceived += async (arg) =>
            {
                if ("lettering".Equals(arg.Channel.Name, StringComparison.OrdinalIgnoreCase) &&
                !arg.Author.IsBot)
                {
                    Console.WriteLine($"{arg.Channel.Name}({arg.Channel.Id}): {arg.Content}");

                    char current = 'A';
                    int length = 1;

                    if (_serverCurrent.ContainsKey(arg.Channel.Id))
                        (current, length) = _serverCurrent[arg.Channel.Id];

                    bool validWord = !wordsChallenge || Array.IndexOf(_validWords, arg.Content.ToLower()) >= 0;
                    bool validLength = !wordLengthChallenge || arg.Content.Length >= length;

                    if (arg.Content.Length > 0 &&
                        arg.Content.StartsWith(current.ToString(), StringComparison.OrdinalIgnoreCase) && 
                        validWord && validLength)
                    {
                        await arg.AddReactionAsync(_tickEmoji);
                        current = (char)(current + 1);
                        length = arg.Content.Length;
                        if (current > 'Z')
                            current = 'A';
                    }
                    else
                    {
                        await arg.AddReactionAsync(_crossEmoji);
                        StringBuilder sb = new StringBuilder($"Fail by {arg.Author.Username}! I wanted a word starting with {current}");
                        if (!validLength) sb.Append($", at least {length} long");
                        if (!validWord) sb.Append(" and that wasn't a valid word");
                        sb.Append("! Next is 'A'");
                        await arg.Channel.SendMessageAsync(sb.ToString());
                        current = 'A';
                    }

                    _serverCurrent[arg.Channel.Id] = (current, length);
                }
            };
            await _client.LoginAsync(TokenType.Bot, args[0]);
            await _client.StartAsync();
            await Task.Delay(-1);
        }
    }
}