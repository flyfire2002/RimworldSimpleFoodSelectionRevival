using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using Verse.AI;

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

                var mapFoods = GetFoods(mapThings, startingPosition);

                traceOutput?.AppendLine($"Found {mapFoods.Count} foods on map: {string.Join(TraceDelimiter, mapFoods.Take(50).Select(x => x.Thing.Label).ToArray())}");

                // Search nearby first to maximize performance, assume that most searches will succeed here
                // As a side effect, pawns will prefer a simple meal nearby over a lavish meal on the other side of the map
                var nearbyFoods = mapFoods
                    .Where(x => x.Distance <= NearbySearchRadius)
                    .ToArray();
                var nearbyResult = SearchFoods(nearbyFoods, profile.Good);
                if (nearbyResult != null)
                    return new FoodSearchResult { Success = true, Thing = nearbyResult };

                // If nothing nearby, expand search radius to entire map
                var farawayFoods = mapFoods
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

                var badMapResult = SearchFoods(mapFoods, profile.Bad);
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

                var desperateMapResult = SearchFoods(mapFoods, profile.Desperate);
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

        public static bool IsValidFoodCategory(FoodCategory foodCategory) =>
            foodCategory != FoodCategory.Ignore && foodCategory != FoodCategory.Null;

        public static bool ResortToBad(Pawn eater) => eater.needs.food.CurCategory >= HungerCategory.UrgentlyHungry;

        public static bool ResortToDesperate(Pawn eater) => eater.needs.food.CurCategory >= HungerCategory.Starving;

        /// <summary>
        /// Filters and categorizes all food items from the given list, sorting by distance
        /// </summary>
        private IList<FoodSearchItem> GetFoods(IList<Thing> things, IntVec3 startingPosition)
        {
            var getFoodsStart = DateTime.Now;

            var result = things
                    .Select(x => new FoodSearchItem(x, startingPosition))
                    // Ignore anything not recognized as food by the categorization algorithm
                    .Where(x => IsValidFoodCategory(x.FoodCategory))
                    .OrderBy(x => x.Distance)
                    .ToArray();

            var getFoodsDuration = (DateTime.Now - getFoodsStart).TotalMilliseconds;
            traceOutput?.AppendLine($"GetFoods filter took {getFoodsDuration}ms for {result.Length} items at {getFoodsDuration/result.Length}ms/item");

            return result;
        }

        /// <summary>
        /// Iterates through the provided categories and returns the first valid food
        /// </summary>
        private Thing SearchFoods(IList<FoodSearchItem> foods, IList<IList<FoodCategory>> categories)
        {
            var searchStartTime = DateTime.Now;

            if (!foods.Any())
                return null;

            // Iterate through categories in order of preference
            foreach (var group in categories)
            {
                // Iterate through matched foods in order of distance
                foreach (var item in foods.Where(x => group.Contains(x.FoodCategory)))
                {
                    // Find the first valid result
                    if (Validate(item))
                    {
                        var searchDuration = (DateTime.Now - searchStartTime).TotalMilliseconds;
                        traceOutput?.AppendLine($"Selecting food item {item} from category {item.FoodCategory} - search took {searchDuration}ms");

                        return item.Thing;
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Validates that the given item is eligible for eating in this situation
        /// </summary>
        /// <remarks>
        /// Performing validation checks as late as possible maximizes optimistic performance
        /// </remarks>
        private bool Validate(FoodSearchItem item)
        {
            if (!parameters.AllowForbidden && item.Thing.IsForbidden(parameters.Getter))
            {
                traceOutput?.AppendLine($"Rejecting {item} because: is forbidden to {parameters.Getter}");
                return false;
            }

            if (!parameters.Eater.WillEat(item.Def, parameters.Getter))
            {
                traceOutput?.AppendLine($"Rejecting item {item} because: {parameters.Getter} will not eat def {item.Def}");
                return false;
            }

            // Special plants logic
            if (item.Def.plant != null)
            {
                if (!parameters.AllowPlant)
                {
                    traceOutput?.AppendLine($"Rejecting {item} because: is plant");
                    return false;
                }

                // TODO: harvestable plants
                //if (parameters.AllowHarvest && thing.def.plant != null && thing is Plant plant && plant.HarvestableNow)
                //{
                //    var harvestedThingDef = thing.def.plant.harvestedThingDef;
                //}

                if (parameters.Getter != parameters.Eater)
                {
                    traceOutput?.AppendLine($"Rejecting {item} because: pawns should not carry plants");
                    return false;
                }
            } // ^ plants logic

            if (!ValidateDispenser(item))
                return false;

            if (!ValidateFoodPreferences(item))
                return false;

            // Potentially expensive path canculation last
            if (!parameters.Getter.CanReach(new LocalTargetInfo(item.Position), Verse.AI.PathEndMode.OnCell, Danger.Unspecified, parameters.Desperate))
            {
                traceOutput?.AppendLine($"Rejecting {item} because: {parameters.Getter} cannot reach");
                return false;
            }

            return true;
        }

        /// <summary>
        /// If the provided thing is a food dispenser, performs validation on the dispenser and the dispensed food
        /// </summary>
        private bool ValidateDispenser(FoodSearchItem item)
        {
            var nutrientPasteDispenser = item.Thing as Building_NutrientPasteDispenser;
            if (nutrientPasteDispenser == null)
                return true;


            Mod.LogMessage($"Thing {item.Thing} ({item.Def}) is nutrient paste dispenser dispensing {nutrientPasteDispenser.DispensableDef}");
            // Vanilla disallow logic:
            // !allowDispenserFull
            // || !getterCanManipulate 
            // || (ThingDefOf.MealNutrientPaste.ingestible.preferability < minPref 
            //     || ThingDefOf.MealNutrientPaste.ingestible.preferability > maxPref) 
            // || (!eater.WillEat(ThingDefOf.MealNutrientPaste, getter) 
            //     || t.Faction != getter.Faction 
            //     && t.Faction != getter.HostFaction) 
            // || (!allowForbidden && t.IsForbidden(getter) 
            //     || !nutrientPasteDispenser.powerComp.PowerOn 
            //     || (!allowDispenserEmpty 
            //         && !nutrientPasteDispenser.HasEnoughFeedstockInHoppers() 
            //         || (!t.InteractionCell.Standable(t.Map) 
            //             || !FoodUtility.IsFoodSourceOnMapSociallyProper(t, getter, eater, allowSociallyImproper)))) 
            // || (getter.IsWildMan() 
            //     || !getter.Map.reachability.CanReachNonLocal(getter.Position, new TargetInfo(t.InteractionCell, t.Map, false), PathEndMode.OnCell, TraverseParms.For(getter, Danger.Some, TraverseMode.ByPawn, false)))

            if (!parameters.AllowDispenserFull)
            {
                traceOutput?.AppendLine($"Rejecting {item} because: search requested no dispensers");
                return false;
            }

            if (!parameters.Getter.CanManipulate())
            {
                traceOutput?.AppendLine($"Rejecting {item} because: {parameters.Getter} cannot manipulate dispenser");
                return false;
            }

            if (!nutrientPasteDispenser.CanDispenseNow)
            {
                traceOutput?.AppendLine($"Rejecting {item} because: dispenser cannot dispense now");
                return false;
            }

            if (!item.Thing.InteractionCell.Standable(item.Thing.Map))
            {
                traceOutput?.AppendLine($"Rejecting {item} because: interaction cell not standable");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Validates the food against the request and eater preferences, if applicable
        /// </summary>
        private bool ValidateFoodPreferences(FoodSearchItem item)
        {
            // Only care about preferences if not desperate
            if (parameters.Desperate || parameters.Eater.IsWildAnimal() || parameters.Eater.IsWildMan())
                return true;


            if (item.FoodCategory == FoodCategory.Hunt && item.Thing.Faction != null)
            {
                traceOutput?.AppendLine($"Rejecting {item} because: tame should not hunt faction pawns");
                return false;
            }

            if (item.Def.ingestible.preferability > parameters.MaxPref)
            {
                traceOutput?.AppendLine($"Rejecting {item} because: preferability {item.Def.ingestible.preferability} exceeds requested maximum {parameters.MaxPref}");
                return false;
            }

            if (item.Thing.Faction != parameters.Getter.Faction
                && item.Thing.Faction != parameters.Getter.HostFaction)
            {
                traceOutput?.AppendLine($"Rejecting {item} because: {parameters.Getter} not owner or guest");
                return false;
            }

            if (!parameters.AllowCorpse && (item.Thing is Corpse))
            {
                traceOutput?.AppendLine($"Rejecting {item} because: is corpse");
                return false;
            }

            if (!parameters.AllowDrug && item.Def.IsDrug)
            {
                traceOutput?.AppendLine($"Rejecting {item} because: is drug");
                return false;
            }

            if (!parameters.AllowSociallyImproper && !item.Thing.IsSociallyProper(parameters.Eater))
            {
                traceOutput?.AppendLine($"Rejecting {item} because: is not socially proper for {parameters.Eater}");
                return false;
            }

            var thoughtsFromConsuming = FoodUtility.ThoughtsFromIngesting(parameters.Eater, item.Thing, item.Def);
            var desperateThoughtFromConsuming = thoughtsFromConsuming.FirstOrDefault(DesperateOnlyThoughts.Contains);
            if (desperateThoughtFromConsuming != null)
            {
                traceOutput?.AppendLine($"Rejecting {item} because: would cause desperate thought {desperateThoughtFromConsuming}");
                return false;
            }

            return true;
        }
    }
}
