// Copyright (c) FCChan. All rights reserved.
//
// Licensed under the MIT license.

namespace FC.Bot.Characters
{
	using System;
	using System.Collections.Generic;
	using System.Text;
	using Discord;
	using XIVAPI;
	using XIVAPICharacter = XIVAPI.Character;

	public static class CharacterExtensions
	{
		public static XIVAPI.ClassJob? GetClassJob(this XIVAPICharacter self, Jobs id)
		{
			if (self.ClassJobs == null)
				return null;

			foreach (XIVAPI.ClassJob job in self.ClassJobs)
			{
				if (job.Job == null)
					return null;

				if (job.Job.ID != (uint)id)
					continue;

				return job;
			}

			return null;
		}

		public static Embed GetGear(this XIVAPICharacter self)
		{
			if (self.GearSet == null || self.GearSet.Gear == null)
				throw new Exception("No gear set on character.");

			EmbedBuilder builder = new EmbedBuilder();
			builder.ImageUrl = self.Portrait;
			builder.ThumbnailUrl = "https://xivapi.com/" + self.GearSet.Gear.MainHand?.Item?.Icon;
			builder.Title = self.Name;
			builder.Description = "Average item level: " + self.GetAverageLevel().ToString();

			builder.AddGear("Main Hand", self.GearSet.Gear.MainHand, false);
			builder.AddGear("Off Hand", self.GearSet.Gear.OffHand, false);
			builder.AddGear("Head", self.GearSet.Gear.Head, true);
			builder.AddGear("Body", self.GearSet.Gear.Body, true);
			builder.AddGear("Hands", self.GearSet.Gear.Hands, true);
			builder.AddGear("Waist", self.GearSet.Gear.Waist, true);
			builder.AddGear("Legs", self.GearSet.Gear.Legs, true);
			builder.AddGear("Feet", self.GearSet.Gear.Feet, true);
			builder.AddGear("Earrings", self.GearSet.Gear.Earrings, true);
			builder.AddGear("Necklace", self.GearSet.Gear.Necklace, true);
			builder.AddGear("Bracelets", self.GearSet.Gear.Bracelets, true);
			builder.AddGear("Ring", self.GearSet.Gear.Ring1, true);
			builder.AddGear("Ring", self.GearSet.Gear.Ring2, true);

			return builder.Build();
		}

		public static int GetAverageLevel(this XIVAPICharacter self)
		{
			if (self.GearSet == null || self.GearSet.Gear == null)
				throw new Exception("No gear set on character.");

			int total = 0;
			int count = 0;
			count += AddItemlevel(self.GearSet.Gear.Body?.Item, ref total) ? 1 : 0;
			count += AddItemlevel(self.GearSet.Gear.Bracelets?.Item, ref total) ? 1 : 0;
			count += AddItemlevel(self.GearSet.Gear.Earrings?.Item, ref total) ? 1 : 0;
			count += AddItemlevel(self.GearSet.Gear.Feet?.Item, ref total) ? 1 : 0;
			count += AddItemlevel(self.GearSet.Gear.Hands?.Item, ref total) ? 1 : 0;
			count += AddItemlevel(self.GearSet.Gear.Head?.Item, ref total) ? 1 : 0;
			count += AddItemlevel(self.GearSet.Gear.Legs?.Item, ref total) ? 1 : 0;
			count += AddItemlevel(self.GearSet.Gear.MainHand?.Item, ref total) ? 1 : 0;
			count += AddItemlevel(self.GearSet.Gear.Necklace?.Item, ref total) ? 1 : 0;
			count += AddItemlevel(self.GearSet.Gear.OffHand?.Item, ref total) ? 1 : 0;
			count += AddItemlevel(self.GearSet.Gear.Ring1?.Item, ref total) ? 1 : 0;
			count += AddItemlevel(self.GearSet.Gear.Ring2?.Item, ref total) ? 1 : 0;
			count += AddItemlevel(self.GearSet.Gear.Waist?.Item, ref total) ? 1 : 0;

			double average = total / count;
			return (int)Math.Round(average);
		}

		public static Embed GetAttributtes(this XIVAPICharacter self)
		{
			if (self.GearSet == null || self.GearSet.Gear == null)
				throw new Exception("No gear set on character.");

			EmbedBuilder builder = new EmbedBuilder();

			Dictionary<AttributeCategories, List<AttributeValue>> attributeLookup = self.GearSet.GetAttributes();

			foreach ((AttributeCategories category, List<AttributeValue> attributes) in attributeLookup)
			{
				StringBuilder attributesBuilder = new StringBuilder();

				foreach (AttributeValue value in attributes)
					attributesBuilder.AppendLine(value.GetString());

				builder.AddField(category.ToString(), attributesBuilder.ToString(), true);
			}

			return builder.Build();
		}

		public static string GetString(this GearSet.GearValue self)
		{
			if (self.Item == null)
				return string.Empty;

			StringBuilder builder = new StringBuilder();
			builder.Append("[");
			builder.Append(self.Item.Name);
			builder.Append("](");
			builder.Append("https://garlandtools.org/db/#item/");
			builder.Append(self.Item.Id);

			if (self.Mirage != null)
			{
				builder.Append(" \"");
				builder.Append(self.Mirage.Name);
				builder.Append("\"");
			}

			builder.AppendLine(")");

			builder.Append("iLv: ");
			builder.Append(self.Item.LevelItem.ToString());
			builder.Append(" ");

			foreach (Data materia in self.Materia)
			{
				builder.Append("[⬤](");
				builder.Append("https://garlandtools.org/db/#item/");
				builder.Append(materia.ID);
				builder.Append(" \"");
				builder.Append(materia.Name);
				builder.Append("\") ");
			}

			return builder.ToString();
		}

		public static Dictionary<AttributeCategories, List<AttributeValue>> GetAttributes(this GearSet self)
		{
			Dictionary<AttributeCategories, List<AttributeValue>> result = new Dictionary<AttributeCategories, List<AttributeValue>>();
			foreach (AttributeValue val in self.Attributes)
			{
				AttributeCategories category = val.GetCategory();
				if (!result.ContainsKey(category))
					result.Add(category, new List<AttributeValue>());

				result[category].Add(val);
			}

			return result;
		}

		public static int GetElementalLevel(this XIVAPICharacter self)
		{
			return self.ClassJobsElemental?.Level ?? 0;
		}

		private static bool AddItemlevel(Item? item, ref int total)
		{
			if (item == null)
				return false;

			total += item.LevelItem;
			return true;
		}

		private static void AddGear(this EmbedBuilder builder, string name, GearSet.GearValue? gear, bool inline)
		{
			if (gear == null)
				return;

			builder.AddField(name, gear.GetString(), inline);
		}
	}
}
