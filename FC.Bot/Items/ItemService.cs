// Copyright (c) FCChan. All rights reserved.
//
// Licensed under the MIT license.

namespace FC.Bot.Items
{
	using System;
	using System.Collections.Generic;
	using System.Text;
	using System.Threading.Tasks;
	using Discord;
	using FC.Bot.Commands;
	using FC.Bot.Services;
	using Universalis;
	using XIVAPI;

	public class ItemService : ServiceBase
	{
		public static string DyeEmote = @"<:Dye:624532865404633088> ";
		public static string SalvageEmote = @"<:Desynth:624533057634041876> ";
		public static string ConvertToMateriaEmote = @"<:ConvertToMateria:624532395261165569> ";
		public static string UntradableEmote = @"<:Untradable:624532956710699008> ";
		public static string UniqueEmote = @"<:Unique:624532714506158091> ";
		public static string ArmoireEmote = @"<:Armoire:624513915765784584> ";
		public static string GlamourDresserEmote = @"<:GlamourDresser:624513915719778305> ";
		public static string CrestEmote = @"<:CompanyCrests:624513915325382678> ";
		public static string AdvancedMeldingForbiddenEmote = @"<:AdvancedMeldingForbidden:624534020906156032> ";
		public static string GilEmote = @"<:Gil:624582640493789184> ";
		public static string HighQualityEmote = @"<:hq:624587323887190025> ";
		public static string NormalQualityEmote = @"<:nq:624606645124857867> ";
		public static string CraftableEmote = @"<:craftable:632174512267329536>";

		[Command("ISearch", Permissions.Everyone, "Gets information on an item")]
		[Command("ItemSearch", Permissions.Everyone, "Gets information on an item")]
		[Command("MB", Permissions.Everyone, "Gets information on an item")]
		public async Task<Embed> GetItem(string search)
		{
			List<SearchAPI.Result> results = await SearchAPI.Search(search, "Item");

			if (results.Count <= 0)
				throw new UserException("I couldn't find any items that match that search.");

			if (results.Count > 1)
			{
				EmbedBuilder embed = new EmbedBuilder();

				StringBuilder description = new StringBuilder();
				for (int i = 0; i < Math.Min(results.Count, 10); i++)
				{
					description.AppendLine(results[i].ID + " - " + results[i].Name);
				}

				embed.Title = results.Count + " results found";
				embed.Description = description.ToString();
				return embed.Build();
			}

			ulong? id = results[0].ID;

			if (id == null)
				throw new Exception("No Id in item");

			return await this.GetItem((ulong)id);
		}

		[Command("ISearch", Permissions.Everyone, "Gets information on an item")]
		[Command("ItemSearch", Permissions.Everyone, "Gets information on an item")]
		[Command("MB", Permissions.Everyone, "Gets information on an item")]
		public async Task<Embed> GetItem(ulong itemId)
		{
			Item item = await ItemAPI.Get(itemId);

			EmbedBuilder embed = item.ToEmbed();

			if (item.IsUntradable != 1)
			{
				(MarketAPI.History? hq, MarketAPI.History? nm) = await MarketAPI.GetBestPrice("Elemental", itemId);

				if (hq != null | nm != null)
				{
					StringBuilder builder = new StringBuilder();
					if (hq != null)
						builder.Append(hq.ToStringEx());

					if (nm != null)
						builder.Append(nm.ToStringEx());

					embed.AddField("Best Market Board prices:", builder.ToString());
				}
			}

			return embed.Build();
		}
	}
}
