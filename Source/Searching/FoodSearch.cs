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
            foodSearchGroups = new FoodSearchGroups(parameters.Getter);
        }

        private readonly StringBuilder traceOutput = new StringBuilder();

        private readonly FoodSearchParameters parameters;
        private readonly FoodSearchGroups foodSearchGroups;

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
                    traceOutput?.AppendLine($"No profile found for {parameters.Eater}, falling back to vanilla");
                    return new FoodSearchResult { Success = false };
                }
                traceOutput?.AppendLine($"Using profile {profile.Name} for {parameters.Eater}");

                foreach (var foodTier in profile)
                {   // foodTier e.g. Good/Bad/Desperate
                    if (!foodTier.ShouldUse(parameters.Eater))
                    {
                        traceOutput?.AppendLine($"Not using food tier {foodTier.Name}, assuming others are worse and aborting here");
                        return new FoodSearchResult { Success = true };
                    }

                    traceOutput?.AppendLine($"Searching food tier {foodTier.Name}");
                    foreach (var foodSearchGroup in foodSearchGroups)
                    {   // foodSearchGroup e.g. Inventory/HomeArea
                        if (!foodSearchGroup.Any())
                            continue;

                        // TODO: optimize sorting/search loops for group lookup?
                        foreach (var categoryGroup in foodTier)
                        {   // categoryGroup e.g. RawMeat||AnimalCorpse
                            var categoryFoods = foodSearchGroup.Where(x => categoryGroup.Contains(x.FoodCategory));
                            foreach (var foodSearchItem in categoryFoods)
                            {   // foodSearchItem e.g. SurvivalMealPack24406
                                if (Validate(foodSearchItem))
                                {
                                    traceOutput?.AppendLine($"Selecting food " + foodSearchItem);
                                    return new FoodSearchResult(foodSearchItem);
                                }
                            }
                        }
                    }
                }

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
        /// Validates that the given item is eligible for eating in this situation
        /// </summary>
        /// <remarks>
        /// Performing validation checks as late as possible maximizes optimistic performance
        /// </remarks>
        private bool Validate(FoodSearchItem item)
        {
            if (!parameters.AllowForbidden && item.Thing.IsForbidden(parameters.Getter))
            {
                traceOutput?.AppendLine($"Rejecting {item.Thing}: is forbidden to {parameters.Getter}");
                return false;
            }

            if (!parameters.Eater.WillEat(item.Def, parameters.Getter))
            {
                traceOutput?.AppendLine($"Rejecting {item.Thing}: {parameters.Eater} will not eat def {item.Def}");
                return false;
            }

            if (!ValidateHunt(item))
                return false;

            if (!ValidatePlant(item))
                return false;

            if (!ValidateDispenser(item))
                return false;

            if (!ValidatePreferences(item))
                return false;

            // Potentially expensive path canculation last
            if (!parameters.Getter.CanReach(new LocalTargetInfo(item.Position), Verse.AI.PathEndMode.OnCell, Danger.Unspecified, parameters.Desperate))
            {
                traceOutput?.AppendLine($"Rejecting {item.Thing}: {parameters.Getter} cannot reach");
                return false;
            }

            return true;
        }

        private bool ValidateHunt(FoodSearchItem item)
        {
            if (item.FoodCategory != FoodCategory.Hunt)
                return true;

            
            if (parameters.Getter.Faction != null
                && item.Thing.Faction != null)
            {
                traceOutput?.AppendLine($"Rejecting {item.Thing}: faction {parameters.Getter.Faction} will not hunt faction {item.Thing.Faction}");
                return false;
            }

            if (parameters.Eater.IsWildAnimal() || parameters.Eater.IsWildMan())
            {
                if (!parameters.Desperate
                    && item.Def.race == parameters.Eater.def.race)
                {
                    traceOutput?.AppendLine($"Rejecting {item.Thing}: wild will not hunt same race if not desperate");
                    return false;
                }
            }

            return true;
        }

        private bool ValidatePlant(FoodSearchItem item)
        {
            if (item.Def.plant == null)
                return true;

            var plant = item.Thing as Plant;
            if (plant == null)
                return true;


            if (!parameters.AllowPlant)
            {
                traceOutput?.AppendLine($"Rejecting {item.Thing}: is plant");
                return false;
            }

            // TODO: harvestable plants
            if (parameters.AllowHarvest 
                && plant.HarvestableNow
                && item.Def.plant.harvestedThingDef.IsIngestible)
            {
                traceOutput?.AppendLine("TODO: harvest plant?");
                // var harvestedThingDef = thing.def.plant.harvestedThingDef;

                // Vanilla harvest logic?
                //Thing foodSource = GenClosest.ClosestThingReachable(getter.Position, getter.Map, ThingRequest.ForGroup(ThingRequestGroup.HarvestablePlant), PathEndMode.Touch, TraverseParms.For(getter, Danger.Deadly, TraverseMode.ByPawn, false), 9999f, (Predicate<Thing>)(x =>
                //{
                //    Plant t = (Plant)x;
                //    if (!t.HarvestableNow)
                //        return false;
                //    ThingDef harvestedThingDef = t.def.plant.harvestedThingDef;
                //    return harvestedThingDef.IsNutritionGivingIngestible && eater.WillEat(harvestedThingDef, getter) && getter.CanReserve((LocalTargetInfo)((Thing)t), 1, -1, (ReservationLayerDef)null, false) && ((allowForbidden || !t.IsForbidden(getter)) && (bestThing == null || FoodUtility.GetFinalIngestibleDef(bestThing, false).ingestible.preferability < harvestedThingDef.ingestible.preferability));
                //}), (IEnumerable<Thing>)null, 0, searchRegionsMax, false, RegionType.Set_Passable, false);
                //if (foodSource != null)
                //{
                //    bestThing = foodSource;
                //    foodDef = FoodUtility.GetFinalIngestibleDef(foodSource, true);
                //}
            }

            if (parameters.Getter != parameters.Eater)
            {
                traceOutput?.AppendLine($"Rejecting {item.Thing}: pawns should not carry plants");
                return false;
            }

            return true;
        }

        private bool ValidateDispenser(FoodSearchItem item)
        {
            var nutrientPasteDispenser = item.Thing as Building_NutrientPasteDispenser;
            if (nutrientPasteDispenser == null)
                return true;


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
                traceOutput?.AppendLine($"Rejecting {item.Thing}: search requested no dispensers");
                return false;
            }

            if (!parameters.Getter.CanManipulate())
            {
                traceOutput?.AppendLine($"Rejecting {item.Thing}: {parameters.Getter} cannot manipulate dispenser");
                return false;
            }

            if (!nutrientPasteDispenser.CanDispenseNow)
            {
                traceOutput?.AppendLine($"Rejecting {item.Thing}: dispenser cannot dispense now");
                return false;
            }

            if (!nutrientPasteDispenser.InteractionCell.Standable(item.Thing.Map))
            {
                traceOutput?.AppendLine($"Rejecting {item.Thing}: dispenser interaction cell not standable");
                return false;
            }

            return true;
        }

        private bool ValidatePreferences(FoodSearchItem item)
        {
            // Only care about preferences at all if not desperate or animalistic
            if (parameters.Desperate || parameters.Eater.IsWildAnimal() || parameters.Eater.IsWildMan())
                return true;


            if (item.Def.ingestible.preferability > parameters.MaxPref)
            {
                traceOutput?.AppendLine($"Rejecting {item.Thing}: preferability {item.Def.ingestible.preferability} exceeds requested maximum {parameters.MaxPref}");
                return false;
            }

            if (item.Thing.Faction != parameters.Getter.Faction
                && item.Thing.Faction != parameters.Getter.HostFaction)
            {
                traceOutput?.AppendLine($"Rejecting {item.Thing}: {parameters.Getter} not owner or guest of {item.Thing.Faction}");
                return false;
            }

            if (!parameters.AllowCorpse && (item.Thing is Corpse))
            {
                traceOutput?.AppendLine($"Rejecting {item.Thing}: is corpse");
                return false;
            }

            if (!parameters.AllowDrug && item.Def.IsDrug)
            {
                traceOutput?.AppendLine($"Rejecting {item.Thing}: is drug");
                return false;
            }

            if (!parameters.AllowSociallyImproper && !item.Thing.IsSociallyProper(parameters.Eater))
            {
                traceOutput?.AppendLine($"Rejecting {item.Thing}: is not socially proper for {parameters.Eater}");
                return false;
            }


            // Animals don't have thoughts (fixing null bug in alien framework patch intercepting pets)
            if (parameters.Eater.IsAnimal())
                return true;


            var thoughtsFromConsuming = FoodUtility.ThoughtsFromIngesting(parameters.Eater, item.Thing, item.Def);
            var desperateThoughtFromConsuming = thoughtsFromConsuming.FirstOrDefault(DesperateOnlyThoughts.Contains);
            if (desperateThoughtFromConsuming != null)
            {
                traceOutput?.AppendLine($"Rejecting {item.Thing}: would cause desperate thought {desperateThoughtFromConsuming}");
                return false;
            }

            return true;
        }
    }
}
