using Sqlbi.Bravo.Infrastructure.Configuration.Settings;

namespace Sqlbi.Bravo.Infrastructure.Configuration;

internal interface IUserSettingsServiceFactory
{
    IUserSettings Create();
}

internal sealed class UserSettingsServiceFactory : IUserSettingsServiceFactory
{
    public IUserSettings Create() => UserPreferences.Current;
}
