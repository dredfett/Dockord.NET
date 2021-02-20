﻿using Dockord.Library.Extensions;
using Dockord.Library.Models;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Exceptions;
using DSharpPlus.Entities;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace Dockord.Bot.Events
{
    class CommandEventHandler : ICommandEventHandler
    {
        private string? _commandName;
        private string? _commandArgs;
        private bool? _isDirectMessage;

        public Task CommandExecuted(CommandsNextExtension sender, CommandExecutionEventArgs e)
        {
            _commandName = e.Command.QualifiedName;
            _commandArgs = e.Context.RawArgumentString;
            EventId eventId = DockordEvents.BotCmdsExec;

            LogCommandEvent(e, eventId, eventMessage: "Command executed successfully.");

            return Task.CompletedTask;
        }

        public async Task CommandErrored(CommandsNextExtension sender, CommandErrorEventArgs e)
        {
            _commandName = e.Command.QualifiedName;
            _commandArgs = e.Context.RawArgumentString;
            _isDirectMessage = e.Context.Channel?.IsPrivate;

            // Check if the error is from a lack of required permissions
            if (e.Exception is ChecksFailedException)
            {
                EventId eventId = DockordEvents.BotCmdsAuthError;
                LogCommandEvent(e, eventId, eventMessage: "User lacked required permissions for command.");

                await SendErrorResponse(e,
                                        title: "Access denied",
                                        description: "You do not have the required permissions to execute this command.");
            }
            else
            {
                EventId eventId = DockordEvents.BotCmdsError;
                LogCommandEvent(e, eventId, eventMessage: "Error executing command.");

                await SendErrorResponse(e,
                                        title: "Error occurred",
                                        description: "There was an unspecified error while executing the command.");
            }
        }

        private void LogCommandEvent(CommandEventArgs e, EventId eventId, string eventMessage = "")
        {
            var eventData = new DiscordEventDataModel
            {
                CommandName = _commandName,
                CommandArgs = _commandArgs,
                Username = e.Context.User?.Username,
                UserDiscriminator = e.Context.User?.Discriminator,
                UserId = e.Context.User?.Id,
                ChannelName = e.Context.Channel?.Name,
                ChannelId = e.Context.Channel?.Id,
                IsDirectMessage = _isDirectMessage,
                GuildName = e.Context.Guild?.Name,
                GuildId = e.Context.Guild?.Id,
            };

            (string message, object[] args) = eventData.ToEventLogTuple(eventMessage);

            if (e is CommandErrorEventArgs ev)
                e.Context.Client.Logger.LogError(eventId, ev.Exception, message, args);
            else
                e.Context.Client.Logger.LogInformation(eventId, message, args);
        }

        private async Task SendErrorResponse(CommandErrorEventArgs e, string title, string description)
        {
            var emoji = DiscordEmoji.FromName(e.Context.Client, ":no_entry:");
            var embed = new DiscordEmbedBuilder()
                .WithTitle(title)
                .WithDescription($"{emoji} {description}")
                .WithColor(DiscordColor.Red)
                .AddField("Command:", $"`{_commandName} {_commandArgs}`")
                .Build();

            if (e.Context.User is DiscordMember user)
                await user.SendMessageAsync(embed);
            else
                await e.Context.RespondAsync(embed);

            if (_isDirectMessage == false)
                await e.Context.Message.DeleteAsync(); // Cleanup invalid command if it is not a DM to bot
        }
    }
}
