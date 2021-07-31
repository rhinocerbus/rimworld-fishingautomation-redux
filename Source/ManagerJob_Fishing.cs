using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using FluffyManager;
using UnityEngine;
using VCE_Fishing;
using Verse;
using static FluffyManager.Constants;

namespace FishingAutomation
{
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public class ManagerJob_Fishing : ManagerJob
    {
        public Dictionary<FishDef, bool> AllowedFish = new Dictionary<FishDef, bool>();
        public History History;
        private Utilities.SyncDirection Sync = Utilities.SyncDirection.AllowedToFilter;
        public bool SyncFilterAndAllowed = true;
        public new Trigger_Threshold Trigger;

        public ManagerJob_Fishing(Manager manager) : base(manager)
        {
            var thingFilter = new ThingFilter(Notify_ThresholdFilterChanged);
            thingFilter.SetDisallowAll();
            var parentFilter = new ThingFilter();
            parentFilter.SetAllowAll(null);
            Trigger = new Trigger_Threshold(this.manager)
            {
                Op = Trigger_Threshold.Ops.LowerThan,
                MaxUpperThreshold = Trigger_Threshold.DefaultMaxUpperThreshold,
                TargetCount = Trigger_Threshold.DefaultCount,
                ThresholdFilter = thingFilter,
                ParentFilter = parentFilter,
                countAllOnMap = true
            };
            History = new History(new[] {I18n.HistoryStock, I18n.HistoryDesignated}, new[] {Color.white, Color.grey});
            if (Scribe.mode == LoadSaveMode.Inactive) { RefreshAllowedFish(); }
        }

        public override bool Completed => false;
        public override string Label => Resources.Strings.Fishing;

        public override ManagerTab Tab
        {
            get { return manager.Tabs.Find(tab => tab is ManagerTab_Fishing); }
        }

        public override string[] Targets =>
            AllowedFish.Keys.Where(key => AllowedFish[key]).Select(fish => fish.thingDef.LabelCap.Resolve()).ToArray();

        public override WorkTypeDef WorkTypeDef => DefDatabase<WorkTypeDef>.GetNamed("VCEF_Fishing");

        public override void CleanUp() { }

        public override void DrawListEntry(Rect rect, bool overview = true, bool active = true)
        {
            Rect labelRect =
                    new Rect(Margin, Margin, rect.width - (active ? StatusRectWidth + 4 * Margin : 2 * Margin),
                        rect.height - 2 * Margin),
                statusRect = new Rect(labelRect.xMax + Margin, Margin, StatusRectWidth, rect.height - 2 * Margin);
            var subtext = SubLabel(labelRect);
            var text = Label + "\n" + subtext;
            GUI.BeginGroup(rect);
            Widgets_Labels.Label(labelRect, text, subtext, TextAnchor.MiddleLeft, margin: Margin);
            if (active) { this.DrawStatusForListEntry(statusRect, Trigger); }
            GUI.EndGroup();
        }

        public override void DrawOverviewDetails(Rect rect)
        {
            History.DrawPlot(rect, Trigger.TargetCount);
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Deep.Look(ref Trigger, nameof(Trigger), manager);
            Scribe_Collections.Look(ref AllowedFish, nameof(AllowedFish), LookMode.Def, LookMode.Value);
            if (Manager.LoadSaveMode == Manager.Modes.Normal) { Scribe_Deep.Look(ref History, nameof(History)); }
        }

        private void Notify_ThresholdFilterChanged()
        {
            if (!SyncFilterAndAllowed || Sync == Utilities.SyncDirection.AllowedToFilter) { return; }
            foreach (var fish in AllowedFish.Keys)
            {
                AllowedFish[fish] = Trigger.ThresholdFilter.Allows(fish.thingDef);
            }
        }

        public void RefreshAllowedFish()
        {
            var options = DefDatabase<FishDef>.AllDefs.Where(fish => fish.fishSizeCategory != FishSizeCategory.Special)
                .ToList();
            foreach (var fish in AllowedFish.Keys.ToList().Where(fish => !options.Contains(fish)))
            {
                AllowedFish.Remove(fish);
            }
            foreach (var fish in options.Where(fish => !AllowedFish.ContainsKey(fish)))
            {
                AllowedFish.Add(fish, false);
            }
            AllowedFish = AllowedFish.OrderBy(fish => fish.Key.thingDef.LabelCap.RawText)
                .ToDictionary(fish => fish.Key, at => at.Value);
        }

        public void SetFishAllowed(FishDef fish, bool allow)
        {
            if (fish == null) { throw new ArgumentNullException(nameof(fish)); }
            if (Prefs.DevMode)
            {
                Log.Message(
                    $"Fishing Automation: Setting fish '{fish.thingDef.LabelCap}' to {(allow ? "allowed" : "forbidden")}");
            }
            AllowedFish[fish] = allow;
            if (SyncFilterAndAllowed)
            {
                Sync = Utilities.SyncDirection.AllowedToFilter;
                var material = fish.thingDef;
                if (Trigger.ParentFilter.Allows(material)) { Trigger.ThresholdFilter.SetAllow(material, allow); }
            }
        }

        private string SubLabel(Rect rect)
        {
            var subLabel = string.Join(", ", Targets);
            return subLabel.Fits(rect) ? subLabel.Italic() : Resources.Strings.Multiple.Italic();
        }

        public override bool TryDoJob()
        {
            if (Prefs.DevMode)
            {
                Log.Message("Fishing Automation: Executing job...");
                foreach (var fish in AllowedFish)
                {
                    Log.Message(
                        $"Fishing Automation: {fish.Key.thingDef.LabelCap} = {(fish.Value ? "allowed" : "forbidden")}");
                }
                Log.Message($"Fishing Automation: Job targets = {string.Join(", ", Targets)}");
            }
            var zones = Find.CurrentMap.zoneManager.AllZones.Where(zone => zone is Zone_Fishing).OfType<Zone_Fishing>()
                .ToList();
            if (Prefs.DevMode)
            {
                Log.Message(
                    $"Fishing Automation: Discovered fishing zones = {string.Join(", ", zones.Select(zone => zone.label))}");
            }
            foreach (var zone in zones)
            {
                if (Prefs.DevMode)
                {
                    Log.Message($"Fishing Automation: Checking fishing zone '{zone.label}'");
                    Log.Message(
                        $"Fishing Automation: Fish in zone = '{string.Join(", ", zone.fishInThisZone.Select(fish => fish.LabelCap))}'");
                }
                if (AllowedFish.Keys.Where(key => AllowedFish[key]).Select(fish => fish.thingDef)
                    .Intersect(zone.fishInThisZone).Any())
                {
                    if (Prefs.DevMode)
                    {
                        Log.Message($"Fishing Automation: Fishing zone '{zone.label}' is relevant");
                        Log.Message($"Fishing Automation: Fishing in zone '{zone.label}' is allowed = {Trigger.State}");
                    }
                    zone.allowFishing = Trigger.State;
                }
            }
            return true;
        }
    }
}