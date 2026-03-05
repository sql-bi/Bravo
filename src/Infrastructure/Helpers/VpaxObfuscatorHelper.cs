namespace Sqlbi.Bravo.Infrastructure.Helpers;

using Dax.Metadata;
using Dax.Vpax.Obfuscator;
using Dax.Vpax.Obfuscator.Common;

internal static class VpaxObfuscatorHelper
{
    public static void ObfuscateAndExportDictionary(Stream vpaxStream, string path)
    {
        try
        {
            var obfuscator = new VpaxObfuscator();

            var dictionary = obfuscator.Obfuscate(vpaxStream, dictionary: null);
            dictionary.WriteTo(path, overwrite: false, indented: true);
        }
        catch (Exception ex)
        {
            throw new BravoException(BravoProblem.VpaxObfuscationError, ex.Message, ex);
        }
    }

    public static void Deobfuscate(Model model, Stream obfuscationDictionaryStream)
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
