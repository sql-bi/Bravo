namespace Sqlbi.Bravo.Infrastructure.Windows.Interop;

internal struct NTSTATUS
{
    public const uint STATUS_SUCCESS = 0x00000000;
    public const uint STATUS_INFO_LENGTH_MISMATCH = 0xC0000004;
    public const uint STATUS_BUFFER_TOO_SMALL = 0xC0000023;
    public const uint STATUS_BUFFER_OVERFLOW = 0x80000005;
    public const uint STATUS_OBJECT_TYPE_MISMATCH = 0xC0000024;
    public const uint STATUS_INVALID_HANDLE = 0xC0000008;
}
