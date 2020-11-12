using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace TestDiscordBot
{
    class Program
    {
        private static DiscordSocketClient _client;
        private static CommandService _commands;
        private static IServiceProvider _services;

        static void Main(string[] args)
        {
            RunBotAsync().GetAwaiter().GetResult();
        }

        public static async Task RunBotAsync()
        {
            _client = new DiscordSocketClient();
            _commands = new CommandService();

            _services = new ServiceCollection()
                .AddSingleton(_client)
                .AddSingleton(_commands)
                .BuildServiceProvider();
            
            string token = ConfigurationManager.ConnectionStrings["token"].ConnectionString;

            _client.Log += _client_Log;

            await RegisterCommandsAsync();

            await _client.LoginAsync(Discord.TokenType.Bot, token);
            await _client.StartAsync();
            
            await _client.SetGameAsync("Harvesting spice", type: Discord.ActivityType.CustomStatus);

            await Task.Delay(-1);
        }

        private static Task _client_Log(Discord.LogMessage arg)
        {
            Console.WriteLine(arg);
            return Task.CompletedTask;
        }

        public static async Task RegisterCommandsAsync()
        {
            _client.MessageReceived += HandleCommandAsync;
            await _commands.AddModulesAsync(Assembly.GetEntryAssembly(), _services);
        }

        private static async Task HandleCommandAsync(SocketMessage arg)
        {
            var message = arg as SocketUserMessage;
            var context = new SocketCommandContext(_client, message);

            if (message.Author.IsBot)
                return;

            int argPos = 0;
            if (message.HasStringPrefix("!", ref argPos))
            {
                var result = await _commands.ExecuteAsync(context, argPos, _services);
                if (!result.IsSuccess)
                    Console.WriteLine(result.Error);
            }
        }
    }
}
