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
