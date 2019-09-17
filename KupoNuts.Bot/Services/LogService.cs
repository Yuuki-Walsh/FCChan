﻿// This document is intended for use by Kupo Nut Brigade developers.

namespace KupoNuts.Bot.Services
{
	using System;
	using System.Collections.Generic;
	using System.Text;
	using System.Threading.Tasks;
	using Discord;
	using Discord.WebSocket;
	using KupoNuts.Bot.Commands;
	using KupoNuts.Utils;

	public class LogService : ServiceBase
	{
		private Queue<string> logMessages = new Queue<string>();

		public override Task Initialize()
		{
			Log.MessageLogged += this.OnMessageLogged;
			Log.ExceptionLogged += this.OnExceptionLogged;

			Program.DiscordClient.UserJoined += this.DiscordClient_UserJoined;
			Program.DiscordClient.UserLeft += this.DiscordClient_UserLeft;
			Program.DiscordClient.UserBanned += this.DiscordClient_UserBanned;
			Program.DiscordClient.UserUnbanned += this.DiscordClient_UserUnbanned;

			return base.Initialize();
		}

		public override Task Shutdown()
		{
			Program.DiscordClient.UserJoined -= this.DiscordClient_UserJoined;
			Program.DiscordClient.UserLeft -= this.DiscordClient_UserLeft;
			Program.DiscordClient.UserBanned -= this.DiscordClient_UserBanned;
			Program.DiscordClient.UserUnbanned -= this.DiscordClient_UserUnbanned;

			return base.Shutdown();
		}

		[Command("LogMe", Permissions.Administrators, "Test a user join log message.")]
		public async Task LogMe(SocketMessage message)
		{
			await this.PostMessage((SocketGuildUser)message.Author, Color.Purple, "Is testing");
		}

		[Command("Log", Permissions.Administrators, "posts the bot log")]
		public async Task PostLog(SocketMessage message)
		{
			StringBuilder text = new StringBuilder();

			foreach (string msg in this.logMessages)
				text.AppendLine(msg);

			EmbedBuilder builder = new EmbedBuilder();
			builder.Title = "Log" + " [" + DateTime.Now.ToString("HH:mm:ss") + "]";
			builder.Description = text.ToString();

			await message.Channel.SendMessageAsync(null, false, builder.Build());
		}

		private async Task DiscordClient_UserJoined(SocketGuildUser user)
		{
			await this.PostMessage(user, Color.Green, "Joined");
		}

		private async Task DiscordClient_UserLeft(SocketGuildUser user)
		{
			await this.PostMessage(user, Color.LightGrey, "Left");
		}

		private async Task DiscordClient_UserBanned(SocketUser user, SocketGuild guild)
		{
			await this.PostMessage(user, Color.Red, "Was Banned");
		}

		private async Task DiscordClient_UserUnbanned(SocketUser user, SocketGuild guild)
		{
			await this.PostMessage(user, Color.Orange, "Was Unbanned");
		}

		private async Task PostMessage(SocketUser user, Color color, string message)
		{
			if (!ulong.TryParse(Settings.Load().UserLogChannel, out ulong channelId))
				return;

			SocketTextChannel? channel = Program.DiscordClient.GetChannel(channelId) as SocketTextChannel;

			if (channel == null)
				return;

			EmbedBuilder builder = new EmbedBuilder();
			builder.Color = color;
			builder.Title = user.Username + " " + message;
			builder.Timestamp = DateTimeOffset.Now;
			builder.ThumbnailUrl = user.GetAvatarUrl();

			if (user is SocketGuildUser guildUser)
			{
				builder.Title = guildUser.Nickname + " (" + user.Username + ") " + message;
				builder.AddField("Joined", TimeUtils.GetDateString(guildUser.JoinedAt), true);
			}

			builder.AddField("Created", TimeUtils.GetDateString(user.CreatedAt), true);

			builder.Footer = new EmbedFooterBuilder();
			builder.Footer.Text = "ID: " + user.Id;

			await channel.SendMessageAsync(null, false, builder.Build());
		}

		private void OnExceptionLogged(string str)
		{
			this.OnMessageLogged(str);
		}

		private void OnMessageLogged(string str)
		{
			this.logMessages.Enqueue(str);

			while (this.logMessages.Count > 100)
			{
				this.logMessages.Dequeue();
			}
		}
	}
}