﻿using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

namespace SmarterFoodSelectionSlim.Searching
{
    public enum FoodCategory : byte
    {
        Null = 0,
        HumanlikeCorpse,
        InsectCorpse,
        Corpse,
        Hay,
        Kibble,
        RawBad,
        RawTasty,
        RawInsect,
        RawHuman,
        FertEggs,
        AnimalProduct,
        MealAwful,
        MealSurvival,
        MealSimple,
        MealFine,
        MealLavish,
        Plant,
        PlantMatter,
        Grass,
        Luxury,
        Hunt,
        Tree,
        Ignore = 255
    }

    public static class FoodCategoryExtensions
    {
        public static FoodCategory DetermineFoodCategory(this Thing thing) =>
            DetermineFoodCategory(thing.def);

        public static FoodCategory DetermineFoodCategory(this ThingDef def)
        {
            if (def == null)
                throw new ArgumentNullException(nameof(def));

            if (def == ThingDefOf.NutrientPasteDispenser)
                return FoodCategory.MealAwful;

            // List all foods with a race as huntable
            if (def.race != null)
                return FoodCategory.Hunt;

            if (def.ingestible != null)
            {
                FoodPreferability foodPref = def.ingestible.preferability;
                FoodTypeFlags foodType = def.ingestible.foodType;

                if (foodPref == FoodPreferability.NeverForNutrition || def.IsDrug)
                    return FoodCategory.Ignore;

                //if food has no nutritional value or is a drug ignore it
                if (def.ingestible.CachedNutrition <= 0f || def.IsDrug)
                    return FoodCategory.Ignore;

                if (foodPref == FoodPreferability.MealFine)
                    return FoodCategory.MealFine;

                if (foodPref == FoodPreferability.MealAwful)
                    return FoodCategory.MealAwful;

                if (foodPref == FoodPreferability.MealSimple)
                {
                    if (def == ThingDefOf.MealSurvivalPack || def == ThingDefOf.Pemmican)
                        return FoodCategory.MealSurvival;

                    return FoodCategory.MealSimple;
                }

                if (foodPref == FoodPreferability.MealLavish)
                    return FoodCategory.MealLavish;

                if ((foodType & FoodTypeFlags.Kibble) != 0)
                    return FoodCategory.Kibble;

                if ((foodType & FoodTypeFlags.AnimalProduct) != 0)
                {
                    if (def.GetCompProperties<CompProperties_Hatcher>() != null)
                        return FoodCategory.FertEggs;
                }

                if (def.ingestible.joyKind == JoyKindDefOf.Gluttonous && def.ingestible.joy >= 0.05f)
                    return FoodCategory.Luxury;

                if ((foodType & FoodTypeFlags.Tree) != 0)
                    return FoodCategory.Tree;

                if ((foodType & FoodTypeFlags.Plant) != 0)
                {
                    if (def == ThingDefOf.Hay)
                        return FoodCategory.Hay;

                    if (def.plant != null)
                        return FoodCategory.Plant;

                    if (def.thingCategories.Contains(ThingCategoryDefOf.PlantMatter))
                        return FoodCategory.PlantMatter;

                    if (foodPref == FoodPreferability.DesperateOnly)
                        return FoodCategory.Ignore;

                    return FoodCategory.Grass;
                }

                if (def.IsCorpse)
                {
                    if (RimWorld.FoodUtility.IsHumanlikeMeat(def))
                        return FoodCategory.HumanlikeCorpse;

                    if (def.FirstThingCategory == ThingCategoryDefOf.CorpsesInsect)
                        return FoodCategory.InsectCorpse;

                    if (def.ingestible?.sourceDef?.race?.IsMechanoid ?? false)
                        return FoodCategory.Ignore;

                    return FoodCategory.Corpse;
                }

                if (def.ingestible.tasteThought != null && def.ingestible.tasteThought.stages.All((ThoughtStage arg) => arg.baseMoodEffect < 0))
                {
                    if (RimWorld.FoodUtility.IsHumanlikeMeat(def))
                        return FoodCategory.RawHuman;

                    if (def == ThingDef.Named("Meat_Megaspider") || def.ingestible.tasteThought == ThoughtDefOf.AteInsectMeatAsIngredient)
                        return FoodCategory.RawInsect;

                    return FoodCategory.RawBad;
                }

                if ((def.ingestible.tasteThought == null || def.ingestible.tasteThought.stages.All((ThoughtStage arg) => arg.baseMoodEffect >= 0)))
                    return FoodCategory.RawTasty;

                if ((foodType & FoodTypeFlags.AnimalProduct) != 0)
                    return FoodCategory.AnimalProduct;
            }

            // non ingestible corpse ?
            if (def.IsCorpse)
                return FoodCategory.Ignore;

            return FoodCategory.Null;
        }
    }
}