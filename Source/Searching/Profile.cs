using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

namespace SmarterFoodSelectionSlim.Searching
{
    /// <summary>
    /// Defines a prioritized list of food categories, grouped into preferability buckets
    /// </summary>
    public class Profile
    {
        public string Name;

        /// <summary>Good food</summary>
        public IList<IList<FoodCategory>> Good;

        /// <summary>Will only eat when desperately hungry</summary>
        public IList<IList<FoodCategory>> Bad;

        /// <summary>Will only eat when starving</summary>
        public IList<IList<FoodCategory>> Desperate;

        public static Profile For(Pawn pawn)
        {
            if (pawn.IsColonist())
                return Colonist;

            if (pawn.IsPet())
                return Pet;

            if (pawn.IsAscetic())
                return Ascetic;

            // Others can use the vanilla algorithm
            return null;
        }

        public static readonly Profile Colonist = new Profile
        {
            Name = "Colonist",
            Good = new[]
            {
                new[] { FoodCategory.MealLavish },
                new[] { FoodCategory.MealFine },
                new[] { FoodCategory.MealSimple },
                new[] { FoodCategory.RawTasty },
                new[] { FoodCategory.MealSurvival },
                new[] { FoodCategory.MealAwful },
            },
            Bad = new[]
            {
                new[] { FoodCategory.FertEggs },
                new[] { FoodCategory.RawBad, FoodCategory.AnimalProduct },
                new[] { FoodCategory.Luxury },
            },
            Desperate = new[]
            {
                new[] { FoodCategory.Plant, FoodCategory.PlantMatter },
                new[] { FoodCategory.RawInsect },
                new[] { FoodCategory.Kibble },
                new[] { FoodCategory.Corpse },
                new[] { FoodCategory.InsectCorpse },
                new[] { FoodCategory.HumanlikeCorpse },
            }
        };

        public static readonly Profile Pet = new Profile
        {
            Name = "Pet",
            Good = new[]
            {
                new[] { FoodCategory.Grass, FoodCategory.Hunt },
                new[] { FoodCategory.Hay },
                new[] { FoodCategory.Kibble },
                new[] { FoodCategory.MealAwful },
                new[] { FoodCategory.RawInsect, FoodCategory.InsectCorpse },
                new[] { FoodCategory.RawHuman, FoodCategory.HumanlikeCorpse },
                new[] { FoodCategory.RawBad },
                new[] { FoodCategory.Corpse },
                new[] { FoodCategory.RawTasty, FoodCategory.AnimalProduct },
            },
            Bad = new[]
            {
                // TODO: Prevent/accommodate meals being taken for training
                new[] { FoodCategory.MealSimple },
                new[] { FoodCategory.Plant, FoodCategory.PlantMatter, FoodCategory.Tree },
            },
            Desperate = new[]
            {
                new[] { FoodCategory.FertEggs },
            }
        };

        public static readonly Profile Ascetic = new Profile
        {
            Name = "Ascetic",
            Good = new[]
            {
                new[] { FoodCategory.MealAwful },
                new[] { FoodCategory.MealSimple },
                new[] { FoodCategory.RawBad, FoodCategory.AnimalProduct },
                new[] { FoodCategory.RawTasty },
                new[] { FoodCategory.MealSurvival },
            },
            Bad = new[]
            {
                new[] { FoodCategory.Plant, FoodCategory.PlantMatter },
                new[] { FoodCategory.MealFine },
                new[] { FoodCategory.MealLavish },
                new[] { FoodCategory.FertEggs },
            },
            Desperate = new[]
            {
                new[] { FoodCategory.RawInsect },
                new[] { FoodCategory.Kibble },
                new[] { FoodCategory.Corpse },
                new[] { FoodCategory.InsectCorpse },
                new[] { FoodCategory.HumanlikeCorpse },
            }
        };
    }
}

