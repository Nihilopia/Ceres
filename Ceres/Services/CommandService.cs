﻿using Discord;
using Discord.Commands;
using Discord.WebSocket;

using Microsoft.Extensions.Configuration;

// (\#if )(DEBUG|RELEASE)
// $1!$2F

namespace Ceres.Services
{
    public class CommandHandler
    {
        private readonly DiscordSocketClient _discord;
        private readonly CommandService _commands;
        private readonly IConfigurationRoot _config;
        private readonly IServiceProvider _provider;

        public CommandHandler(DiscordSocketClient discord, CommandService commands, IConfigurationRoot config, IServiceProvider provider)
        {
            _discord = discord;
            _commands = commands;
            _config = config;
            _provider = provider;
            _discord.MessageReceived += OnMessageReceivedAsync;
        }

        private async Task OnMessageReceivedAsync(SocketMessage s)
        {
            if (s is not SocketUserMessage msg) return;
            if (msg.Author.Id == _discord.CurrentUser.Id) return;

            SocketCommandContext context = new(_discord, msg);
            string prefix = _config["ceres.prefix"];
            int prefixLength = 0;

            #region Reminder emphasizer
            if (s.Embeds != null || s.Embeds.Count != 0)
            {
                IReadOnlyCollection<Embed> msgEmbed = s.Embeds;
                string embedDescription = msgEmbed?.FirstOrDefault()?.Description;
                embedDescription ??= string.Empty;
                if (s.Author.Id == 526166150749618178 && embedDescription.Contains("Reminder from"))
                {
                    await context.Channel.SendMessageAsync("<a:DinkDonk:1025546103447355464>");
                }
            }
            #endregion

            if (msg.HasStringPrefix(prefix, ref prefixLength) || msg.HasMentionPrefix(_discord.CurrentUser, ref prefixLength))
            {
                string commandWithoutPrefix = msg.Content.Replace(prefix, string.Empty).Trim();
                if (string.IsNullOrWhiteSpace(commandWithoutPrefix))
                    return;

                IResult result = _commands.ExecuteAsync(context, prefixLength, _provider).Result;

                if (!result.IsSuccess)
                    await context.Channel.SendMessageAsync(result.ToString());
            }
        }

        public class CommandsCollection : ModuleBase<SocketCommandContext>
        {
            private readonly DiscordSocketClient _discord;
            private readonly CommandService _commands;
            private readonly IConfigurationRoot _config;
            private readonly IServiceProvider _provider;
            private readonly CommonFronterStatusMethods _fronterStatusMethods;
            private readonly LoggingService _logger;
            private readonly DirectoryInfo _folderDir;
            private readonly Random _unsafeRng;
            private readonly Emoji _waitEmote = new("⏳");

            public CommandsCollection(DiscordSocketClient discord, CommandService commands, IConfigurationRoot config, IServiceProvider provider)
            {
                _discord = discord;
                _commands = commands;
                _config = config;
                _provider = provider;
                _fronterStatusMethods = new(discord, config);
                _logger = new();
                _folderDir = new(config["ceres.foldercommandpath"]);
                _unsafeRng = new();
            }

            enum CeresCommand
            {
                UpdateFront,
                Egg,
                AddReaction,
                Say,
                WhoKnows
            }

            [Command("updatefront")]
            [Alias("u", "update", "ufront", "uf", "updatef")]
            [Summary("Updates the fronting status")]
            public Task UpdateFront()
            {
                _ = _fronterStatusMethods.SetFronterStatusAsync();
                return ReplyAsync("Front status updated");
            }

            [Command("egg")]
            [Alias("ei", "eckeaberaufhessisch")]
            [Summary("egg (engl. \"Ei\"")]
            public Task Egg()
            {
                return Context.Channel.SendFileAsync(@"C:\Users\Emmi\Documents\ähm\ei.png");
            }

