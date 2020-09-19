
using HarmonyLib;

namespace Autosave
{
	class DebuggingPatches
    {
		[HarmonyPrefix]
		[HarmonyPatch(typeof(IngameMenu), "GiveFeedback")]
		static bool GiveFeedbackDebugPrefix()
		{
			Player.main.GetComponent<AutosaveController>()?.ExecuteAutosave();
			return false;
		}
	}
}