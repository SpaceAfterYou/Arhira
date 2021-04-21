using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using static Arhira.Defines;

namespace Arhira
{
    public sealed class Worker : IHostedService
    {
        private readonly DiscordSocketClient _client;
        private readonly ILogger<Worker> _logger;
        private readonly string _token;

        public Worker(ILogger<Worker> logger, IConfiguration configuration)
        {
            _logger = logger;
            _token = configuration["Discord:Token"];
            _client = new DiscordSocketClient(new DiscordSocketConfig
            {
                AlwaysDownloadUsers = true,
                LogLevel = LogSeverity.Verbose,
                DefaultRetryMode = RetryMode.AlwaysFail
            });

            _client.ReactionAdded += Client_ReactionAdded;
            _client.ReactionRemoved += Client_ReactionRemoved;

            _client.Log += LogAsync;
            _client.Ready += Client_Ready;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await _client.LoginAsync(TokenType.Bot, _token).ConfigureAwait(false);
            await _client.StartAsync().ConfigureAwait(false);
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return _client.StopAsync();
        }

        private Task Client_ReactionRemoved(Cacheable<IUserMessage, ulong> arg1, ISocketMessageChannel channel,
            SocketReaction reaction)
        {
            _logger.LogInformation("Client_ReactionRemoved");

            if (reaction.MessageId != MessagesIds.HelloMessage) return Task.CompletedTask;

            var guild = _client.Guilds.FirstOrDefault(s => s.Id == GuildId);

            var emote = guild?.Emotes.FirstOrDefault(s => s.Name == reaction.Emote.Name);
            if (emote is null || EmojiRolePairs.All(s => s.EmojiId != emote.Id)) return Task.CompletedTask;

            IGuildUser? user = guild?.Users.FirstOrDefault(s => s.Id == reaction.UserId);
            if (user is null) return Task.CompletedTask;

            var pair = EmojiRolePairs.FirstOrDefault(s => s.EmojiId == emote.Id);
            if (pair is null) return Task.CompletedTask;

            var role = guild?.Roles.FirstOrDefault(s => s.Id == pair.RoleId);
            return role is null ? Task.CompletedTask : user.RemoveRoleAsync(role);
        }

        private Task Client_ReactionAdded(Cacheable<IUserMessage, ulong> arg1, ISocketMessageChannel channel,
            SocketReaction reaction)
        {
            _logger.LogInformation("Client_ReactionAdded");

            if (reaction.MessageId != MessagesIds.HelloMessage) return Task.CompletedTask;

            var guild = _client.Guilds.FirstOrDefault(s => s.Id == GuildId);

            var emote = guild?.Emotes.FirstOrDefault(s => s.Name == reaction.Emote.Name);
            if (emote is null || EmojiRolePairs.All(s => s.EmojiId != emote.Id)) return Task.CompletedTask;

            IGuildUser? user = guild?.Users.FirstOrDefault(s => s.Id == reaction.UserId);
            if (user is null) return Task.CompletedTask;

            var pair = EmojiRolePairs.FirstOrDefault(s => s.EmojiId == emote.Id);
            if (pair is null) return Task.CompletedTask;

            var role = guild?.Roles.FirstOrDefault(s => s.Id == pair.RoleId);
            return role is null ? Task.CompletedTask : user.AddRoleAsync(role);
        }

        private Task Client_Ready()
        {
            _logger.LogInformation("Client_Ready");
            _logger.LogInformation($"{_client.CurrentUser} is connected!");

            var guild = _client.Guilds.FirstOrDefault(s => s.Id == GuildId);
            if (guild is not null) _ = Task.Run(async () => await RoleRestore(guild).ConfigureAwait(false));

            return Task.CompletedTask;
        }

        private async Task RoleRestore(SocketGuild guild)
        {
            _logger.LogInformation("RoleRestore");

            IReadOnlyDictionary<GuildEmote, SocketRole> roles = EmojiRolePairs
                .Select(s => new {Key = guild.Emotes.First(e => e.Id == s.EmojiId)!, Value = guild.GetRole(s.RoleId)})
                .ToDictionary(k => k.Key, v => v.Value);

            ITextChannel channel = guild.TextChannels.First(s => s.Id == ChannelIds.EntryPoint);

            IMessage message = await channel.GetMessageAsync(MessagesIds.HelloMessage).ConfigureAwait(false);

            foreach ((IEmote emote, var metadata) in message.Reactions)
            await foreach (IReadOnlyCollection<IUser> users in message.GetReactionUsersAsync(emote, int.MaxValue))
            foreach (IUser user in users)
            {
                var guildUser = guild.GetUser(user.Id);
                if (guildUser is not null)
                {
                    var guildEmote = guild.Emotes.FirstOrDefault(s => s.Name == emote.Name);
                    if (guildEmote is not null && roles.TryGetValue(guildEmote, out var role))
                        await guildUser.AddRoleAsync(role).ConfigureAwait(false);
                }
            }

            _logger.LogInformation("RoleRestore Done");
        }

        private Task LogAsync(LogMessage log)
        {
            _logger.LogInformation(log.ToString());
            return Task.CompletedTask;
        }
    }
}