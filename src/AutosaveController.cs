using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UWE;
using static QModManager.Utility.Logger;

namespace Autosave
{
	/* Some of the following code is based on "Safe Autosave" by berkay2578:
	 * https://www.nexusmods.com/subnautica/mods/94
	 * https://github.com/berkay2578/SubnauticaMods/tree/master/SafeAutosave
	 *
	 * Directory replication code from:
	 * https://stackoverflow.com/questions/58744/copy-the-entire-contents-of-a-directory-in-c-sharp */

	public class AutosaveController : MonoBehaviour
	{
		private const int RetryTicks = 5;

		string savePath = "";

		private const int PriorWarningTicks = 30;

		private const string AutosaveNameFormat = "autosave_slot{0:0000}";

		private readonly List<string> allowedAutosaveNames = new List<string>();

		private string lastUsedAutosaveName;

		private bool isSaving = false;

		private int totalTicks = 0;

		private int nextSaveTriggerTick = 120;

		private string SlotNameFormatted(int slotNumber)
		{
			// Example output: "autosave_slot0003"
			return string.Format(AutosaveNameFormat, slotNumber);
		}

		private string GetSavePath()
		{
			PlatformServices platformServices = PlatformUtils.main.GetServices();
			if (platformServices is PlatformServicesEpic)
			{
				savePath = Path.Combine(
					Application.persistentDataPath,
					"Subnautica",
					"SavedGames");
			}
			else if (platformServices is PlatformServicesSteam)
			{
				savePath = Path.Combine(
					new DirectoryInfo(Application.dataPath).Parent.FullName,
					"SNAppData",
					"SavedGames");
			}
			return savePath;
		}

		private string LastUsedAutosaveFromStorage()
		{
			string savedGamesDir = GetSavePath();

			if (Directory.Exists(savedGamesDir))
			{
				DirectoryInfo[] saveDirectories = new DirectoryInfo(savedGamesDir).GetDirectories("*slot*", SearchOption.TopDirectoryOnly);

				if (saveDirectories.Count() > 0)
				{
					IOrderedEnumerable<DirectoryInfo> saveSlotsByLastModified = saveDirectories.OrderByDescending(d => d.GetFiles("gameinfo.json")[0].LastWriteTime);

					foreach (DirectoryInfo save in saveSlotsByLastModified)
					{
						if (this.allowedAutosaveNames.Contains(save.Name))
						{
							// The most recent save slot used, which matches the maximum save slots setting
							return save.Name;
						}
					}
				}
			}

			return string.Empty;
		}

		private string NextAutosaveSlotName()
		{
			if (!string.IsNullOrEmpty(this.lastUsedAutosaveName))
			{
				for (int i = 0; i < this.allowedAutosaveNames.Count - 1; i++)
				{
					// Returns slot 0 if the latest autosave was the maximum slot allowed (due to .Count -1)
					if (this.allowedAutosaveNames[i] == this.lastUsedAutosaveName)
					{
						return this.allowedAutosaveNames[i + 1];
					}
				}
			}

			return this.SlotNameFormatted(0);
		}

		private bool IsSafePlayerHealth(float minHealthPercent)
		{
			float playerHealthPercent = Player.main.liveMixin.GetHealthFraction() * 100f;

			return playerHealthPercent >= minHealthPercent;
		}

		// Modified IngameMenu.GetAllowSaving
		private bool IsSafeToSave()
		{
			if (IntroVignette.isIntroActive || LaunchRocket.isLaunching)
			{
				Entry.LogDebug($"Did not save, isIntroActive == {IntroVignette.isIntroActive} / isLaunching == {LaunchRocket.isLaunching}");

				return false;
			}

			if (PlayerCinematicController.cinematicModeCount > 0 && Time.time - PlayerCinematicController.cinematicActivityStart <= 30f)
			{
				Entry.LogDebug("Did not save because cinematics are active");

				return false;
			}

			float safeHealthFraction = Entry.GetConfig.MinimumPlayerHealthPercent;

			if (safeHealthFraction > 0f && !this.IsSafePlayerHealth(safeHealthFraction))
			{
				LiveMixin playerLiveMixin = Player.main.liveMixin;
				Entry.LogDebug($"Did not save because player health was {playerLiveMixin.health} / {playerLiveMixin.maxHealth}");

				return false;
			}

			return !SaveLoadManager.main.isSaving;
		}

		private void CopyScreenshotFiles(string originalSlot, string targetSlot)
		{
			string originalScreenshotsDir = Path.Combine(Path.Combine(GetSavePath(), originalSlot), ScreenshotManager.screenshotsFolderName);

			if (Directory.Exists(originalScreenshotsDir))
			{
				string newScreenshotsDir = originalScreenshotsDir.Replace(originalSlot, targetSlot);

				// .CreateDirectory harmlessly terminates if the target already exists
				Directory.CreateDirectory(newScreenshotsDir);

				string[] filesToCopy = Directory.GetFiles(originalScreenshotsDir, "*", SearchOption.TopDirectoryOnly);

				// Copy all the files & replace any files with the same name
				foreach (string screenshot in filesToCopy)
				{
					File.Copy(screenshot, screenshot.Replace(originalSlot, targetSlot), true);
				}
			}
		}

