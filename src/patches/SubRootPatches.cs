using HarmonyLib;

namespace Autosave
{
	class SubRootPatches
    {
        [HarmonyPrefix]
		[HarmonyPatch(typeof(SubRoot), "OnPlayerExited")]
		[HarmonyPatch(typeof(SubRoot), "OnPlayerEntered")]
		static void Prefix()
		{
			Player.main.GetComponent<AutosaveController>()?.DelayAutosave();
		}
    }
}