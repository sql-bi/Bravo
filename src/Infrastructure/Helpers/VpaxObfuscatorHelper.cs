namespace Sqlbi.Bravo.Infrastructure.Helpers;

using Dax.Metadata;
using Dax.Vpax.Obfuscator;
using Dax.Vpax.Obfuscator.Common;

internal static class VpaxObfuscatorHelper
{
    public static void DeobfuscateModel(Model model, Stream obfuscationDictionaryStream)
    {
        try
        {
            var dictionary = ObfuscationDictionary.ReadFrom(obfuscationDictionaryStream);
            var obfuscator = new VpaxObfuscator();

            obfuscator.Deobfuscate(model, dictionary);
        }
        catch (Exception ex)
        {
            throw new BravoException(BravoProblem.VpaxDeobfuscationError, ex.Message, ex);
        }
    }
}
