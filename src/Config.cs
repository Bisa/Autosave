using System;
using System.IO;
using SMLHelper.V2.Json;
using SMLHelper.V2.Options.Attributes;
using static QModManager.Utility.Logger;

namespace Autosave
{
	[Menu("Autosave", LoadOn = MenuAttribute.LoadEvents.MenuRegistered | MenuAttribute.LoadEvents.MenuOpened)]
	public class Config : ConfigFile
	{

		// Maximum config values
		const int MinutesBetweenAutosavesMax = 360;
		const int MaxAutosaveSlotsMax = 50;
		const int MinimumPlayerHealthPercentMax = 100;

		// Minimum config values
		const int MinutesBetweenAutosavesMin = 1;
		const int MaxAutosaveSlotsMin = 1;
		const int MinimumPlayerHealthPercentMin = 0;

		// Default config values
		const int MinutesBetweenAutosavesDefault = 15;
		const int MaxAutosaveSlotsDefault = 3;
		const int MinimumPlayerHealthPercentDefault = 25;
		const bool AutoSavePermaDeathDefault = true;

		// Config value descriptions
		const string MinutesBetweenAutosavesDesc = "Minutes between autosaves";
		const string MaxAutosaveSlotsDesc = "Max autosave slots";
		const string MinimumPlayerHealthPercentDesc = "Minimum player health %";
		const string AutoSavePermaDeathDesc = "Autosave when permanent death is enabled";

		// Config value definitions
		[Slider(MinutesBetweenAutosavesDesc, MinutesBetweenAutosavesMin, MinutesBetweenAutosavesMax, DefaultValue = MinutesBetweenAutosavesDefault)]
		public int MinutesBetweenAutosaves = MinutesBetweenAutosavesDefault;

		[Slider(MaxAutosaveSlotsDesc, MaxAutosaveSlotsMin, MaxAutosaveSlotsMax, DefaultValue = MaxAutosaveSlotsDefault)]
		public int MaxAutosaveSlots = MaxAutosaveSlotsDefault;

		[Slider(MinimumPlayerHealthPercentDesc, MinimumPlayerHealthPercentMin, MinimumPlayerHealthPercentMax, DefaultValue = MinimumPlayerHealthPercentDefault)]
		public int MinimumPlayerHealthPercent = MinimumPlayerHealthPercentDefault;
		
		[ToggleAttribute(AutoSavePermaDeathDesc)]
		public bool AutoSavePermaDeath = AutoSavePermaDeathDefault;

		internal static void ValidateAndFix(Config conf)
		{
			string fixingFormat = "Fixing config for '{0}', invalid value '{1}' - using '{2}' instead."; // TODO: Translate
			bool fixedSomething = false;

			if(conf.MinutesBetweenAutosaves > MinutesBetweenAutosavesMax)
			{
				string msg = string.Format(
					fixingFormat,
					MinutesBetweenAutosavesDesc,
					conf.MinutesBetweenAutosaves,
					MinutesBetweenAutosavesMax
				);
				conf.MinutesBetweenAutosaves = MinutesBetweenAutosavesMax;
				Entry.LogWarning(msg);
				fixedSomething = true;
			}

			else if(conf.MinutesBetweenAutosaves < MinutesBetweenAutosavesMin)
			{
				string msg = string.Format(
					fixingFormat,
					MinutesBetweenAutosavesDesc,
					conf.MinutesBetweenAutosaves,
					MinutesBetweenAutosavesMin
				);
				conf.MinutesBetweenAutosaves = MinutesBetweenAutosavesMin;
				Entry.LogWarning(msg);
				fixedSomething = true;
			}

			if(conf.MaxAutosaveSlots > MaxAutosaveSlotsMax)
			{
				string msg = string.Format(
					fixingFormat,
					MaxAutosaveSlotsDesc,
					conf.MaxAutosaveSlots,
					MaxAutosaveSlotsMax
				);
				conf.MaxAutosaveSlots = MaxAutosaveSlotsMax;
				Entry.LogWarning(msg);
				fixedSomething = true;
			}

			if(conf.MaxAutosaveSlots < MaxAutosaveSlotsMin)
			{
				string msg = string.Format(
					fixingFormat,
					MaxAutosaveSlotsDesc,
					conf.MaxAutosaveSlots,
					MaxAutosaveSlotsMin
				);
				conf.MaxAutosaveSlots = MaxAutosaveSlotsMin;
				Entry.LogWarning(msg);
				fixedSomething = true;
			}

			if(conf.MinimumPlayerHealthPercent > MinimumPlayerHealthPercentMax)
			{
				string msg = string.Format(
					fixingFormat,
					MinimumPlayerHealthPercentDesc,
					conf.MinimumPlayerHealthPercent,
					MinimumPlayerHealthPercentMax
				);
				conf.MinimumPlayerHealthPercent = MinimumPlayerHealthPercentMax;
				Entry.LogWarning(msg);
				fixedSomething = true;
			}

			if(conf.MinimumPlayerHealthPercent < MinimumPlayerHealthPercentMin)
			{
				string msg = string.Format(
					fixingFormat,
					MinimumPlayerHealthPercentDesc,
					conf.MinimumPlayerHealthPercent,
					MinimumPlayerHealthPercentMin
				);
				conf.MinimumPlayerHealthPercent = MinimumPlayerHealthPercentMin;
				Entry.LogWarning(msg);
				fixedSomething = true;
			}

			if(fixedSomething)
			{
				Entry.DisplayMenuWarn("Fixed invalid config value(s), see log for details.");
			}
		}

		public class LegacySettings : ConfigFile
		{
			public LegacySettings() : base("settings") {}

			public int SecondsBetweenAutosaves = 900;

			public int MaxSaveFiles = 3;

			public int MinimumPlayerHealthPercent = 25;

			public bool AutoSavePermaDeath = true;
    	}

        internal void MigrateAndLoadLegacySettings()
        {
			Entry.LogInfo("Converting legacy settings.json");
			
			LegacySettings legacy = new LegacySettings();
			legacy.Load();
			
			// Convert legacy values
			Entry.Config.MinutesBetweenAutosaves = (legacy.SecondsBetweenAutosaves / 60);
			Entry.Config.MaxAutosaveSlots = legacy.MaxSaveFiles;
			Entry.Config.MinimumPlayerHealthPercent = legacy.MinimumPlayerHealthPercent;
			Entry.Config.AutoSavePermaDeath = legacy.AutoSavePermaDeath;

			// Move the legacy config			
			FileInfo legacyfile = new FileInfo(legacy.JsonFilePath);
			legacyfile.MoveTo(legacyfile.FullName + ".legacy");

			Entry.DisplayMenuInfo("Converted legacy settings.json to config.json"); // TODO: Translate
        }
    }
}
