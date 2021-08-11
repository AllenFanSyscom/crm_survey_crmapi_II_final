using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;


namespace syscom.Reflection
{
	public static partial class ReflectionUtils
	{
		//public static Assembly DynamicGetAssemblyBy( String dllName )
		//{
		//	var targetPath = AppDomain.CurrentDomain.BaseDirectory;
		//	if ( !targetPath.EndsWith( @"\" ) ) targetPath += @"\";
		//	if ( !targetPath.ToUpper().Contains( @"BIN\" ) )
		//	{
		//		targetPath += @"bin\";
		//	}

		//	if ( !System.IO.File.Exists( targetPath + dllName ) )
		//	{
		//		throw Err.FileNotFound( "無法在目錄【" + targetPath + "】找到Dll【" + dllName + "】, 請檢查檔案是否存在執行目錄" );
		//	}

		//	var assembly = Assembly.LoadFile( targetPath + dllName );
		//	if ( assembly == null )
		//		throw Err.FileNotFound( "無法在執行時期讀取 【" + dllName + "】, 請檢查Dll是否存在執行目錄【" + targetPath + "】" );

		//	return assembly;
		//}

		public static Assembly GetAssemblyBy( String assemblyName )
		{
			var dllName = assemblyName.ToLower();
			var assembly = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault( a => a.FullName.ToLower() == dllName );
			if ( assembly != null ) return assembly;

			throw Err.Utility( "找不到Assembly [ " + assemblyName + " ]" );
		}

		public static Object CreateInstanceBy( String assemblyName, String className )
		{
			try
			{
				var handler = Activator.CreateInstance( assemblyName, className );
				return handler.Unwrap();
			}
			catch ( TypeLoadException ex )
			{
				throw Err.Utility( "無法產生Type [ " + className + " ] 從 [ " + assemblyName + " ]", ex );
			}
		}

		public static Object CreateInstanceOnInternalBy( Type type, params Object[] parameters )
		{
			try
			{
				const BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Instance;
				var instance = Activator.CreateInstance( type, flags, null, parameters, null );
				return instance;
			}
			catch ( TypeLoadException ex )
			{
				throw Err.Utility( "無法產生 Type: [ " + type + " ] 使用 parameters: [ " + parameters + " ]", ex );
			}
		}


		public static Type DynamicGetTypeBy( this Assembly assembly, String className )
		{
			var type = assembly.GetType( className );
			if ( type == null )
				throw Err.Module( "Bootstrap", "在目標Assembly【" + assembly.FullName + "】中無法取得【" + className + "】" );
			return type;
		}

		//public static MethodInfo DynamicGetMethodBy( Type type, String methodName, BindingFlags flags )
		//{
		//	var method = type.GetMethod( methodName, flags );
		//	if ( method == null )
		//		throw Err.ArgumentNull( "在目標class【" + type.FullName + "】中無法找到【" + methodName + "】Method" );

		//	return method;
		//}

		//public static MethodInfo DynamicGetMethodBy( Type type, String methodName )
		//{
		//	var method = type.GetMethod( methodName );
		//	if ( method == null )
		//		throw Err.ArgumentNull( "在目標class【" + type.FullName + "】中無法找到【" + methodName + "】Method" );

		//	return method;
		//}


		///// <summary>嘛嘿嘿嘛嘿嘿...小心使用</summary>
		//public static TResult DynamicExecuteAssemblyBy<TResult>( String dllName, Func<Assembly, TResult> invokeAssembly )
		//{
		//	var assembly = DynamicGetAssemblyBy( dllName );

		//	return invokeAssembly( assembly );
		//}

		///// <summary>動態取得Method...小心使用</summary>
		//public static MethodInfo DynamicGetMethodBy( String dllName, String className, String methodName )
		//{
		//	var assembly = DynamicGetAssemblyBy( dllName );

		//	var type = DynamicGetTypeBy( assembly, className );

		//	var method = DynamicGetMethodBy( type, methodName );

		//	return method;
		//}

		///// <summary>動態call Method, 小心使用</summary>
		//public static TResult DynamicExecuteBy<TResult>( MethodInfo method, params Object[] parameters )
		//{
		//	//這樣寫是為了讓Debug好用
		//	var result = method.Invoke( null, parameters );
		//	var converted = (TResult)result;
		//	return converted;
		//}
	}
}