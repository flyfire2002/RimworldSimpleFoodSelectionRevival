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

        public FoodSearchItem(Thing thing, IntVec3 position) : this(thing)
        {
            Position = position;
            Distance = (position - thing.Position).LengthManhattan;
        }

        public readonly Thing Thing;
        public readonly IntVec3 Position;
        public readonly int Distance;
        public FoodCategory FoodCategory;
    }
}
