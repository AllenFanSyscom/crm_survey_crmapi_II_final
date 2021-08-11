using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace syscom
{
	public static class StackUtils
	{
		static StackFrame gatewayFrame;

		public static StackFrame GetPrevious()
		{
#if DEBUG
			return new StackFrame( 2 );
#else
			return new StackFrame( 1 );
#endif
		}

		public static StackTrace GetStackTrace() { return new StackTrace(); }

		/// <summary>取得Main進入點的資訊</summary>
		public static StackFrame GetApplicationMainFrame()
		{
			if ( gatewayFrame != null ) return gatewayFrame;

			var st = new StackTrace();

			gatewayFrame = st.GetFrames().FirstOrDefault( f => f.ToString().Contains( "Main at offset" ) );
			return gatewayFrame;
			//if ( mainFrame == null ) throw Err.InvalidOperation( "程式並非由Main進入點進入, 無法取得MainFrame" );
		}

		public static Type GetApplicationMainGatewayType()
		{
			if ( gatewayFrame == null ) GetApplicationMainFrame();
			return gatewayFrame == null ? null : gatewayFrame.GetMethod().DeclaringType;
		}

		public static MethodBase GetFirstFrameMethod()
		{
			var st = new StackTrace();

			var first = st.GetFrames().FirstOrDefault();

			Debug.Assert( first == null, "first frame be null is impossible" );

			return first.GetMethod();
		}

		//public static MethodBase GetTestMethodGateway()
		//{
		//	var st = new StackTrace();

		//	var frames = st.GetFrames();

		//	MethodBase? method = null;
		//	foreach ( var frame in frames )
		//	{
		//		method = frame.GetMethod();

		//		if ( method.GetCustomAttributes<Microsoft.VisualStudio.TestTools.UnitTesting.TestMethodAttribute>().Any() )
		//			return method;
		//	}

		//	throw Err.Utility( "無法找到TestMethod的入口" );
		//}

		static readonly Assembly assembly_core = typeof( StackUtils ).Assembly;
		static readonly Assembly assembly_mscorlib = typeof( String ).Assembly;
		static readonly Assembly assembly_system = typeof( Debug ).Assembly;


		public static Int32 GetFrameCount( this StackTrace strackTrace ) { return strackTrace.FrameCount; }

		public static String GetStackFrameMethodName( MethodBase method, Boolean includeMethodInfo, Boolean cleanAsyncMoveNext, Boolean cleanAnonymousDelegates )
		{
			if ( method == null ) return null;
			var methodName = method.Name;
			var callerClassType = method.DeclaringType;
			if ( cleanAsyncMoveNext && methodName == "MoveNext" && callerClassType?.DeclaringType != null && callerClassType.Name.StartsWith( "<" ) )
			{
				// UnitTests.LayoutRenderers.CallSiteTests+<CleanNamesOfAsyncContinuations>d_3'1.MoveNext
				var endIndex = callerClassType.Name.IndexOf( '>', 1 );
				if ( endIndex > 1 )
				{
					methodName = callerClassType.Name.Substring( 1, endIndex - 1 );
					if ( methodName.StartsWith( "<" ) ) methodName = methodName.Substring( 1, methodName.Length - 1 ); // Local functions, and anonymous-methods in Task.Run()
				}
			}

			// Clean up the function name if it is an anonymous delegate
			// <.ctor>b__0
			// <Main>b__2
			if ( cleanAnonymousDelegates && methodName.StartsWith( "<" ) && methodName.Contains( "__" ) && methodName.Contains( ">" ) )
			{
				var startIndex = methodName.IndexOf( '<' ) + 1;
				var endIndex = methodName.IndexOf( '>' );
				methodName = methodName.Substring( startIndex, endIndex - startIndex );
			}

			if ( includeMethodInfo && methodName == method.Name ) methodName = method.ToString();

			return methodName;
		}

		public static String GetStackFrameMethodClassName( MethodBase method, Boolean includeNameSpace, Boolean cleanAsyncMoveNext, Boolean cleanAnonymousDelegates )
		{
			if ( method == null ) return null;

			var callerClassType = method.DeclaringType;
			if ( cleanAsyncMoveNext && method.Name == "MoveNext" && callerClassType?.DeclaringType != null && callerClassType.Name.StartsWith( "<" ) )
			{
				// UnitTests.LayoutRenderers.CallSiteTests+<CleanNamesOfAsyncContinuations>d_3'1
				var endIndex = callerClassType.Name.IndexOf( '>', 1 );
				if ( endIndex > 1 ) callerClassType = callerClassType.DeclaringType;
			}

			if ( !includeNameSpace
			     && callerClassType?.DeclaringType != null
			     && callerClassType.IsNested
			     && callerClassType.GetCustomAttribute<CompilerGeneratedAttribute>() != null )
				return callerClassType.DeclaringType?.Name;

			var className = includeNameSpace ? callerClassType?.FullName : callerClassType?.Name;

			if ( !cleanAnonymousDelegates || className == null ) return className;
			var index = className.IndexOf( "+<>", StringComparison.Ordinal );
			if ( index >= 0 ) className = className.Substring( 0, index );

			return className;
		}

		[MethodImpl( MethodImplOptions.NoInlining )]
		public static String GetClassFullName()
		{
			const Int32 framesToSkip = 2;

			var className = String.Empty;
			var stackFrame = new StackFrame( framesToSkip, false );
			className = GetClassFullName( stackFrame );
			return className;
		}

		public static String GetClassFullName( StackFrame stackFrame )
		{
			var className = LookupClassNameFromStackFrame( stackFrame );
			if ( !String.IsNullOrEmpty( className ) ) return className;
			var stackTrace = new StackTrace( false );
			className = GetClassFullName( stackTrace );
			if ( String.IsNullOrEmpty( className ) ) className = stackFrame.GetMethod()?.Name ?? String.Empty;

			return className;
		}

		static String GetClassFullName( StackTrace stackTrace )
		{
			foreach ( var frame in stackTrace.GetFrames() )
			{
				var className = LookupClassNameFromStackFrame( frame );
				if ( !String.IsNullOrEmpty( className ) ) return className;
			}

			return String.Empty;
		}

		public static Assembly LookupAssemblyFromStackFrame( StackFrame stackFrame )
		{
			var method = stackFrame.GetMethod();
			if ( method == null ) return null;

			var assembly = method.DeclaringType?.Assembly ?? method.Module?.Assembly;
			// skip stack frame if the method declaring type assembly is from hidden assemblies list

			if ( assembly == assembly_core ) return null;
			if ( assembly == assembly_mscorlib ) return null;
			return assembly == assembly_system ? null : assembly;
		}

		public static String LookupClassNameFromStackFrame( StackFrame stackFrame )
		{
			var method = stackFrame.GetMethod();
			if ( method == null || LookupAssemblyFromStackFrame( stackFrame ) == null ) return String.Empty;
			var className = GetStackFrameMethodClassName( method, true, true, true );
			if ( !String.IsNullOrEmpty( className ) )
			{
				if ( !className.StartsWith( "System.", StringComparison.Ordinal ) ) return className;
			}
			else
			{
				className = method.Name ?? String.Empty;
				if ( className != "lambda_method" && className != "MoveNext" ) return className;
			}

			return String.Empty;
		}




		public static Assembly LookupExecutingAssembly( params String[] ignoreNames )
		{
			var IgnoreAssemblyNames = new []{ "syscom.core", "mscorlib" };
			var frames = new StackTrace().GetFrames();

			foreach ( var frame in frames )
			{
				var assembly = frame.GetMethod().Module.Assembly;
				var name = assembly.GetName().Name;
				//Log.LogFile( $"[Stack] finding[{ name }] ignoreNames[{ ignoreNames.ToJson() }]" );

				if (
					assembly == assembly_mscorlib ||
					assembly == assembly_core ||
					assembly == assembly_system
				)
					continue;

				if( IgnoreAssemblyNames.Contains( name ) ) continue;
				if( ignoreNames != null && ignoreNames.Contains( name ) ) continue;

				return assembly;
			}

			return null;
		}
	}
}
