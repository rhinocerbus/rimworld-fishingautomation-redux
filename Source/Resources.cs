using UnityEngine;
using VCE_Fishing;
using Verse;

namespace FishingAutomation
{
    internal static class Resources
    {
        internal static class Strings
        {
            internal static readonly string All = "FM.All".Translate();
            internal static readonly string AllowedFish = "FishingAutomation.AllowedFish".Translate();
            internal static readonly string Delete = "FM.Delete".Translate();
            internal static readonly string Fishing = "FishingAutomation.Fishing".Translate();
            internal static readonly string Manage = "FM.Manage".Translate();
            internal static readonly string Multiple = "multiple".Translate();
            internal static TaggedString NewFishingJob = "FishingAutomation.NewFishingJob".Translate();

            internal static readonly string
                NewFishingJobTooltip = "FishingAutomation.NewFishingJob.Tooltip".Translate();

            internal static readonly string SyncFilterAndAllowed = "FishingAutomation.SyncFilterAndAllowed".Translate();

            internal static readonly string SyncFilterAndAllowedTooltip =
                "FishingAutomation.SyncFilterAndAllowed.Tooltip".Translate();

            internal static readonly string Threshold = "FM.Threshold".Translate();

            internal static TaggedString FishSizeCategory(FishSizeCategory fishSizeCategory)
            {
                return $"FishingAutomation.FishSizeCategory.{fishSizeCategory}".Translate();
            }

            internal static TaggedString FishSizeCategoryTooltip(FishSizeCategory fishSizeCategory)
            {
                return $"FishingAutomation.FishSizeCategory.{fishSizeCategory}.Tooltip".Translate();
            }

            internal static TaggedString TargetCount(int currentCount, int targetCount)
            {
                return "FishingAutomation.TargetCount".Translate(currentCount, targetCount);
            }

            internal static TaggedString TargetCountTooltip(int currentCount, int targetCount)
            {
                return "FishingAutomation.TargetCount.Tooltip".Translate(currentCount, targetCount);
            }
        }

        [StaticConstructorOnStartup]
        internal static class Textures
        {
            public static Texture2D IconFish = ContentFinder<Texture2D>.Get("fishing-automation-tab-icon");
        }
    }
}