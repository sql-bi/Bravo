using System.Security;

namespace Sqlbi.Bravo.UI.Framework.Interfaces
{
    internal interface ISecurePassword
    {
        SecureString SecurePassword { get; }
    }
}