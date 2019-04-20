using Harmony;
using SmarterFoodSelectionSlim.Patches;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

namespace SmarterFoodSelectionSlim
{
    [StaticConstructorOnStartup]
    class Mod 
    {
        static Mod()
        {
            var harmony = HarmonyInstance.Create(ModName);

            FoodUtility_BestFoodSourceOnMap.Patch(harmony);
            FoodUtility_TryFindBestFoodSourceFor.Patch(harmony);
            
            LogMessage("Initialized");
        }

        const string ModName = "SmarterFoodSelectionSlim";

        public static void LogMessage(string text) => Log.Message($"[{ModName}] {text}");
        public static void LogWarning(string text) => Log.Warning($"[{ModName}] {text}");
        public static void LogError(string text) => Log.Error($"[{ModName}] {text}");
    }
}
