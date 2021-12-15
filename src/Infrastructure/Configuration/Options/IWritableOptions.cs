using Microsoft.Extensions.Options;
using System;

namespace Sqlbi.Bravo.Infrastructure.Configuration.Options
{
    public interface IWritableOptions<out T> : IOptions<T> where T : class, new()
    {
        void Update(Action<T> action);
    }
}
