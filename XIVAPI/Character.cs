// Copyright (c) FCChan. All rights reserved.
//
// Licensed under the MIT license.

namespace XIVAPI
{
	using System;
	using System.Collections.Generic;
	using System.Text;

	[Serializable]
	public class Character
	{
		public ClassJob? ActiveClassJob { get; set; }
		public string Avatar { get; set; } = string.Empty;
		public string Bio { get; set; } = string.Empty;
		public List<ClassJob>? ClassJobs { get; set; }
		public ClassJob? ClassJobsElemental { get; set; }
		public string DC { get; set; } = string.Empty;
		public string FreeCompanyId { get; set; } = string.Empty;
		public GearSet? GearSet { get; set; }
		public uint Gender { get; set; }
		public GrandCompany? GrandCompany { get; set; }
		public Data? GuardianDeity { get; set; }
		public uint ID { get; set; }
		////public List<>? Minions;
		////public List<>? Mounts;
		public string Name { get; set; } = string.Empty;
		public string Nameday { get; set; } = string.Empty;
		public uint ParseDate { get; set; }
		public string Portrait { get; set; } = string.Empty;
		////public uint? PvPTeamId { get; set; }
		public Data? Race { get; set; }
		public string Server { get; set; } = string.Empty;
		public Data? Title { get; set; }
		public bool TitleTop { get; set; }
		public Data? Town { get; set; }
		public Data? Tribe { get; set; }

		public override string? ToString()
		{
			return "Character: " + this.Name + " (" + this.ID + ")";
		}
	}
}
