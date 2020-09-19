using HarmonyLib;
using QModManager.API;
using QModManager.API.ModLoading;
using QModManager.Utility;
using SMLHelper.V2.Handlers;
using SMLHelper.V2.Json;
using System;
using System.IO;
using System.Reflection;
using static QModManager.Utility.Logger;

namespace Autosave
{
	[QModCore]
	public static class Entry
	{
		internal static Config Config { get; } = OptionsPanelHandler.Main.RegisterModOptions<Config>();

		internal static string GetAssemblyDirectory
		{
			get
			{
				string fullPath = Assembly.GetExecutingAssembly().Location;

				return Path.GetDirectoryName(fullPath);
			}
		}

		internal static void LogFatal(string message, Exception exception = null)
		{
			LogMessage(message, Level.Fatal, exception);
		}

		internal static void LogError(string message, Exception exception = null)
		{
			LogMessage(message, Level.Error, exception);
		}

		internal static void LogWarning(string message, Exception exception = null)
		{
			LogMessage(message, Level.Warn, exception);
		}

		internal static void LogInfo(string message)
		{
			LogMessage(message, Level.Info);
		}

		internal static void LogDebug(string message, bool showOnScreen = false)
		{
			LogMessage(message, Level.Debug, null, showOnScreen);
		}

		internal static void DisplayMenuInfo(string message)
		{
			DisplayMenuMessage(message, Level.Info, "green");
		}

		internal static void DisplayMenuWarn(string message)
		{
			DisplayMenuMessage(message, Level.Warn, "orange");
		}

		internal static void DisplayMenuMessage(string message, Level level = Level.Info, string color = "red")
		{
			LogInfo("Displaying message on main menu:");
			message = string.Format(
					"{0} {1}",
					(level == Level.Info) ? string.Empty : level.ToString() + ": ",
					message);
			QModServices.Main.AddCriticalMessage(message, 25, color, true);
		}

		internal static void DisplayMessage(string message, Level level = Level.Info)
		{
			LogInfo("Displaying message in-game:");
			Logger.Log(
				Level.Info,
				string.Format(
					"[Autosave{0}] {1}",
					(level == Level.Info) ? string.Empty : ":" + level.ToString(),
					message),
				null,
				true);
		}

		private static void LogMessage(string message, Level level = Level.Info, System.Exception exception = null, bool showOnScreen = false)
		{
			// Start by logging to file
			Logger.Log(
				level,
				message,
				exception,
				false);

			// ... and display on screen if we are debugging
			if(showOnScreen && Logger.DebugLogsEnabled)
			{
				DisplayMessage(message, level);
			}
		}

		static void LoadConfig()
		{
			// Check for and handle legacy settings.json
			if(File.Exists(Path.Combine(Entry.GetAssemblyDirectory, "settings.json")))
			{
				Config.MigrateAndLoadLegacySettings();
			}

			else
			{
				Config.Load();
			}

			Config.ValidateAndFix(Config);
			Config.Save();

			if(!Config.AutoSavePermaDeath)
			{
				DisplayMenuWarn("Will not save games with permanent death, toggle this in the options menu.");
			}
		}

		[QModPatch]
		public static void Initialize()
		{
			LoadConfig();

			var harmony = new Harmony("io.github.bisa.autosave");
			#if DEBUG
			harmony.PatchAll(typeof(DebuggingPatches));
			#endif
			harmony.PatchAll(typeof(PlayerPatches));
			harmony.PatchAll(typeof(IngameMenuPatches));
			harmony.PatchAll(typeof(SubRootPatches));
			harmony.PatchAll(typeof(LanguagePatches));
		}
	}
}
