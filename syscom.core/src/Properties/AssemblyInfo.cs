using System.Reflection;
using System.Runtime.InteropServices;

//===========================================================
// Dll General Information's
//===========================================================
[assembly: AssemblyTitle( "syscom.core" )]
[assembly: AssemblyDescription( "" )]
[assembly: AssemblyConfiguration( "" )]
[assembly: AssemblyCompany( "" )]
[assembly: AssemblyProduct( "syscom.core" )]
[assembly: AssemblyCopyright( "Copyright ©  2020" )]
[assembly: AssemblyTrademark( "" )]
[assembly: AssemblyCulture( "" )]

//===========================================================
// Dll Lib Settings
//===========================================================
[assembly: Guid( "7823d327-e04e-434e-b0f6-8c662e0b3d88" )]
[assembly: ComVisible( false )]
[assembly: System.CLSCompliant( false )]
[assembly: System.Security.AllowPartiallyTrustedCallers]
[assembly: System.Security.SecurityRules( System.Security.SecurityRuleSet.Level1 )]

//===========================================================
// Framework Dll Version - by Raz:20150707
// Rules:
// 1. Major Version  - Large-Scale Architecture Level
// 2. Minor Version  - Major functionally changing, New Feature or Functional change
// 3. Change Year    - Change Year : YYYY
// 4. Last Version   - Change Date : MMDD
//===========================================================
[assembly: AssemblyVersion( "1.6.2020.0525" )]

//===========================================================
// Develop series Settings
//===========================================================
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo( "syscom.UnitTests" )]
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo( "syscom.Monitor.Agent" )]
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo( "syscom.Monitor.MonitorServer" )]
