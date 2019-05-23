using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

namespace SimpleFoodSelection.Searching
{
    public class FoodSearchResult
    {
        public Thing Thing;
        public ThingDef Def;
        public bool Success;

        public FoodSearchResult() { }

        public FoodSearchResult(FoodSearchItem foodSearchItem)
        {
            Thing = foodSearchItem.Thing;
            Def = foodSearchItem.Def;
            Success = true;
        }
    }
}
