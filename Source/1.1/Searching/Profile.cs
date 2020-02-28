using RimWorld;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

namespace SimpleFoodSelection.Searching
{
    /// <summary>
    /// Defines a prioritized list of food categories, grouped into preferability buckets
    /// </summary>
    public class Profile : IEnumerable<ProfileFoodTier>
    {
        public readonly string Name;

        public Profile(string name, 
            IEnumerable<IEnumerable<FoodCategory>> good, 
            IEnumerable<IEnumerable<FoodCategory>> bad, 
            IEnumerable<IEnumerable<FoodCategory>> desperate)
        {
            Name = name;
            Good = new ProfileFoodTier("Good", good, x => true);
            Bad = new ProfileFoodTier("Bad", bad, ResortToBad);
            Desperate = new ProfileFoodTier("Desperate", desperate, ResortToDesperate);
            Tiers = new[]
            {
                Good,
                Bad,
                Desperate
            };
        }

        /// <summary>Good food</summary>
        private readonly ProfileFoodTier Good;

        /// <summary>Will only eat when desperately hungry</summary>
        private readonly ProfileFoodTier Bad;

        /// <summary>Will only eat when starving</summary>
        private readonly ProfileFoodTier Desperate;

        /// <summary>All food tiers for this profile, in decreasing order of preference</summary>
        public readonly IEnumerable<ProfileFoodTier> Tiers;

        private static bool ResortToBad(Pawn eater) => 
            eater.needs.food.CurCategory >= HungerCategory.UrgentlyHungry;

        private static bool ResortToDesperate(Pawn eater) => 
            eater.needs.food.CurCategory >= HungerCategory.Starving;

        #region IEnumerable implementation
        public IEnumerator<ProfileFoodTier> GetEnumerator() => Tiers.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => Tiers.GetEnumerator();
        #endregion

        public static Profile For(Pawn pawn)
        {
            if (pawn.IsPet())
                return Pet;

            if (pawn.IsAscetic())
                return Ascetic;

            if (!pawn.AnimalOrWildMan())
                return Human;

            // Others can use the vanilla algorithm
            return null;
        }

        public static readonly Profile Human = new Profile(
            name: "Human",
            good: new[]
            {
                new[] { FoodCategory.MealLavish },
                new[] { FoodCategory.MealFine },
                new[] { FoodCategory.MealSimple },
                new[] { FoodCategory.RawTasty },
                new[] { FoodCategory.MealSurvival },
                new[] { FoodCategory.MealAwful },
            },
            bad: new[]
            {
                new[] { FoodCategory.FertEggs },
                new[] { FoodCategory.RawBad, FoodCategory.AnimalProduct },
                new[] { FoodCategory.Luxury },
            },
            desperate: new[]
            {
                new[] { FoodCategory.Plant, FoodCategory.PlantMatter },
                new[] { FoodCategory.RawInsect },
                new[] { FoodCategory.Kibble },
                new[] { FoodCategory.Corpse },
                new[] { FoodCategory.InsectCorpse },
                new[] { FoodCategory.HumanlikeCorpse },
            });

        public static readonly Profile Pet = new Profile(
            name: "Pet",
            good: new[]
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
            bad: new[]
            {
                // TODO: Prevent/accommodate meals being taken for training
                new[] { FoodCategory.MealSimple },
                new[] { FoodCategory.Plant, FoodCategory.PlantMatter, FoodCategory.Tree },
            },
            desperate: new[]
            {
                new[] { FoodCategory.FertEggs },
            });

        public static readonly Profile Ascetic = new Profile(
            name: "Ascetic",
            good: new[]
            {
                new[] { FoodCategory.MealAwful },
                new[] { FoodCategory.MealSimple },
                new[] { FoodCategory.RawBad, FoodCategory.AnimalProduct },
                new[] { FoodCategory.RawTasty },
                new[] { FoodCategory.MealSurvival },
            },
            bad: new[]
            {
                new[] { FoodCategory.Plant, FoodCategory.PlantMatter },
                new[] { FoodCategory.MealFine },
                new[] { FoodCategory.MealLavish },
                new[] { FoodCategory.FertEggs },
            },
            desperate: new[]
            {
                new[] { FoodCategory.RawInsect },
                new[] { FoodCategory.Kibble },
                new[] { FoodCategory.Corpse },
                new[] { FoodCategory.InsectCorpse },
                new[] { FoodCategory.HumanlikeCorpse },
            });
    }
}
