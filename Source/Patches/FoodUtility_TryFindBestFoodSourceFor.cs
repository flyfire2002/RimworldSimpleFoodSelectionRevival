using Harmony;
using RimWorld;
using SmarterFoodSelectionSlim.Searching;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

namespace SmarterFoodSelectionSlim.Patches
{
    public static class FoodUtility_TryFindBestFoodSourceFor
    {
        public static void Patch(HarmonyInstance harmony)
        {
            harmony.Patch(
                original: typeof(FoodUtility).GetMethod("TryFindBestFoodSourceFor"),
                prefix: new HarmonyMethod(typeof(FoodUtility_TryFindBestFoodSourceFor).GetMethod("Prefix")));
        }

        public static bool Prefix(ref bool __result,
            Pawn getter, Pawn eater, bool desperate, ref Thing foodSource, ref ThingDef foodDef, bool canRefillDispenser, bool canUseInventory,
            bool allowForbidden, bool allowCorpse, bool allowSociallyImproper, bool allowHarvest, bool forceScanWholeMap)
        {
#if DEBUG
            var traceOutput = new StringBuilder();
            traceOutput.AppendLine($"Intercepting FoodUtility.TryFindBestFoodSourceFor getter={getter}|eater={eater}|desperate={desperate}|"
                + $"canRefillDispenser={canRefillDispenser}|canUseInventory={canUseInventory}|allowForbidden={allowForbidden}|"
                + $"allowCorpse={allowCorpse}|allowSociallyImproper={allowSociallyImproper}|allowHarvest={allowHarvest}|forceScanWholeMap={forceScanWholeMap}");
#endif
            try
            {
                var parameters = new FoodSearchParameters(
                    getter: getter,
                    eater: eater,
                    desperate: desperate,
                    canUseInventory: canUseInventory,
                    maxPref: FoodPreferability.MealLavish,
                    allowPlant: true,
                    allowDrug: true,
                    allowCorpse: allowCorpse,
                    allowDispenserFull: true,
                    allowDispenserEmpty: true,
                    allowForbidden: allowForbidden,
                    allowSociallyImproper: allowSociallyImproper,
                    allowHarvest: allowHarvest,
                    forceScanWholeMap: forceScanWholeMap);

#if DEBUG
                var result = new FoodSearch(parameters, traceOutput).Find();
#else
                var result = new FoodSearch(parameters).Find();
#endif

                if (result.Success)
                {
#if DEBUG
                    traceOutput.AppendLine($"Found food {result.Thing?.Label ?? "(none)"} with def {result.Def?.label ?? "(none)"} for {eater}");
#endif

                    __result = result.Thing != null;
                    foodSource = result.Thing;
                    foodDef = result.Def;
                    return false;
                }
            }
            catch (Exception ex)
            {
                Mod.LogError(ex.ToString() + Environment.NewLine + ex.StackTrace);
            }
#if DEBUG
            finally
            {
                Mod.LogMessage(traceOutput.ToString());
            }
#endif

            // If failure, fall back to vanilla
            return true;
        }
    }
}
