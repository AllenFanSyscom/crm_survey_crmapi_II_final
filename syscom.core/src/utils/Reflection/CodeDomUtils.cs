using System;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CSharp;
using syscom.Reflection;

namespace syscom.CodeDom
{
	public static class CodeDomUtils
	{
		// public static CSharpCodeProvider NewCodeProvider => new CSharpCodeProvider( new Dictionary<String, String> { { "CompilerVersion", "v4.0" } } );
		// public static CodeCompileUnit NewCodeCompileUnit => new CodeCompileUnit();
		//
		// public static CompilerParameters NewCompilerOptions( params String[] dlls )
		// {
		// 	if ( dlls == null || dlls.Length == 0 ) dlls = new[] { "mscorlib.dll", "System.Core.dll" };
		// 	var options = new CompilerParameters( dlls )
		// 	{
		// 		GenerateExecutable = false,
		// 		GenerateInMemory = true
		// 	};
		// 	return options;
		// }


		static CodeDomUtils() { }


		//public static List<Type> CreateInterfaceDynamicRuntimeTypesBy( IEnumerable<Type> sourceTypes, String? outputAssemblyName = null, String version = "" )
		//{
		//	var targetTypeList = new List<Type>();

		//	var csc = NewCodeProvider;
		//	var options = NewCompilerOptions();
		//	var unit = NewCodeCompileUnit;

		//	//版本資訊
		//	if ( !String.IsNullOrEmpty( version ) ) unit.AddAssemblyVersionBy( version );

		//	foreach ( var sourceType in sourceTypes )
		//	{
		//		if ( !sourceType.IsInterface ) throw Err.Utility( "來源型別[ " + sourceType.FullName + " ]必需為Interface才能動態實作" );
		//		if ( !sourceType.Name.StartsWith( "I" ) ) throw Err.Utility( "來源型別[ " + sourceType.FullName + " ]必需以I為開頭名稱" );

		//		var targetTypeName = sourceType.Name.Substring( 1 );
		//		var targetNamespace = sourceType.Namespace + ".DynamicTypes";

		//		var targetFullName = targetNamespace + "." + targetTypeName;
		//		var existType = Type.GetType( targetFullName ); if ( existType != null ) continue;//如果型別已存在則略過

		//		//實作class, 採用一般策略
		//		sourceType.AutoImplementClassBy( unit, options, targetNamespace, targetTypeName );
		//	}

		//	options.OutputAssembly = outputAssemblyName ?? "syscom.configRuntimeDynamicType." + targetTypeList.Count + ".dll";
		//	options.GenerateInMemory = true;
		//	options.GenerateExecutable = false;

		//	var result = csc.CompileBy( options, unit );
		//	var resultType = result.CompiledAssembly.GetTypes().ToList();
		//	return resultType;
		//}


		//public static Type CreateInterfaceDynamicRuntimeTypeBy<TInterface>( String targetNamespace, String dynamicTypeName, IEnumerable<String>? includeNamespaces = null, IEnumerable<String>? includeDlls = null, Boolean recursiveResolveMember = false )
		//{
		//	var sourceType = typeof( TInterface );
		//	return CreateInterfaceDynamicRuntimeTypeBy( sourceType, targetNamespace, dynamicTypeName, includeNamespaces, includeDlls, recursiveResolveMember );
		//}
		//public static Type CreateInterfaceDynamicRuntimeTypeBy( Type sourceType, String targetNamespace, String dynamicTypeName, IEnumerable<String>? includeNamespaces = null, IEnumerable<String>? includeDlls = null, Boolean recursiveResolveMember = false )
		//{
		//	if ( !sourceType.IsInterface ) throw Err.Utility( "CreateDynamicRuntimeTypeBy的來源型別必需為Interface" );

		//	var targetTypeFullName = sourceType.Namespace + ".DynamicTypes." + dynamicTypeName;
		//	var existType = Type.GetType( targetTypeFullName );
		//	if ( existType != null ) return existType;


		//	var csc = NewCodeProvider;
		//	var options = NewCompilerOptions();
		//	options.OutputAssembly = "syscom.configDynamicTypes." + dynamicTypeName + ".dll";
		//	options.GenerateInMemory = true;
		//	options.GenerateExecutable = false;


		//	var unit = NewCodeCompileUnit;
		//	var ns = unit.CreateOrGetNamespaceBy( targetNamespace );
		//	ns.AutoImportBy( "System" );


		//	if ( includeNamespaces != null ) { foreach ( var nsItem in includeNamespaces ) { ns.AutoImportBy( nsItem ); } }


		//	if ( includeDlls != null )
		//	{
		//		foreach ( var dll in includeDlls )
		//		{
		//			var dllName = dll.ToUpper().EndsWith( ".DLL" ) ? dll : ( dll + ".dll" );
		//			options.ReferencedAssemblies.Add( dllName );
		//		}
		//	}

		//	//實作class, 採用一般策略
		//	var type = sourceType.AutoImplementClassBy( unit, options, targetNamespace, dynamicTypeName );


		//	var result = csc.CompileBy( options, unit );
		//	var resultType = result.CompiledAssembly.GetTypes().FirstOrDefault();
		//	return resultType;
		//}
	}
}
