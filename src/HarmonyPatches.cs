using System;
using System.Reflection;
using HarmonyLib;

namespace Autosave
{
	public static class AutosavePatches
	{
		private static Harmony harmony = null;
#if DEBUG
		private static bool TestPatch_GiveFeedback_Prefix()
		{
			Player.main.GetComponent<AutosaveController>()?.ExecuteAutosave();

			return false;
		}
#endif

		private static void Patch_Player_Awake_Postfix(Player __instance)
		{
			__instance.gameObject.AddComponent<AutosaveController>();
		}

		private static void Patch_Subroot_PlayerEnteredOrExited_Postfix()
		{
			Entry.LogDebug("Player entered or exited sub. Delaying autosave.");

			Player.main.GetComponent<AutosaveController>()?.DelayAutosave();
		}

		private static void Patch_SetCurrentLanguage_Postfix()
		{
			Translation.ReloadLanguage();
		}

		private static bool Patch_SaveGame_Prefix()
        {
            return Player.main.GetComponent<AutosaveController>().ChangeSlotIfOnAutosaveSlot();
        }

		public static bool Initialize()
        {
            if ((harmony = new Harmony("com.github.bisa.autosave")) == null)
            {
                Entry.LogError("Unable to initialize Harmony!");
                return false;
            }
            return true;
        }

		public static void PatchAll()
		{
			Entry.LogDebug("Applying all patches...");
#if DEBUG
			Entry.LogDebug("Patching Menu Feedback to perform autosave for debugging", true);
			harmony.Patch( 
				original: typeof(IngameMenu).GetMethod("GiveFeedback"),
				prefix: new HarmonyMethod(typeof(AutosavePatches),
							nameof(AutosavePatches.TestPatch_GiveFeedback_Prefix)));
#endif

			HarmonyMethod delayAutosave = new HarmonyMethod(typeof(AutosavePatches),
				nameof(AutosavePatches.Patch_Subroot_PlayerEnteredOrExited_Postfix));

			harmony.Patch(
				original: typeof(IngameMenu).GetMethod("SaveGame"),
				prefix: new HarmonyMethod(typeof(AutosavePatches),
							nameof(AutosavePatches.Patch_SaveGame_Prefix)));

			// Autosave injection
			harmony.Patch(
				original: typeof(Player).GetMethod("Awake"),
				postfix: new HarmonyMethod(typeof(AutosavePatches),
							nameof(AutosavePatches.Patch_Player_Awake_Postfix)));

			// Delay autosave if player has entered or exited a base or vehicle
			harmony.Patch(
				original: typeof(SubRoot).GetMethod("OnPlayerEntered"),
				postfix: delayAutosave);

			harmony.Patch(
				original: typeof(SubRoot).GetMethod("OnPlayerExited"),
				postfix: delayAutosave);

			// Reset language cache upon language change
			harmony.Patch(
				original: typeof(Language).GetMethod("SetCurrentLanguage"),
				postfix: new HarmonyMethod(typeof(AutosavePatches),
							nameof(AutosavePatches.Patch_SetCurrentLanguage_Postfix)));

			Entry.LogDebug("Done with patching!");
		}
    }
}
