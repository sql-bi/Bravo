namespace Sqlbi.Bravo.Infrastructure.Windows.Interop
{
    /// <summary>
    /// https://github.com/dahall/Vanara/blob/master/PInvoke/Shared/WinError/HRESULT.Values.cs
    /// https://www.magnumdb.com/
    /// </summary>
    internal class HRESULT
    {
        public const int S_OK = 0;

        //public const int ERROR_FILE_NOT_FOUND = -2147024894;

        public const int ERROR_INVALID_DATA = unchecked((int)0x8007000D);

        public const int E_NOINTERFACE = unchecked((int)0x80004002);

        public const int NTE_BAD_KEY_STATE = unchecked((int)0x8009000B);
    }
}
