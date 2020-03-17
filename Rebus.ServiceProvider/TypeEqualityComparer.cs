using System;
using System.Collections.Generic;

namespace Rebus.ServiceProvider
{
    internal class TypeEqualityComparer : IEqualityComparer<object>
    {
        public new bool Equals(object x, object y)
        {
            return x != null && x.GetType().Equals(y?.GetType());
        }

        public int GetHashCode(object obj)
        {
            if (obj == null)
                throw new ArgumentNullException(nameof(obj));

            return obj.GetType().GetHashCode();
        }
    }
}
