namespace Sqlbi.Bravo.Infrastructure.Extensions.DynamicLinq
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    internal class Signature : IEquatable<Signature>
    {
        private readonly DynamicProperty[] _properties;
        private readonly int _hashCode;

        public Signature(IEnumerable<DynamicProperty> properties)
        {
            _properties = properties.ToArray();
            _hashCode = 0;

            foreach (DynamicProperty p in properties)
            {
                _hashCode ^= p.Name.GetHashCode() ^ p.Type.GetHashCode();
            }
        }

        public DynamicProperty[] Properties => _properties;

        public override int GetHashCode()
        {
            return _hashCode;
        }

        public override bool Equals(object? obj)
        {
            return obj is Signature signature && Equals(signature);
        }

        public bool Equals(Signature? other)
        {
            if (Properties.Length != other?.Properties.Length) 
                return false;

            for (var i = 0; i < Properties.Length; i++)
            {
                var @this = Properties[i];
                if (@this.Name != other.Properties[i].Name || 
                    @this.Type != other.Properties[i].Type)
                {
                    return false;
                }
            }
            return true;
        }
    }
}
