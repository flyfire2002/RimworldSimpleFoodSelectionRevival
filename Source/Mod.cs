using HarmonyLib;
using SimpleFoodSelection.Patches;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

namespace SimpleFoodSelection
{
    [StaticConstructorOnStartup]
    class Mod 
    {
        static Mod()
        {
            var harmony = new Harmony(ModName);

            FoodUtility_BestFoodSourceOnMap.Patch(harmony);
            FoodUtility_TryFindBestFoodSourceFor.Patch(harmony);
            
            LogMessage("Initialized");
        }

        const string ModName = "SimpleFoodSelection";

        public static void LogMessage(string text) => Log.Message($"[{ModName}] {text}");
        public static void LogWarning(string text) => Log.Warning($"[{ModName}] {text}");
        public static void LogError(string text) => Log.Error($"[{ModName}] {text}");
    }
}
