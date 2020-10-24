using System.Reflection;
using HarmonyLib;
using JetBrains.Annotations;
using Verse;

namespace FishingAutomation
{
    [UsedImplicitly]
    public class FishingAutomationMod : Mod
    {
        public FishingAutomationMod(ModContentPack content) : base(content)
        {
            var harmony = new Harmony("LordKuper.FishingAutomation");
            harmony.PatchAll(Assembly.GetExecutingAssembly());
            if (Prefs.DevMode) { Log.Message("Fishing Automation: Initialized.", true); }
        }
    }
}