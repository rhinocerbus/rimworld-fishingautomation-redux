using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using FluffyManager;
using UnityEngine;
using VCE_Fishing;
using Verse;
using static FluffyManager.Constants;

namespace FishingAutomation
{
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public class ManagerTab_Fishing : ManagerTab
    {
        private List<ManagerJob_Fishing> _jobs;
        private float _leftRowHeight = 9999f;
        private Vector2 _scrollPosition = Vector2.zero;
        private ManagerJob_Fishing _selected;

        public ManagerTab_Fishing(Manager manager) : base(manager)
        {
            _selected = new ManagerJob_Fishing(manager);
        }

        public override Texture2D Icon => Resources.Textures.IconFish;
        public override IconAreas IconArea => IconAreas.Middle;
        public override string Label => Resources.Strings.Fishing;

        public override ManagerJob Selected
        {
            get => _selected;
            set => _selected = (ManagerJob_Fishing) value;
        }

        private void DoContent(Rect rect)
        {
            Widgets.DrawMenuSection(rect);
            var optionsColumnRect = new Rect(
                rect.xMin, rect.yMin, rect.width * 3 / 5f, rect.height - Margin - ButtonSize.y);
            var fishColumnRect = new Rect(optionsColumnRect.xMax, rect.yMin, rect.width * 2 / 5f,
                rect.height - Margin - ButtonSize.y);
            var buttonRect = new Rect(rect.xMax - ButtonSize.x, rect.yMax - ButtonSize.y, ButtonSize.x - Margin,
                ButtonSize.y - Margin);
            Widgets_Section.BeginSectionColumn(optionsColumnRect, "Fishing.Options", out var position, out var width);
            Widgets_Section.Section(ref position, width, DrawThreshold, Resources.Strings.Threshold);
            Widgets_Section.EndSectionColumn("Fishing.Options", position);
            Widgets_Section.BeginSectionColumn(fishColumnRect, "Fishing.AllowedFish", out position, out width);
            Widgets_Section.Section(ref position, width, DrawFishShortcuts, Resources.Strings.AllowedFish);
            Widgets_Section.Section(ref position, width, DrawFishList);
            Widgets_Section.EndSectionColumn("Fishing.AllowedFish", position);
            if (!_selected.Managed)
            {
                if (Widgets.ButtonText(buttonRect, Resources.Strings.Manage))
                {
                    _selected.Managed = true;
                    manager.JobStack.Add(_selected);
                    Refresh();
                }
            }
            else
            {
                if (Widgets.ButtonText(buttonRect, Resources.Strings.Delete))
                {
                    manager.JobStack.Delete(_selected);
                    _selected = null;
                    Refresh();
                }
            }
        }

        private void DoLeftRow(Rect rect)
        {
            Widgets.DrawMenuSection(rect);
            var height = _leftRowHeight;
            var scrollView = new Rect(0f, 0f, rect.width, height);
            if (height > rect.height) { scrollView.width -= ScrollbarWidth; }
            Widgets.BeginScrollView(rect, ref _scrollPosition, scrollView);
            var scrollContent = scrollView;
            GUI.BeginGroup(scrollContent);
            var cur = Vector2.zero;
            var i = 0;
            foreach (var job in _jobs)
            {
                var row = new Rect(0f, cur.y, scrollContent.width, LargeListEntryHeight);
                Widgets.DrawHighlightIfMouseover(row);
                if (_selected == job) { Widgets.DrawHighlightSelected(row); }
                if (i++ % 2 == 1) { Widgets.DrawAltRect(row); }
                var jobRect = row;
                jobRect.width -= 50f;
                job.DrawListEntry(jobRect, false);
                if (Widgets.ButtonInvisible(jobRect)) { _selected = job; }
                cur.y += LargeListEntryHeight;
            }
            var newRect = new Rect(0f, cur.y, scrollContent.width, LargeListEntryHeight);
            Widgets.DrawHighlightIfMouseover(newRect);
            if (i % 2 == 1) { Widgets.DrawAltRect(newRect); }
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(newRect, $"<{Resources.Strings.NewFishingJob.Resolve()}>");
            Text.Anchor = TextAnchor.UpperLeft;
            if (Widgets.ButtonInvisible(newRect)) { Selected = new ManagerJob_Fishing(manager); }
            TooltipHandler.TipRegion(newRect, Resources.Strings.NewFishingJobTooltip);
            cur.y += LargeListEntryHeight;
            _leftRowHeight = cur.y;
            GUI.EndGroup();
            Widgets.EndScrollView();
        }

        public override void DoWindowContents(Rect canvas)
        {
            var leftRow = new Rect(0f, 0f, DefaultLeftRowSize, canvas.height);
            var contentCanvas = new Rect(leftRow.xMax + Margin, 0f, canvas.width - leftRow.width - Margin,
                canvas.height);
            DoLeftRow(leftRow);
            if (Selected != null) { DoContent(contentCanvas); }
        }

        private float DrawFishList(Vector2 pos, float width)
        {
            var start = pos;
            var rowRect = new Rect(pos.x, pos.y, width, ListEntryHeight);
            var allowedFish = _selected.AllowedFish;
            var fishDefs = new List<FishDef>(allowedFish.Keys);
            foreach (var def in fishDefs)
            {
                Utilities.DrawToggle(rowRect, def.thingDef.LabelCap,
                    new TipSignal(() => GetFishTooltip(def), def.GetHashCode()), _selected.AllowedFish[def],
                    () => _selected.AllowedFish[def] = !_selected.AllowedFish[def]);
                rowRect.y += ListEntryHeight;
            }
            return rowRect.yMin - start.y;
        }

        private float DrawFishShortcuts(Vector2 pos, float width)
        {
            var start = pos;
            var rowRect = new Rect(pos.x, pos.y, width, ListEntryHeight);
            var allowed = _selected.AllowedFish;
            var fishDefs = new List<FishDef>(allowed.Keys);
            Utilities.DrawToggle(rowRect, Resources.Strings.All.Italic(), string.Empty, allowed.Values.All(p => p),
                allowed.Values.All(p => !p), () => fishDefs.ForEach(p => _selected.SetFishAllowed(p, true)),
                () => fishDefs.ForEach(p => _selected.SetFishAllowed(p, false)));
            foreach (var fishSizeCategory in Enum.GetValues(typeof(FishSizeCategory)).OfType<FishSizeCategory>())
            {
                if (fishSizeCategory == FishSizeCategory.Special) { continue; }
                rowRect.y += ListEntryHeight;
                var fishBySize = fishDefs.Where(fishDef => fishDef.fishSizeCategory == fishSizeCategory).ToList();
                Utilities.DrawToggle(rowRect, Resources.Strings.FishSizeCategory(fishSizeCategory).Italic(),
                    Resources.Strings.FishSizeCategoryTooltip(fishSizeCategory), fishBySize.All(p => allowed[p]),
                    fishBySize.All(p => !allowed[p]), () => fishBySize.ForEach(p => _selected.SetFishAllowed(p, true)),
                    () => fishBySize.ForEach(p => _selected.SetFishAllowed(p, false)));
            }
            return rowRect.yMax - start.y;
        }

        private float DrawThreshold(Vector2 pos, float width)
        {
            var start = pos;
            if (_selected == null) { throw new NullReferenceException("Selected job is null"); }
            if (_selected.Trigger == null) { throw new NullReferenceException("Selected job's trigger is null"); }
            var currentCount = _selected.Trigger.CurrentCount;
            var targetCount = _selected.Trigger.TargetCount;
            _selected.Trigger.DrawTriggerConfig(ref pos, width, ListEntryHeight,
                Resources.Strings.TargetCount(currentCount, targetCount),
                Resources.Strings.TargetCountTooltip(currentCount, targetCount));
            Utilities.DrawToggle(ref pos, width, Resources.Strings.SyncFilterAndAllowed,
                Resources.Strings.SyncFilterAndAllowedTooltip, ref _selected.SyncFilterAndAllowed);
            return pos.y - start.y;
        }

        private string GetFishTooltip(FishDef fishDef)
        {
            var sb = new StringBuilder();
            if (fishDef.thingDef != null)
            {
                sb.Append(fishDef.thingDef.description);
                if (fishDef.baseFishingYield > 0f)
                {
                    sb.Append("\n\n");
                    sb.Append(I18n.YieldOne(fishDef.baseFishingYield, fishDef.thingDef));
                }
            }
            return sb.ToString();
        }

        public override void PreOpen()
        {
            Refresh();
        }

        private void Refresh()
        {
            if (Prefs.DevMode) { Log.Message("Fishing Automation: Refreshing fishing tab...", true); }
            _jobs = manager.JobStack.FullStack<ManagerJob_Fishing>();
            foreach (var job in _jobs) { job.RefreshAllowedFish(); }
            _selected?.RefreshAllowedFish();
        }
    }
}