namespace Sqlbi.Bravo.Infrastructure.Extensions.DynamicLinq
{
    using System;

    internal class DynamicProperty
    {
        private readonly string _name;
        private readonly Type _type;

        public DynamicProperty(string name, Type type)
        {
            _name = name ?? throw new ArgumentNullException(nameof(name));
            _type = type ?? throw new ArgumentNullException(nameof(type));
        }

        public string Name => _name;

        public Type Type => _type;
    }
}
