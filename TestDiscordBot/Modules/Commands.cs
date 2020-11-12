using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TestDiscordBot.Utils;

namespace TestDiscordBot.Modules
{
    public class Commands : ModuleBase<SocketCommandContext>
    {
        private static Random rng = new Random();

        private static readonly Emoji thumbsDownEmoji = new Emoji("👎");
        private static readonly Emoji thumbsUpEmoji = new Emoji("👍");
        private static readonly Emoji coolEmoji = new Emoji("😎");
        private static readonly Emoji upEmoji = new Emoji("⬆");
        private static readonly Emoji downEmoji = new Emoji("⬇");

        private static Dictionary<string, int> games = new Dictionary<string, int>();

        [Command("help")]
        public async Task Help()
        {
            var embed = new EmbedBuilder()
                .WithTitle("Available Commands")
                .AddField("!playguess", "Start a new game of 'Guess the number'")
                .AddField("!guess <number>", "Make a guess in the 'Guess the number' game")
                .AddField("!role <role name>", "List all users with this role")
                .AddField("!user <username/nickname>", "List user's roles")
                .AddField("!ping", "Test if the bot is responsive")
                .AddField("!mute", "ADMIN ONLY - Server Mute all users in your current voice channel")
                .AddField("!unmute", "ADMIN ONLY - Unmute all users in your current voice channel")
                .AddField("!purge", "ADMIN ONLY - Delete all messages in this channel");

            await ReplyAsync(embed: embed.Build());
        }

        [Command("ping")]
        public async Task Ping()
        {
            var emojis = new[]
            {
                new Emoji("🇵"),
                new Emoji("🇴"),
                new Emoji("🇳"),
                new Emoji("🇬"),
            };
            await Context.Message.AddReactionsAsync(emojis);
        }

        [Command("user")]
        public async Task UserInfo(SocketUser user = null)
        {
            var userInfo = (user ?? Context.Client.CurrentUser) as SocketGuildUser;
            var name = userInfo.Nickname ?? $"{userInfo.Username}#{userInfo.Discriminator}";

            var embed = new EmbedBuilder()
                .WithAuthor(userInfo.Nickname ?? userInfo.Username, iconUrl: userInfo.GetAvatarUrl())
                .WithColor(rng.Color());
            
            var sb = new StringBuilder();
            foreach (SocketRole role in userInfo.Roles)
            {
                if (!role.IsEveryone)
                    sb.AppendLine(role.Name);
            }

            embed.AddField("Roles", sb.ToString());

            await ReplyAsync(embed: embed.Build());
        }

        [Command("playguess")]
        public async Task StartGuessingGame()
        {
            string user = $"{Context.User.Username}#{Context.User.Discriminator}";
            if (games.ContainsKey(user))
            {
                await ReplyAsync("You are already playing!");
                return;
            }

            games.Add(user, rng.Next(0, 101));
            await ReplyAsync("Guess a number!");
        }

        [Command("guess")]
        public async Task GuessNumber(int guess)
        {
            string user = $"{Context.User.Username}#{Context.User.Discriminator}";
            if (!games.ContainsKey(user))
            {
                await ReplyAsync("You are not playing a game!");
                return;
            }

            int num = games[user];
            if (guess > num)
            {
                await Context.Message.AddReactionAsync(downEmoji);
                //await ReplyAsync("Too big!");
            }
            else if (guess < num)
            {
                await Context.Message.AddReactionAsync(upEmoji);
                //await ReplyAsync("Too small...");
            }
            else
            {
                games.Remove(user);
                await Context.Message.AddReactionAsync(coolEmoji);
                await ReplyAsync("You win!", embed: new EmbedBuilder().AddField("The number was:", $"{num}").Build());
            }
        }

        [RequireUserPermission(GuildPermission.MuteMembers, Group = "Permission")]
        [Command("mute")]
        public async Task Mute()
        {
            var voice = Context.Guild.VoiceChannels.Where(vc => vc.Users.Contains(Context.User)).FirstOrDefault();

            if (voice == null)
            {
                await ReactNegative();
                return;
            }
            
            foreach (var user in voice.Users)
            {
                await user.ModifyAsync(x => x.Mute = true);
            }

            await ReactPositive();
            return;
        }

        [RequireUserPermission(GuildPermission.MuteMembers, Group = "Permission")]
        [Command("unmute")]
        public async Task Unmute()
        {
            var voice = Context.Guild.VoiceChannels.Where(vc => vc.Users.Contains(Context.User)).FirstOrDefault();

            if (voice == null)
            {
                await ReactNegative();
                return;
            }

            foreach (var user in voice.Users)
            {
                await user.ModifyAsync(x => x.Mute = false);
            }

            await ReactPositive();
            return;
        }
        
        [RequireUserPermission(GuildPermission.ManageMessages, Group = "Permission")]
        [Command("purge")]
        public async Task PurgeMessagesAsync(int rowCount = 0)
        {
            var messages = await Context.Channel.GetMessagesAsync(rowCount == 0 ? int.MaxValue : rowCount).FlattenAsync();

            // filter out pinned messages older than 14 days
            var filteredMessages = messages.Where(x => !x.IsPinned && (DateTimeOffset.UtcNow - x.Timestamp).TotalDays <= 14);

            var count = filteredMessages.Count();

            if (count == 0)
                await ReplyAsync("Nothing to delete.");
            else
            {
                await (Context.Channel as ITextChannel).DeleteMessagesAsync(filteredMessages);
                await ReactPositive();
            }
        }

        [Command("role")]
        public async Task UsersByRoleAsync(string roleName = "")
        {
            if (roleName == "")
            {
                await ReactNegative();
                return;
            }

            var role = Context.Guild.Roles.Where(r => r.Name.ToLower() == roleName.ToLower()).FirstOrDefault();
            if (role == null || role.IsEveryone)
            {
                await ReactNegative();
                return;
            }

            var embed = new EmbedBuilder()
                .WithTitle($"\"{role.Name}\" members:")
                .WithColor(rng.Color())
                .WithFooter($"Total: {role.Members.Count()}");

            StringBuilder sb = new StringBuilder();
            foreach (var member in role.Members)
            {
                sb.AppendLine(member.Nickname ?? member.Username);
            }
            embed.WithDescription(sb.ToString());

            await ReplyAsync(embed: embed.Build());
        }

        public async Task ReactNegative() => await Context.Message.AddReactionAsync(thumbsDownEmoji);
        public async Task ReactPositive() => await Context.Message.AddReactionAsync(thumbsUpEmoji);
    }
}
