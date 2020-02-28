using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

namespace SimpleFoodSelection.Searching
{
    public static class FoodCategoryCache
    {
        public static FoodCategory GetFor(Thing thing)
        {
            if (cache.ContainsKey(thing.def))
                return cache[thing.def];

            var category = thing.DetermineFoodCategory();
            cache.Add(thing.def, category);
            return category;
        }

        private static Dictionary<ThingDef, FoodCategory> cache = new Dictionary<ThingDef, FoodCategory>();
    }
}
