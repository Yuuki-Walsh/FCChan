﻿// Copyright (c) FCChan. All rights reserved.
//
// Licensed under the MIT license.

namespace FC.Bot.Lodestone
{
	using System;
	using System.Collections.Generic;
	using System.Threading.Tasks;
	using Discord;
	using FC.Bot.Commands;
	using FC.Bot.Services;
	using FC.Data;
	using FC.Utils;
	using global::Lodestone.News;
	using NodaTime;

	public class LodestoneService : ServiceBase
	{
		private Table<PostedNews> newsDb = new Table<PostedNews>("KupoNuts_News", 0);

		public override async Task Initialize()
		{
			await this.newsDb.Connect();
			await base.Initialize();

			await this.Update();
			ScheduleService.RunOnSchedule(this.Update, 15);
		}

		[Command("maintenance", Permissions.Everyone, "Gets info about the next maintenance window.")]
		[Command(@"maint", Permissions.Everyone, "Gets info about the next maintenance window.")]
		public async Task<Embed> GetMaintenance()
		{
			List<NewsItem> items = await NewsAPI.Latest(Categories.Maintenance);

			Instant now = TimeUtils.Now;
			NewsItem? nextMaint = null;
			Instant? bestStart = null;
			foreach (NewsItem item in items)
			{
				Instant? start = item.GetStart();
				Instant? end = item.GetEnd();

				if (start == null || end == null)
					continue;

				if (!item.Title.Contains("All Worlds"))
					continue;

				if (start < bestStart)
					continue;

				if (start < now.Minus(Duration.FromDays(14)))
					continue;

				bestStart = start;
				nextMaint = item;
			}

			if (nextMaint != null)
			{
				EmbedBuilder builder = new EmbedBuilder();
				builder.ThumbnailUrl = "https://img.finalfantasyxiv.com/lds/h/F/DlQYVw2bqzA5ZOCfXKZ-Qe1IZU.svg";
				builder.Title = nextMaint.Title;

				Instant? start = nextMaint.GetStart();
				Instant? end = nextMaint.GetEnd();

				if (start == null || end == null)
					throw new Exception();

				Duration timeUntilStart = (Duration)(start - now);
				Duration timeUntilEnd = (Duration)(end - now);

				if (timeUntilStart.TotalMinutes > 0)
				{
					builder.Description = "Starts In: " + TimeUtils.GetDurationString(start - now);
				}
				else if (timeUntilEnd.TotalMinutes > 0)
				{
					builder.Description = "Ends In: " + TimeUtils.GetDurationString(end - now);
				}
				else
				{
					builder.Description = "Completed: " + TimeUtils.GetDurationString(now - end) + " ago.";
				}

				builder.AddField("Starts", TimeUtils.GetDateTimeString(start));
				builder.AddField("Ends", TimeUtils.GetDateTimeString(end));
				builder.AddField("Duration", TimeUtils.GetDurationString(end - start));
				return builder.Build();
			}

			throw new UserException("I couldn't find any maintenance.");
		}

		[Command("news", Permissions.Administrators, "Updates lodestone news")]
		public async Task Update()
		{
			Log.Write("Updating lodestone news", "Bot");

			if (!ulong.TryParse(Settings.Load().LodestoneChannel, out ulong channelId))
				return;

			List<NewsItem> news = await NewsAPI.Feed();
			news.Reverse();
			foreach (NewsItem item in news)
			{
				if (item.Id == null)
					continue;

				PostedNews entry = await this.newsDb.LoadOrCreate(item.Id);

				if (!entry.IsPosted)
				{
					Log.Write("Posting Lodestone news: " + item.Title, "Bot");
					await item.Post(channelId);

					entry.IsPosted = true;
					await this.newsDb.Save(entry);

					// don't flood channel!
					await Task.Delay(500);
				}
			}
		}

		public class PostedNews : EntryBase
		{
			public bool IsPosted { get; set; }
		}
	}
}
