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
	public static class TypeUtils
	{
		static String[] _ignoreAssemblies = new[]
		{
			"ArtInjector",
			"Oracle.DataAccess",
			"WebGrease", "Antlr3.Runtime", "JSON"
		};

		/// <summary>取得所有排除系統層級的dll所產生的型別</summary>
		public static List<Type> GetAllUserTypes
		{
			get
			{
				var assemblies = AssemblyUtils.GetAssembliesExcludeSystem().ToList();
				var assTargets = assemblies
				                 .Where
				                 ( a =>
					                   !a.GlobalAssemblyCache &&
					                   !a.FullName.StartsWith( "System" ) &&
					                   !a.FullName.StartsWith( "Microsoft" ) &&
					                   !a.FullName.StartsWith( "EntityFramework" ) &&
					                   !a.FullName.StartsWith( "DevExpress" ) &&
					                   !a.FullName.StartsWith( "XDesProc" ) &&
					                   !a.IsDynamic
				                 ).ToList();


				var list = new List<Type>();

				assTargets.RemoveAll( a => _ignoreAssemblies.Contains( a.GetName().Name ) );

				foreach ( var ass in assTargets )
					try
					{
						if ( ass.FullName.Contains( "PluginVC" ) )
						{
							var types = ass.GetTypes()
							               .Where( t => t.IsVisible )
							               .ToArray();
							list.AddRange( types );
						}
						else
						{
							var types = ass.GetTypes()
							               .Where
							               ( t =>
								                 //t.IsVisible && //Raz -> 不能加, 因為internal型別會死
								                 //!( t.Name.StartsWith( "_" ) ) &&
								                 !t.Name.StartsWith( "<" ) &&
								                 !t.FullName.StartsWith( "ProtoBuf" ) &&
								                 !t.FullName.StartsWith( "Microsoft" ) &&
								                 !t.FullName.StartsWith( "System" )
							               )
							               .ToArray();
							list.AddRange( types );
						}
					}
					catch ( Exception ex )
					{
						throw Err.Utility( "無法取得型別在[ " + ass.FullName + " ]", ex );
					}

				return list;
			}
		}

		/// <summary>取得當前AppDomain中所有UserDefine的Types</summary>
		public static List<Type> GetAllUserDefineTypes
		{
			get
			{
				var list = new List<Type>();
				var assemblies = AppDomain.CurrentDomain.GetAssemblies();
				var assesUserDefine = assemblies.Where( a => a.FullName.ToUpper().StartsWith( "UserDefine" ) );

				foreach ( var ass in assesUserDefine )
					try
					{
						var types = ass.GetTypes();
						list.AddRange( types );
					}
					catch ( Exception ex )
					{
						throw Err.Utility( "無法取得型別在[ " + ass.FullName + " ]", ex );
					}

				return list;
			}
		}
	}
}
