using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

namespace SmarterFoodSelectionSlim.Searching
{
    public class FoodSearch
    {
        const int NearbySearchRadius = 50;
        const string TraceDelimiter = "|";

        private static readonly ThoughtDef[] DesperateOnlyThoughts = new[]
        {
            ThoughtDefOf.AteHumanlikeMeatAsIngredient,
            ThoughtDefOf.AteHumanlikeMeatDirect,
            ThoughtDefOf.AteRottenFood,
        };

        public FoodSearch(FoodSearchParameters parameters)
        {
            this.parameters = parameters;
        }

        public FoodSearch(FoodSearchParameters parameters, StringBuilder traceOutput) : this(parameters)
        {
            this.traceOutput = traceOutput;
        }

        private readonly StringBuilder traceOutput = new StringBuilder();

        private readonly FoodSearchParameters parameters;

        /// <summary>
        /// Finds the best, closest food item
        /// </summary>
        public FoodSearchResult Find()
        {
            try
            {
                var profile = Profile.For(parameters.Eater);
                if (profile == null)
                {
                    traceOutput?.AppendLine($"No profile for {parameters.Eater}");
                    return new FoodSearchResult { Success = false };
                }
                traceOutput?.AppendLine($"Using profile {profile.Name} for {parameters.Eater}");

                IList<FoodSearchItem> inventoryFoods = new FoodSearchItem[0];
                // Check inventory before doing anything else
                if (parameters.CanUseInventory && parameters.Getter.CanManipulate())
                {
                    var inventoryThings = parameters.Getter?.inventory?.innerContainer?.InnerListForReading;
                    if (inventoryThings != null && inventoryThings.Any())
                    {
                        inventoryFoods = GetFoods(inventoryThings, IntVec3.Zero);

                        traceOutput?.AppendLine($"Found {inventoryThings.Count} foods in {parameters.Getter}'s inventory: {string.Join(TraceDelimiter, inventoryFoods.Select(x => x.Thing.Label).ToArray())}");

                        var inventoryResult = SearchFoods(inventoryFoods, profile.Good);
                        if (inventoryResult != null)
                            return new FoodSearchResult { Success = true, Thing = inventoryResult };
                    }
                }

                // Check the map for the rest of the search
                var mapThings = parameters.Getter.Map.listerThings.AllThings;
                if (!mapThings.Any())
                    return new FoodSearchResult { Success = true };

                var startingPosition = (parameters.Getter ?? parameters.Eater).Position;

                var foods = GetFoods(mapThings, startingPosition);

                traceOutput?.AppendLine($"Found {foods.Count} foods on map: {string.Join(TraceDelimiter, foods.Take(50).Select(x => x.Thing.Label).ToArray())}");

                // Search nearby first to maximize performance, assume that most searches will succeed here
                // As a side effect, pawns will prefer a simple meal nearby over a lavish meal on the other side of the map
                var nearbyFoods = foods
                    .Where(x => x.Distance <= NearbySearchRadius)
                    .ToArray();
                var nearbyResult = SearchFoods(nearbyFoods, profile.Good);
                if (nearbyResult != null)
                    return new FoodSearchResult { Success = true, Thing = nearbyResult };

                // If nothing nearby, expand search radius to entire map
                var farawayFoods = foods
                    .Where(x => x.Distance > NearbySearchRadius)
                    .ToArray();
                var farawayResult = SearchFoods(farawayFoods, profile.Good);
                if (farawayResult != null)
                    return new FoodSearchResult { Success = true, Thing = farawayResult };

                // Only resort to bad foods if necessary
                if (!ResortToBad(parameters.Eater))
                {
                    traceOutput?.AppendLine($"Not resorting to Bad foods");
                    return new FoodSearchResult { Success = true };
                }
                traceOutput?.AppendLine($"Looking for Bad foods...");

                var badInventoryResult = SearchFoods(inventoryFoods, profile.Bad);
                if (badInventoryResult != null)
                    return new FoodSearchResult { Success = true, Thing = badInventoryResult };

                var badMapResult = SearchFoods(foods, profile.Bad);
                if (badMapResult != null)
                    return new FoodSearchResult { Success = true, Thing = badMapResult };

                // Only resort to desperate foods as a last resort
                if (!ResortToDesperate(parameters.Eater))
                {
                    traceOutput?.AppendLine($"Not resorting to Desperate foods");
                    return new FoodSearchResult { Success = true };
                }
                traceOutput?.AppendLine($"Looking for Desperate foods...");
                parameters.Desperate = true;

                // TODO: reconsider previous options rejected because of desperate thoughts
                var desperateInventoryResult = SearchFoods(inventoryFoods, profile.Desperate);
                if (desperateInventoryResult != null)
                    return new FoodSearchResult { Success = true, Thing = desperateInventoryResult };

                var desperateMapResult = SearchFoods(foods, profile.Desperate);
                if (desperateMapResult != null)
                    return new FoodSearchResult { Success = true, Thing = desperateMapResult };

                // Don't fall back to vanilla in case certain food types were excluded intentionally
                return new FoodSearchResult { Success = true };
            }
            catch (Exception ex)
            {
                Mod.LogError(ex.ToString() + Environment.NewLine + ex.StackTrace);
                return new FoodSearchResult { Success = false };
            }
        }

        /// <summary>
        /// The minimal definition of what things ever count as food
        /// </summary>
        /// <remarks>
        /// If an incorrect item is passed to the Ingest job, it will get caught in a failure loop. These should never be considered.
        /// </remarks>
        public bool IsFood(Thing thing) =>
            thing.def.IsNutritionGivingIngestible
            && thing.IngestibleNow
            && FoodUtility.WillEat(parameters.Eater, thing, parameters.Getter != parameters.Eater ? parameters.Getter : null);

