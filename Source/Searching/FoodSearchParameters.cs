using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

namespace SmarterFoodSelectionSlim.Searching
{
    public class FoodSearchParameters
    {
        public Pawn Getter;
        public Pawn Eater;
        public bool Desperate;
        public bool CanUseInventory;
        public FoodPreferability MinPref = FoodPreferability.DesperateOnly;
        public FoodPreferability MaxPref;
        public bool AllowPlant;
        public bool AllowDrug;
        public bool AllowCorpse;
        //public bool AllowDispenserFull;
        //public bool AllowDispenserEmpty;
        public bool AllowForbidden;
        public bool AllowSociallyImproper;
        public bool AllowHarvest;
        public bool ForceScanWholeMap;

        public FoodSearchParameters(
            Pawn getter,
            Pawn eater,
            bool desperate,
            bool canUseInventory,
            FoodPreferability maxPref,
            bool allowPlant,
            bool allowDrug,
            bool allowCorpse,
            //bool allowDispenserFull,
            //bool allowDispenserEmpty,
            bool allowForbidden,
            bool allowSociallyImproper,
            bool allowHarvest,
            bool forceScanWholemap)
        {
            Getter = getter;
            Eater = eater;
            Desperate = desperate;
            CanUseInventory = canUseInventory;
            MaxPref = maxPref;
            AllowPlant = allowPlant;
            AllowDrug = allowDrug;
            AllowCorpse = allowCorpse;
            //AllowDispenserFull = allowDispenserFull;
            //AllowDispenserEmpty = allowDispenserEmpty;
            AllowForbidden = allowForbidden;
            AllowSociallyImproper = allowSociallyImproper;
            AllowHarvest = allowHarvest;
            ForceScanWholeMap = forceScanWholemap;
        }
    }
}
