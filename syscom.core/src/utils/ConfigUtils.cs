using System;
using System.IO;
using System.Linq;
using System.Reflection;
using syscom.config;

namespace syscom
{
	public static class ConfigUtils
	{
		static readonly syscom.ILogger log = syscom.LogUtils.GetLoggerForCurrentClass();

		/// <summary>取得當前的Config</summary>
		public static Config Current => GetCurrentConfig( "Shortener.Common" );

		//========================================================================================================================
		// Config
		//========================================================================================================================

		/// <summary>取得 Web.config 或 App.config 組態物件, Json格式</summary>
		public static Config GetCurrentConfig( params String[] ignoreAssemblyNames )
		{
			var assemblyEntry = Assembly.GetEntryAssembly();		//ok win[exe,iis] mac[ut,exe]
			var assemblyExec = Assembly.GetExecutingAssembly();
			var assemblyLook = StackUtils.LookupExecutingAssembly( ignoreAssemblyNames );

			var assembly = assemblyEntry;
			if ( assembly == null ) assembly = assemblyLook;


			if( assembly == null ) throw new Exception( "Cannot Found Executing Assembly" );
			var assemblyName = assembly.GetName().Name;
			var nameLower = assemblyName.ToLower();


			var path = new DirectoryInfo( AppDomain.CurrentDomain.BaseDirectory );
			var files = path.GetFiles();

			Boolean fnMatch( FileInfo fi )
			{
				var name = fi.Name.ToLower();
				return name.Contains( "app.config" ) || name.Contains( "web.config" ) || name.Contains( "config.ini" ) || name.Contains( nameLower + ".config" ) || name.Contains( nameLower + ".exe.config" ) || name.Contains( nameLower + ".dll.config" );
			}

			var file = files.FirstOrDefault( fnMatch );

			log.Debug( $"[Config] assembly[{assemblyName}] file[{file.FullName}]" );

			if ( file == null )
			{
				throw new Exception( $"自動取得當前程式( {assemblyName} )之Config檔案失敗, 請確認您的執行路徑[{AppDomain.CurrentDomain.BaseDirectory}]有App.Config或Web.Config檔案, files[ {files.Select( f => f.Name ).ToArray().ToJson()} ]" );
			}

			return new Config( File.ReadAllText( file.FullName ) );
		}


		//========================================================================================================================
		// AppSetting
		//========================================================================================================================

		/// <summary>指定 Key 取得 config 內的 AppSetting 值 (若取不到值, 則傳回null)</summary>
		public static String? GetAppSettingOrNull( String key )
		{
			var config = Current;
			return config.AppSettings.ContainsKey( key ) ? config.AppSettings[key] : null;
		}

		/// <summary>指定 Key 取得 config 內的 AppSetting 值 (若取不到值, 則以defaultValue代替)</summary>
		public static TValue GetAppSettingOr<TValue>( String key, TValue defaultValue )
		{
			var value = GetAppSettingOrNull( key );
			return value == null ? defaultValue : value.GetValueOrDefault<TValue>();
		}

		/// <summary>指定key取得資料, 若異常或為null, 將回傳預設值</summary>
		public static TValue GetAppSettingOr<TValue>( String key, Func<String, TValue> fnConvert, TValue defaultValue )
		{
			var str = GetAppSettingOrNull( key );
			try
			{
				if ( str == null || String.IsNullOrEmpty( str ) ) return defaultValue;

				var converted = fnConvert( str );
				return converted == null ? defaultValue : converted;
			}
			catch { return defaultValue; }
		}


		/// <summary>指定 Key 取得 config 內的 AppSetting 值 (若取不到值, 則拋出Exception)</summary>
		public static TValue GetAppSettingOrException<TValue>( String key, String errMessage )
		{
			var config = Current;
			return GetAppSettingOrException<TValue>( config, key, errMessage );
		}

		/// <summary>指定 Key 取得 config 內的 AppSetting 值 (若取不到值, 則拋出Exception)</summary>
		public static TValue GetAppSettingOrException<TValue>( this Config config, String key, String errMessage )
		{
			var targetType = typeof( TValue );
			Object value;
			if ( !config.AppSettings.ContainsKey( key ) ) throw new Exception( errMessage.GetNullOr( m => m + ", " ) + "在Config設定檔中, 找不到指定的AppSetting key ：[" + key + "]" );
			var settingValue = config.AppSettings[key];
			try
			{
				value = settingValue.ConvertOrDefault<TValue>();
			}
			catch ( Exception ex )
			{
				throw new Exception( "指定的AppSetting Key：[" + key + "]取得值[ " + settingValue + " ], 但轉型為" + targetType.Name + "途中失敗.", ex );
			}

			return (TValue) value;
		}


		/// <summary>(已快取)指定 Key 取得 config 內的 AppSetting 值 (若取不到值, 則拋出Exception)</summary>
		public static String GetAppSettingOrException( String key )
		{
			var config = Current;
			return config.GetAppSettingOrException<String>( key, "" );
		}


		//========================================================================================================================
		// Connection String
		//========================================================================================================================

		/// <summary>(已快取)指定 Key 取得 config 內的 ConnectionString 值 (若取不到值, 則拋出Exception)</summary>
		public static String GetConnectionStringOrException( String key )
		{
			// if ( getSetting == null ) throw new Exception( "在WebConfig或AppConfig中, 找不到指定的 ConnectionString key ：[" + key + "]" );
			// return getSetting;

			//todo: impl
			return null;
		}


		/// <summary>(已快取)指定 Key 取得 config 內的 ConnectionString 值 (若取不到值,傳回null)</summary>
		public static String GetConnectionStringOrNull( String key )
		{
			//todo: impl
			return null;
		}
	}
}
