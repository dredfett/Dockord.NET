﻿using Dockord.Library.Exceptions;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.Interactivity;
using System;
using System.Threading.Tasks;

namespace Dockord.Bot.Modules.InteractivityCommands.ConfirmOrDeny
{
    public class ReactionResultHandler
    {
        private readonly CommandContext _ctx;

        public ReactionResultHandler(CommandContext ctx)
        {
            _ctx = ctx;
        }

        /// <summary>
        /// Update the interactivity reaction message, and then send a new result
        /// message to the original channel that the command was sent from.
        /// </summary>
        /// <param name="reactionMessage"></param>
        /// <param name="reactionEvent"></param>
        public async Task Result(ReactionMessage reactionMessage, InteractivityResult<MessageReactionAddEventArgs> reactionEvent)
        {
            DateTime timestamp = DateTime.UtcNow;
            string resultText = "Denied";
            var emoji = new ConfirmOrDenyEmojiModel(_ctx);
            var embedBuilder = new DiscordEmbedBuilder()
                .WithTimestamp(timestamp)
                .WithColor(DiscordColor.Red);

            if (reactionEvent.Result.Emoji == emoji.Confirmed)
            {
                resultText = "Confirmed";
                embedBuilder.Color = DiscordColor.Green;
            }

            string username = reactionEvent.Result.User?.Username ?? "<unknown username>";
            string footerText = $"{reactionEvent.Result.Emoji} {resultText} by {username}";
            embedBuilder.WithFooter(footerText);

            if (_ctx.Channel.Id != reactionMessage.Channel?.Id)
                await SendResultsToUserDefinedChannel(embedBuilder, resultText).ConfigureAwait(false);

            embedBuilder.AddField("Jump Link", $"[Original Message]({_ctx.Message.JumpLink})");
            await reactionMessage.DeleteAllReactions().ConfigureAwait(false);
            await reactionMessage.Update(embedBuilder).ConfigureAwait(false);
        }

        /// <summary>
        /// Update reaction message with a timeout notice, and then notify the user
        /// who executed the command in a direct message that the command timed out.
        /// </summary>
        /// <param name="reactionMessage"></param>
        public async Task TimeOut(ReactionMessage reactionMessage)
        {
            DateTime timestamp = DateTime.UtcNow;
            DiscordEmoji errorEmoji = DiscordEmoji.FromName(_ctx.Client, ":no_entry:");
            var embedBuilder = new DiscordEmbedBuilder()
                .WithTimestamp(timestamp)
                .WithFooter($"{errorEmoji} Request timed out")
                .WithColor(DiscordColor.Red);

            await reactionMessage.DeleteAllReactions().ConfigureAwait(false);
            await reactionMessage.Update(embedBuilder).ConfigureAwait(false);

            throw new InteractivityTimedOutException("Command timed out. No one neither confirmed nor denied.");
        }

        /// <summary>
        /// Sends the reaction message result to the channel that the command was
        /// originally sent from.
        /// </summary>
        /// <param name="embed"></param>
        /// <param name="resultText"></param>
        private async Task SendResultsToUserDefinedChannel(DiscordEmbedBuilder embed, string resultText)
        {
            embed.WithTitle("Results")
                 .ClearFields()
                 .WithDescription($"{_ctx.User.Mention}: [{resultText}]({_ctx.Message.JumpLink})")
                 .Build();

            await _ctx.RespondAsync(embed).ConfigureAwait(false);
        }
    }
}
