using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

namespace SimpleFoodSelection.Searching
{
    /// <summary>
    /// Encapsulates the order in which items should be searched (for example: inventory, then home area, then other) within a Profile food tier
    /// </summary>
    /// <remarks>
    /// By not generating groups until required, we avoid having to sort unnecessary items if an earlier group has a match.
    /// By caching after the first iteration, we avoid having to re-generate each time.
    /// </remarks>
    public class FoodSearchGroups : IEnumerable<IList<FoodSearchItem>>
    {
        public FoodSearchGroups(Pawn getter)
        {
            this.getter = getter;
        }

        private readonly Pawn getter;

        private readonly List<IList<FoodSearchItem>> groups = new List<IList<FoodSearchItem>>();
        public IEnumerable<IList<FoodSearchItem>> Groups
        {
            get
            {
                if (groups.Any())
                {
                    foreach (var group in groups)
                        yield return group;

                    yield break;
                }

                if (getter.IsPlayerFaction() || getter.IsGuest())
                {
                    foreach (var group in GetColonistGroups())
                    {
                        yield return group;
                        groups.Add(group);
                    }
                    yield break;
                }

                // Else, wild/other
                foreach (var group in GetWildGroups())
                {
                    yield return group;
                    groups.Add(group);
                }
            }
        }

        IEnumerable<IList<FoodSearchItem>> GetWildGroups()
        {
            yield return GetInventoryItems().ToArray();

            yield return GetMapItems().ToArray();
        }
        
        IEnumerable<IList<FoodSearchItem>> GetColonistGroups()
        {
            yield return GetInventoryItems().ToArray();

            var mapFoods = GetFoods(getter.Map.listerThings.AllThings, getter.Position).ToArray();
            var homeArea = getter.Map.areaManager.Home;

            yield return mapFoods.Where(x => homeArea[x.Position]).ToArray();

            yield return mapFoods.Where(x => !homeArea[x.Position]).ToArray();
        }

        IEnumerable<FoodSearchItem> GetInventoryItems()
        {
            var inventoryThings = getter?.inventory?.innerContainer?.InnerListForReading;
            if (inventoryThings == null || !inventoryThings.Any())
                yield break;

            foreach (var food in GetFoods(inventoryThings, IntVec3.Invalid))
                yield return food;
        }

        IEnumerable<FoodSearchItem> GetMapItems() =>
            GetFoods(getter.Map.listerThings.AllThings, getter.Position);

        /// <summary>
        /// Filters and categorizes all food items from the given list, sorting by distance
        /// </summary>
        IEnumerable<FoodSearchItem> GetFoods(IList<Thing> things, IntVec3 startingPosition) =>
            things.Select(x => new FoodSearchItem(x, startingPosition))
                    // Ignore anything non-ingestible to avoid having to sort later
                    .Where(x => x.IsIngestibleNow());

        #region IEnumerable implementation
        public IEnumerator<IList<FoodSearchItem>> GetEnumerator() => Groups.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => Groups.GetEnumerator();
        #endregion
    }
}
