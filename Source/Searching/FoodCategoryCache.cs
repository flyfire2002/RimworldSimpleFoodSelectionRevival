using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

namespace SmarterFoodSelectionSlim.Searching
{
    public static class FoodCategoryCache
    {
        public static FoodCategory GetFor(Thing thing) => GetFor(thing.def);

        public static FoodCategory GetFor(ThingDef def)
        {
            if (cache.ContainsKey(def))
                return cache[def];

            var category = def.DetermineFoodCategory();
            cache.Add(def, category);
            return category;
        }

        private static Dictionary<ThingDef, FoodCategory> cache = new Dictionary<ThingDef, FoodCategory>();
    }
}
