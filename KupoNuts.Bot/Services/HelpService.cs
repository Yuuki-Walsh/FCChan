﻿// This document is intended for use by Kupo Nut Brigade developers.

namespace KupoNuts.Bot.Services
{
	using System;
	using System.Collections.Generic;
	using System.Reflection;
	using System.Text;
	using System.Text.RegularExpressions;
	using System.Threading.Tasks;
	using Discord;
	using Discord.WebSocket;
	using KupoNuts.Bot.Commands;

	public class HelpService : ServiceBase
	{
		public static string GetTypeName(Type type)
		{
			switch (Type.GetTypeCode(type))
			{
				case TypeCode.Boolean: return "boolean";
				case TypeCode.Int16:
				case TypeCode.UInt16:
				case TypeCode.Int32:
				case TypeCode.UInt32:
				case TypeCode.Int64:
				case TypeCode.UInt64:
				case TypeCode.Single:
				case TypeCode.Double:
				case TypeCode.Decimal: return "number";
				case TypeCode.DateTime: return "date and time";
				case TypeCode.String: return "string";
			}

			if (type == typeof(SocketTextChannel))
				return "#channel";

			if (type == typeof(IEmote))
				return ":emote:";

			if (type == typeof(IUser))
				return "@user";

			if (type == typeof(IGuildUser))
				return "@user";

			return type.Name;
		}

		public static string GetParamName(string? name)
		{
			if (name == null)
				return "unknown";

			return Regex.Replace(name, "([A-Z])", " $1", RegexOptions.Compiled).Trim();
		}

		public static async Task GetHelp(SocketMessage message, string command)
		{
			StringBuilder builder = new StringBuilder();

			Permissions permissions = CommandsService.GetPermissions(message.Author);

			builder.AppendLine(GetHelp(command, permissions));

			EmbedBuilder embed = new EmbedBuilder();
			embed.Description = builder.ToString();

			if (string.IsNullOrEmpty(embed.Description))
			{
				await message.Channel.SendMessageAsync("I'm sorry, I didn't find any help for that command.");
				return;
			}

			await message.Channel.SendMessageAsync(null, false, embed.Build());
		}

		public static async Task GetHelp(SocketMessage message, Permissions permissions)
		{
			StringBuilder builder = new StringBuilder();

			List<string> commandStrings = new List<string>(CommandsService.GetCommands());
			commandStrings.Sort();

			foreach (string commandString in commandStrings)
			{
				if (commandString == "help")
					continue;

				builder.AppendLine(commandString);

				/*string? help = GetHelp(commandString, permissions);
				if (help != null)
				{
					builder.AppendLine(help);
				}*/
			}

			EmbedBuilder embed = new EmbedBuilder();
			embed.Description = builder.ToString();

			string messageStr = "To get more information on a specific command, look it up directly, like `>>help \"GoodBot\"` or `>>goodbot ?`";
			await message.Channel.SendMessageAsync(messageStr, false, embed.Build());
		}

		[Command("Help", Permissions.Everyone, "really?")]
		public async Task Help(SocketMessage message)
		{
			Permissions permissions = CommandsService.GetPermissions(message.Author);
			await GetHelp(message, permissions);
		}

		[Command("Help", Permissions.Everyone, "really really?")]
		public async Task Help(SocketMessage message, string command)
		{
			await GetHelp(message, command);
		}

		private static string? GetHelp(string commandStr, Permissions permissions)
		{
			StringBuilder builder = new StringBuilder();
			List<Command> commands = CommandsService.GetCommands(commandStr);

			int count = 0;
			foreach (Command command in commands)
			{
				// Don't show commands users cannot access
				if (command.Permission > permissions)
					continue;

				count++;
			}

			if (count <= 0)
				return null;

			builder.Append("__");
			////builder.Append(CommandsService.CommandPrefix);
			builder.Append(commandStr);
			builder.AppendLine("__");

			foreach (Command command in commands)
			{
				// Don't show commands users cannot access
				if (command.Permission > permissions)
					continue;

				builder.Append(Utils.Characters.Tab);
				builder.Append(command.Permission);
				builder.Append(" - *");
				builder.Append(command.Help);
				builder.AppendLine("*");

				List<ParameterInfo> parameters = command.GetNeededParams();

				builder.Append("**");
				builder.Append(Utils.Characters.Tab);
				builder.Append(CommandsService.CommandPrefix);
				builder.Append(commandStr);
				builder.Append(" ");

				for (int i = 0; i < parameters.Count; i++)
				{
					if (i != 0)
						builder.Append(", ");

					ParameterInfo param = parameters[i];

					builder.Append('[');
					builder.Append(GetParamName(param.Name));
					builder.Append(']');
				}

				builder.Append("**");
				builder.AppendLine();
				builder.AppendLine();
			}

			return builder.ToString();
		}
	}
}
