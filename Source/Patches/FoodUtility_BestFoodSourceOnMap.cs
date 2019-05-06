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
    public static class FoodUtility_BestFoodSourceOnMap
    {
        public static void Patch(HarmonyInstance harmony)
        {
            harmony.Patch(
                original: typeof(FoodUtility).GetMethod("BestFoodSourceOnMap"),
                prefix: new HarmonyMethod(typeof(FoodUtility_BestFoodSourceOnMap).GetMethod("Prefix")));
        }

        public static bool Prefix(ref Thing __result,
            Pawn getter, Pawn eater, bool desperate, ref ThingDef foodDef, FoodPreferability maxPref, 
            bool allowPlant, bool allowDrug, bool allowCorpse, bool allowDispenserFull, bool allowDispenserEmpty,
            bool allowForbidden, bool allowSociallyImproper, bool allowHarvest, bool forceScanWholeMap)
        {
#if DEBUG
            var traceOutput = new StringBuilder();
            traceOutput.AppendLine($"Intercepting FoodUtility.BestFoodSourceOnMap getter={getter}|eater={eater}|maxPref={maxPref}|"
                + $"allowPlant={allowPlant}|allowDrug={allowDrug}|allowCorpse={allowCorpse}|allowDispenserFull={allowDispenserFull}|allowDispenserEmpty={allowDispenserEmpty}|"
                + $"allowForbidden={allowForbidden}|allowSociallyImproper={allowSociallyImproper}|allowHarvest={allowHarvest}|forceScanWholeMap={forceScanWholeMap}");
#endif
            try
            {
                var parameters = new FoodSearchParameters(
                    getter: getter,
                    eater: eater,
                    desperate: desperate,
                    canUseInventory: false,
                    maxPref: maxPref,
                    allowPlant: allowPlant,
                    allowDrug: allowDrug,
                    allowCorpse: allowCorpse,
                    allowDispenserFull: allowDispenserFull,
                    allowDispenserEmpty: allowDispenserEmpty,
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

                    __result = result.Thing;
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
