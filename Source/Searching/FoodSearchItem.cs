using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

namespace SmarterFoodSelectionSlim.Searching
{
    public class FoodSearchItem
    {
        public FoodSearchItem(Thing thing)
        {
            Thing = thing;
            FoodCategory = FoodCategoryCache.GetFor(thing);
        }

        public FoodSearchItem(Thing thing, IntVec3 searchingFrom) : this(thing)
        {
            Position = GetThingPosition(thing);
            Distance = (searchingFrom - thing.Position).LengthManhattan;
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
                }
                return def;
            }
        }

        /// <summary>
        /// Whether the item is ever a valid food, regardless of eater or getter or other circumstances
        /// </summary>
        public bool IsIngestibleNow()
        {
            if (FoodCategory == FoodCategory.Null
                || FoodCategory == FoodCategory.Ignore)
            {
                // Excluded by categorization algorithm
                return false;
            }

            if (Thing is Building_NutrientPasteDispenser)
            {
                // Need to do further checks which may not be necessary if better food is available
                return true;
            }

            return Thing.IngestibleNow;
        }

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
