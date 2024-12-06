using System.Reflection;
using System.Resources;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

[assembly: AssemblyTitle("Bravo for Power BI")]
[assembly: AssemblyDescription("Bravo for Power BI")]
[assembly: AssemblyCompany("SQLBI Corporation")]
[assembly: AssemblyProduct("Bravo for Power BI")]
[assembly: AssemblyCopyright("© SQLBI Corporation")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]
[assembly: AssemblyVersion("0.0.0.999")]
[assembly: AssemblyInformationalVersion("0.0.0.999-DEV")]
#if DEBUG
[assembly: AssemblyConfiguration("Debug")]
#else
[assembly: AssemblyConfiguration("Release")]
#endif

[assembly: ComVisible(false)]
[assembly: NeutralResourcesLanguage("en-US")]
[assembly: SupportedOSPlatform("Windows7.0")]
[assembly: TargetPlatform("Windows7.0")]

[assembly: InternalsVisibleTo("Bravo.Tests")]