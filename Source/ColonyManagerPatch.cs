using System.Diagnostics.CodeAnalysis;
using FluffyManager;
using HarmonyLib;
using JetBrains.Annotations;
using Verse;

namespace FishingAutomation
{
    [HarmonyPatch(typeof(Manager), MethodType.Constructor, typeof(Map))]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    [UsedImplicitly]
    public static class ColonyManagerPatch
    {
        [UsedImplicitly]
        public static void Postfix(Manager __instance)
        {
            __instance.Tabs.Add(new ManagerTab_Fishing(__instance));
        }
    }
}