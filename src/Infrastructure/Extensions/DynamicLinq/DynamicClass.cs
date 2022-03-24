namespace Sqlbi.Bravo.Infrastructure.Extensions.DynamicLinq
{
    using System.Reflection;
    using System.Text;

    internal abstract class DynamicClass
    {
        public override string ToString()
        {
            var properties = GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public);
            var builder = new StringBuilder();

            builder.Append('{');

            for (var i = 0; i < properties.Length; i++)
            {
                if (i > 0) 
                    builder.Append(", ");

                builder.Append(properties[i].Name);
                builder.Append('=');
                builder.Append(properties[i].GetValue(this, null));
            }

            builder.Append('}');

            return builder.ToString();
        }
    }
}