        public static bool IsValidFoodCategory(ThingDef def) =>
            IsValidFoodCategory(def.DetermineFoodCategory());
        public static bool IsValidFoodCategory(FoodCategory foodCategory) =>
            foodCategory != FoodCategory.Ignore && foodCategory != FoodCategory.Null;

        public static bool ResortToBad(Pawn eater) => eater.needs.food.CurCategory >= HungerCategory.UrgentlyHungry;

        public static bool ResortToDesperate(Pawn eater) => eater.needs.food.CurCategory >= HungerCategory.Starving;

        /// <summary>
        /// Filters and categorizes all food items from the given list, sorting by distance
        /// </summary>
        private IList<FoodSearchItem> GetFoods(IList<Thing> things, IntVec3 startingPosition)
        {
#if DEBUG
            var getFoodsStart = DateTime.Now;
#endif
            var result = things
                    // Do early filtering for obvious mismatches
                    .Where(IsFood)
                    .Select(x => new FoodSearchItem(x, startingPosition))
                    // Ignore anything not recognized as food by the categorization algorithm
                    .Where(x => IsValidFoodCategory(x.FoodCategory))
                    .OrderBy(x => x.Distance)
                    .ToArray();
#if DEBUG
            var getFoodsDuration = (DateTime.Now - getFoodsStart).TotalMilliseconds;
            traceOutput.AppendLine($"GetFoods filter took {getFoodsDuration}ms for {result.Length} items at {getFoodsDuration/result.Length}ms/item");
#endif
            return result;
        }

        /// <summary>
        /// Iterates through the provided categories and returns the first valid food
        /// </summary>
        private Thing SearchFoods(IList<FoodSearchItem> foods, IList<IList<FoodCategory>> categories)
        {
#if DEBUG
            var searchStartTime = DateTime.Now;
#endif
            if (!foods.Any())
                return null;

            // Iterate through categories in order of preference
            foreach (var group in categories)
            {
                // Iterate through matched foods in order of distance
                foreach (var item in foods.Where(x => group.Contains(x.FoodCategory)))
                {
                    // Find the first valid result
                    if (Validate(item.Thing))
                    {
#if DEBUG
                        var searchDuration = (DateTime.Now - searchStartTime).TotalMilliseconds;
                        traceOutput?.AppendLine($"Selecting food item {item.Thing} from category {item.FoodCategory} - search took {searchDuration}ms");
#endif
                        return item.Thing;
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Validates that the given food thing is eligible for eating in this situation
        /// </summary>
        /// <remarks>
        /// Performing validation checks as late as possible maximizes optimistic performance
        /// </remarks>
        private bool Validate(Thing thing)
        {
            if (!parameters.AllowForbidden && thing.IsForbidden(parameters.Getter))
            {
                traceOutput?.AppendLine($"Rejecting {thing} because: is forbidden to {parameters.Getter}");
                return false;
            }

            // Special plants logic
            if (thing.def.plant != null)
            {
                if (!parameters.AllowPlant)
                {
                    traceOutput?.AppendLine($"Rejecting {thing} because: is plant");
                    return false;
                }

                // TODO: harvestable plants
                //if (parameters.AllowHarvest && thing.def.plant != null && thing is Plant plant && plant.HarvestableNow)
                //{
                //    var harvestedThingDef = thing.def.plant.harvestedThingDef;
                //}

                if (parameters.Getter != parameters.Eater)
                {
                    traceOutput?.AppendLine($"Rejecting {thing} because: should not carry plants");
                    return false;
                }
            } // ^ plants logic

            // TODO: nutrient paste dispensers
            //var nutrientPasteDispenser = thing as Building_NutrientPasteDispenser

            // Only care about preferences if not desperate
            if (!parameters.Desperate & !parameters.Eater.AnimalOrWildMan())
            {
                if (thing.def.ingestible.preferability > parameters.MaxPref)
                {
                    traceOutput?.AppendLine($"Rejecting {thing} because: preferability {thing.def.ingestible.preferability} exceeds requested maximum {parameters.MaxPref}");
                    return false;
                }

                if (!parameters.AllowCorpse && (thing is Corpse))
                {
                    traceOutput?.AppendLine($"Rejecting {thing} because: is corpse");
                    return false;
                }

                if (!parameters.AllowDrug && thing.def.IsDrug)
                {
                    traceOutput?.AppendLine($"Rejecting {thing} because: is drug");
                    return false;
                }

                if (!parameters.AllowSociallyImproper && !thing.IsSociallyProper(parameters.Eater))
                {
                    traceOutput?.AppendLine($"Rejecting {thing} because: is not socially proper");
                    return false;
                }

                var thoughtsFromConsuming = FoodUtility.ThoughtsFromIngesting(parameters.Eater, thing, thing.def);
                var desperateThoughtFromConsuming = thoughtsFromConsuming.FirstOrDefault(DesperateOnlyThoughts.Contains);
                if (desperateThoughtFromConsuming != null)
                {
                    traceOutput?.AppendLine($"Rejecting {thing} because: would cause desperate thought {desperateThoughtFromConsuming}");
                    return false;
                }
            } // ^ if not desperate or animalistic

            // Potentially expensive path canculation last
            if (!parameters.Getter.CanReach(new LocalTargetInfo(thing.Position), Verse.AI.PathEndMode.Touch, Danger.Unspecified, parameters.Desperate))
            {
                traceOutput?.AppendLine($"Rejecting {thing} because: {parameters.Getter} cannot reach");
                return false;
            }

            return true;
        }
    }
}
