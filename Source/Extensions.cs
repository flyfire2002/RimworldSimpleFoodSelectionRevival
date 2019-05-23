using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

namespace SimpleFoodSelection
{
    public static class Extensions
    {
        public static bool IsAnimal(this Pawn pawn) =>
            pawn.RaceProps?.Animal ?? false;

        public static bool IsPlayerFaction(this Pawn pawn) =>
            pawn.Faction?.IsPlayer ?? false;

        public static bool IsColonist(this Pawn pawn) =>
            pawn.IsPlayerFaction() && !pawn.IsAnimal();

        public static bool IsGuest(this Pawn pawn) =>
            !pawn.IsPlayerFaction() && pawn.HostFaction == Faction.OfPlayer;

        public static bool IsPet(this Pawn pawn) =>
            pawn.IsAnimal() && pawn.IsPlayerFaction();

        public static bool IsWildAnimal(this Pawn pawn) =>
            pawn.IsAnimal() && pawn.Faction == null;

        public static bool IsSownPlant(this Thing thing) =>
            (thing as Plant)?.sown ?? false;

        public static bool IsIncapacitated(this Pawn pawn) =>
            !pawn.health.InPainShock && pawn.Downed;

        public static bool IsCannibal(this Pawn pawn) =>
            pawn.story?.traits?.HasTrait(TraitDefOf.Cannibal) ?? false;

        public static bool IsAscetic(this Pawn pawn) =>
            pawn.story?.traits?.HasTrait(TraitDefOf.Ascetic) ?? false;

        public static bool IsHostile(this Pawn pawn) =>
            pawn.Faction?.HostileTo(Faction.OfPlayer) ?? false;

        public static bool IsFriendly(this Pawn pawn) =>
            !pawn.IsHostile();

        public static bool IsInsectFaction(this Pawn pawn) =>
            pawn.Faction == Faction.OfInsects;

        public static bool CanManipulate(this Pawn pawn) =>
            (pawn.RaceProps?.ToolUser ?? false) && (pawn.health?.capacities?.CapableOf(PawnCapacityDefOf.Manipulation) ?? false);

        public static bool HasFlag<T>(this T test, T value) where T : Enum
        {
            if (test == null)
                return false;

            if (value == null)
                throw new ArgumentNullException("value");

            var num = Convert.ToUInt64(value);
            return ((Convert.ToUInt64(test) & num) == num);

        }
    }
}
