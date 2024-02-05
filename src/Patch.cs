using HarmonyLib;

namespace LC.Grub4K.YippeeReloaded;

internal static class BugHandlerPatches
{
    [HarmonyPatch(typeof(HoarderBugAI), "Start")]
    [HarmonyPostfix]
    public static void HoarderBugAI_Start_Post(HoarderBugAI __instance)
    {
        AudioReplacer.AddBug(__instance);
    }

    [HarmonyPatch(typeof(StartOfRound), "unloadSceneForAllPlayers")]
    [HarmonyPostfix]
    public static void StartOfRound_unloadSceneForAllPlayers()
    {
        AudioReplacer.RemoveAllBugs();
    }
}