            [Command("react")]
            [Summary("Adds a reaction to a message")]
            public Task AddReaction(string emote, string messageId = null)
            {
                IMessage msg = null;
                if (messageId != null)
                {
                    bool channelValid = ulong.TryParse(messageId, out ulong messageIdUlong);
                    msg = Task.Run(async () => { return await Context.Channel.GetMessageAsync(messageIdUlong); }).Result;
                    if (msg == null) return CommandError(CeresCommand.AddReaction, "Error: **Command must be executed in the same channel**");
                    if (!channelValid) return CommandError(CeresCommand.AddReaction, "Error: **Something's wrong with the message ID**");
                }
                else
                {
                    if (Context.Message.Reference == null)
                        return CommandError(CeresCommand.AddReaction, "Error: **No message ID specified and not replied to any message**");

                    msg = Task.Run(async () => { return await Context.Channel.GetMessageAsync((ulong)Context.Message.Reference.MessageId); }).Result;
                }

                dynamic reaction = null;
                Emote emoteReaction = null;
                Emoji emojiReaction = null;
                bool emoteValid = Emote.TryParse(emote, out emoteReaction);
                if (!emoteValid)
                {
                    emojiReaction = new Emoji(emote);
                    reaction = emojiReaction;
                }
                else
                {
                    reaction = emoteReaction;
                }

                return msg.AddReactionAsync(reaction);
            }

            [Command("echo")]
            [Alias("say")]
            public Task Say(string msg, ulong channelId = 0ul, ulong guildId = 0ul, ulong replyToMsgID = 0ul)
            {
                #region Guild ID parsing
                if (guildId == 0ul)
                    guildId = Context.Guild.Id;
                SocketGuild guild = Context.Client.GetGuild(guildId);
                if (guild == null)
                    return Context.Channel.SendMessageAsync("Invalid Guild ID");
                #endregion

                #region Channel ID parsing
                if (channelId == 0ul)
                    channelId = Context.Channel.Id;
                if (guild.GetChannel(channelId) is not IMessageChannel messageChannel) // Null check
                    return Context.Channel.SendMessageAsync("Invalid Channel ID");
                #endregion

                #region Message ID parsing
                if (replyToMsgID != 0ul)
                {
                    IMessage replyMessage = Task.Run(async () => { return await messageChannel.GetMessageAsync(replyToMsgID); }).Result;
                    if (replyMessage == null)
                        return Context.Channel.SendMessageAsync("Invalid Message ID");
                    MessageReference reference = new(replyToMsgID, channelId, guildId, true);

                    return messageChannel.SendMessageAsync(msg, messageReference: reference);
                }
                #endregion

                return messageChannel.SendMessageAsync(msg);
            }

            [Command("whoknows")]
            [Alias("wk")]
            public Task WhoKnows()
            {
                return ReplyAsync("```ANSI\n[0;31mDid you mean: [4;34m/whoknows```");
            }

            [Command("folder")]
            [Alias("f")]
            public Task Folder()
            {
                FileInfo[] folderFiles = _folderDir.GetFiles()
                                                   .Where(file => file.Name != "ei.png" || !(file.Attributes.HasFlag(FileAttributes.System) || file.Attributes.HasFlag(FileAttributes.Directory)))
                                                   .ToArray();
                int rand = _unsafeRng.Next(0, folderFiles.Length);
                string filePath = folderFiles[rand].FullName;
                string text= filePath switch
                {
                    "redditsave.com_german_spongebob_is_kinda_weird-vrm48d21ch081.mp4"
                        => "CW Laut",
                    "brr_uzi.mp4"
                        => "CW Laut",
                    "Discord_become_hurensohn2.png"
                        => "Credits: Aurora",
                    _ // default
                        => string.Empty
                };

                return Context.Channel.SendFileAsync(filePath, text);
            }

            private Task CommandError(CeresCommand command, string errorMsg)
            {
                switch (command)
                {
                    case CeresCommand.AddReaction:
                        errorMsg += "\nUsage `!react [emote] [messageId]`";
                        errorMsg += "\nUsage `!react [emote]` when replying to a message. The replied to message will be used as reaction target.";
                        break;
                }

                return ReplyAsync(errorMsg);
            }

            protected override async Task BeforeExecuteAsync(CommandInfo command)
            {
                await Context.Message.AddReactionAsync(_waitEmote);
                LogMessage log = new(LogSeverity.Info, command.Name, $"{Context.User.Username}#{Context.User.Discriminator} used a command in #{Context.Channel.Name}");
                await _logger.OnLogAsync(log);
                await base.BeforeExecuteAsync(command);
            }

            protected override async Task AfterExecuteAsync(CommandInfo command)
            {
                await Context.Message.RemoveReactionAsync(_waitEmote, 966325392707301416);
                await base.AfterExecuteAsync(command);
            }
        }
    }
}
