using System;
using System.Collections.Generic;

namespace Rebus.Internals
{
    class TypeEqualityComparer : IEqualityComparer<object>
    {
        public new bool Equals(object x, object y) => x != null && x.GetType() == y?.GetType();

        public int GetHashCode(object obj)
        {
            if (obj == null) throw new ArgumentNullException(nameof(obj));

            return obj.GetType().GetHashCode();
        }
    }
}
