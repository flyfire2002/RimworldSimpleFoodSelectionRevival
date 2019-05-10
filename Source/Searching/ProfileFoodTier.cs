using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

namespace SmarterFoodSelectionSlim.Searching
{
    public class ProfileFoodTier : IEnumerable<IEnumerable<FoodCategory>>
    {
        public ProfileFoodTier(string name, IEnumerable<IEnumerable<FoodCategory>> data, Func<Pawn, bool> shouldUse)
        {
            Name = name;
            this.data = data;
            this.shouldUse = shouldUse;
        }

        public string Name { get; }

        private readonly IEnumerable<IEnumerable<FoodCategory>> data;
        private readonly Func<Pawn, bool> shouldUse;

        public bool ShouldUse(Pawn pawn) => shouldUse(pawn);

        #region IEnumerable implementation
        public IEnumerator<IEnumerable<FoodCategory>> GetEnumerator() => data.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => data.GetEnumerator();
        #endregion
    }
}