		// Modified IngameMenu.SaveGameAsync
		private IEnumerator AutosaveCoroutine()
		{
			this.isSaving = true;
			bool hardcoreMode = Entry.GetConfig.HardcoreMode;

#if DEBUG
			// Close ingame menu if open, used for testing
			IngameMenu.main.Close();
#endif

			Entry.DisplayMessage("AutosaveStarting".Translate());
			string cachedSaveSlot = string.Empty;

			if(!hardcoreMode)
			{
				if(!Directory.Exists(GetSavePath()))
				{
					Entry.LogFatal(string.Format(
						"Unable to find save directory, the expected '{0}' does not exist.",
						cachedSaveSlot));
					Entry.DisplayMessage("Could not find your SavedGames directory, see log for details!", // TODO: Translate
						Level.Fatal);

					this.isSaving = false;
					throw new FileNotFoundException(savePath);
				}

				cachedSaveSlot = Path.Combine(GetSavePath(), SaveLoadManager.main.GetCurrentSlot());
			}


			Entry.LogDebug($"Cached save slot == {cachedSaveSlot}", true);

			yield return null;

			string autosaveSlot = !hardcoreMode ? this.NextAutosaveSlotName() : string.Empty;

			if (!hardcoreMode)
			{
				SaveLoadManager.main.SetCurrentSlot(autosaveSlot);
			}

			Entry.LogDebug($"Set custom slot as {autosaveSlot}", true);

			yield return null;

			IEnumerator saveGameAsync = (IEnumerator)typeof(IngameMenu).GetMethod("SaveGameAsync", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(IngameMenu.main, null);

			yield return saveGameAsync;

			Entry.LogDebug("Executed _SaveGameAsync");

			if (!hardcoreMode)
			{
				this.CopyScreenshotFiles(cachedSaveSlot, autosaveSlot);

				Entry.LogDebug($"Copied screenshots from {cachedSaveSlot} to {autosaveSlot}", true);

				SaveLoadManager.main.SetCurrentSlot(cachedSaveSlot);
				this.lastUsedAutosaveName = autosaveSlot;
			}

			int autosaveInterval = Entry.GetConfig.SecondsBetweenAutosaves;
			this.nextSaveTriggerTick += autosaveInterval;

			Entry.LogDebug("Updated save slot and trigger tick");

			yield return null;

			Entry.LogDebug("Autosave sequence complete");
			Entry.DisplayMessage("AutosaveEnding".FormatTranslate(autosaveInterval.ToString()));

			this.isSaving = false;

			yield break;
		}

		private void Tick()
		{
			this.totalTicks++;

			if (this.totalTicks % 30 == 0)
			{
				Entry.LogDebug($"totalTicks reached {this.totalTicks} ticks");
			}

			if (this.totalTicks == this.nextSaveTriggerTick - PriorWarningTicks)
			{
				Entry.LogDebug("Warning ticks reached, should display an ErrorMessage.");
				Entry.DisplayMessage("AutosaveWarning".FormatTranslate(PriorWarningTicks.ToString()));
			}

			else if (this.totalTicks >= this.nextSaveTriggerTick && !this.isSaving)
			{
				if (this.IsSafeToSave())
				{
					this.ExecuteAutosave();
				}

				else
				{
					Entry.LogDebug("IsSafeToSave false. Delaying autosave.");

					this.DelayAutosave();
				}
			}
		}

		// Monobehaviour.Awake(), called before Start()
		private void Awake()
		{
			if (Entry.GetConfig == null)
			{
				Entry.LogWarning("Main config missing. Trying to load config.");

				ConfigHandler.LoadConfig();
			}

			this.nextSaveTriggerTick = Entry.GetConfig.SecondsBetweenAutosaves;

			for (int i = 0; i < Entry.GetConfig.MaxSaveFiles; i++)
			{
				this.allowedAutosaveNames.Add(this.SlotNameFormatted(i));
			}

			this.lastUsedAutosaveName = !Entry.GetConfig.HardcoreMode ? this.LastUsedAutosaveFromStorage() : string.Empty;

			Entry.LogDebug($"SecondsBetweenAutosaves == {Entry.GetConfig.SecondsBetweenAutosaves}");
			Entry.LogDebug($"MaxSaveFiles == {Entry.GetConfig.MaxSaveFiles}");
			Entry.LogDebug($"SafePlayerHealthFraction == {Entry.GetConfig.MinimumPlayerHealthPercent}");
			Entry.LogDebug($"lastUsedAutosaveName == {this.lastUsedAutosaveName}");
			Entry.LogDebug($"HardcoreMode == {Entry.GetConfig.HardcoreMode}");

		}

		// Monobehaviour.Start
		private void Start()
		{
			this.InvokeRepeating(nameof(AutosaveController.Tick), 1f, 1f);
		}

		public void DelayAutosave()
		{
			this.nextSaveTriggerTick += RetryTicks;
		}

		public void ExecuteAutosave()
		{
			if (!this.isSaving)
			{
				try
				{
					CoroutineHost.StartCoroutine(this.AutosaveCoroutine());
				}

				catch (Exception ex)
				{
					Entry.LogError("Failed to execute save coroutine. Something went wrong.", ex);
					Entry.DisplayMessage("Failed to execute save coroutine, see log for details!", Level.Error); // TODO: Translate

					// TODO: Handle the exception?
				}
			}

			else
			{
				Entry.DisplayMessage("AutosaveInProgress".Translate());
			}
		}
	}
}
