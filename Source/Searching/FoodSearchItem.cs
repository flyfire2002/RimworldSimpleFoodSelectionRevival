using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

namespace SimpleFoodSelection.Searching
{
    public class FoodSearchItem
    {
        private FoodSearchItem(Thing thing)
        {
            Thing = thing;
            FoodCategory = FoodCategoryCache.GetFor(thing);
        }

        public FoodSearchItem(Thing thing, IntVec3 searchingFrom) : this(thing)
        {
            if (searchingFrom == IntVec3.Invalid)
            {
                // If invalid, assume inventory
                Position = IntVec3.Invalid;
                Distance = -1;
            }
            else
            {
                // Otherwise, assume valid map position
                Position = GetThingPosition(thing);
                Distance = (searchingFrom - thing.Position).LengthManhattan;
            }
        }

        public readonly Thing Thing;
        public readonly IntVec3 Position;
        public readonly int Distance;
        public FoodCategory FoodCategory;

        private ThingDef def;
        public ThingDef Def
        {
            get
            {
                if (def == null)
                {
                    def = RimWorld.FoodUtility.GetFinalIngestibleDef(Thing, false);
                    if (def == null)
                    {
                        Mod.LogError("Unable to retrieve def for Thing " + Thing);
                    }
                }
                return def;
            }
        }

        /// <summary>
        /// Whether the item could ever be a valid food, regardless of eater or getter or other circumstances
        /// </summary>
        public bool IsIngestibleNow
        {
            get
            {
                if (FoodCategory == FoodCategory.Null || FoodCategory == FoodCategory.Ignore)
                {
                    // Excluded by categorization algorithm
                    return false;
                }

                if (Thing is Building_NutrientPasteDispenser)
                {
                    // Need to do further checks which may not be necessary if better food is available
                    return true;
                }

                if (Thing is Pawn)
                {
                    // Allow pawns for hunting checks
                    return true;
                }

                // All other foods self-report
                return Thing.IngestibleNow;
            }
        }

        public bool IsInInventory => Position == IntVec3.Invalid;

        /// <summary>
        /// What cell to use as a thing's position, for both distance and reachability calculations
        /// </summary>
        private IntVec3 GetThingPosition(Thing thing)
        {
            if (thing is Building_NutrientPasteDispenser)
            {
                return Thing.InteractionCell;
            }

            return Thing.Position;
        }

        public override string ToString() => $"{Thing} ({Def}|{FoodCategory})";
    }
}
