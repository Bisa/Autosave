using Oculus.Newtonsoft.Json;
using System;
using System.IO;
using static QModManager.Utility.Logger;

namespace Autosave
{
	/* Json serialization from:
	 * https://docs.unity3d.com/Manual/JSONSerialization.html
	 *
	 * Directory fetching snippet from:
	 * https://stackoverflow.com/questions/52797/how-do-i-get-the-path-of-the-assembly-the-code-is-in */

	internal static class ConfigHandler
	{
		private const string SettingsFileName = "settings.json";

		private static bool ValidateValues(Config cfg)
		{
#if (!DEBUG)
			if (cfg.SecondsBetweenAutosaves < 120)
			{
				Entry.LogWarning("Please allow at least two minutes between saves. Config invalidated.");

				return false;
			}

			if (cfg.MaxSaveFiles < 1)
			{
				Entry.LogWarning("Please allow at least one autosave file slot. Config invalidated.");

				return false;
			}

			if (cfg.MaxSaveFiles > 9999)
			{
				Entry.LogWarning("I can not handle more than 9999 autosave files. Config invalidated.");

				return false;
			}

			if (cfg.AutoSavePermaDeath.GetType() != typeof(bool))
			{
				Entry.LogWarning("Please use only true or false for AutoSavePermaDeath. Config invalidated.");

				return false;
			}
#endif

			return true;
		}

		internal static void LoadConfig()
		{
			try
			{
				string settingsFilePath = Path.Combine(Entry.GetAssemblyDirectory, SettingsFileName);
				string settingsAsJson = File.ReadAllText(settingsFilePath);
				Config configFromJson = JsonConvert.DeserializeObject<Config>(settingsAsJson);

				if (ValidateValues(configFromJson))
				{
					Entry.GetConfig = configFromJson;
				}

				else
				{
					Entry.DisplayMessage("Failed to validate your config, reverting to defaults, see log for details!", Level.Warn); // TODO: Translate

					Entry.GetConfig = new Config();
				}

#if DEBUG
				Entry.GetConfig.SecondsBetweenAutosaves = 60;
				Entry.GetConfig.MaxSaveFiles = 4;
#endif
			}

			catch (Exception ex)
			{
				Entry.LogError("Caught exception while executing LoadConfig", ex);
				Entry.DisplayMessage("Encountered an exception, see log for details!", Level.Error); // TODO: Translate
				
				// TODO: Handle the exception?
				// 		 (Methods down the line might expect the config object to be initialized)
			}
		}
	}
}
