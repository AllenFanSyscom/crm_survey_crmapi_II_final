using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using syscom;
using syscom.config;
using syscom.arch.ioc.impls;

namespace syscom
{
	public static partial class SyscomUtils
	{
		// /// <summary>核心的版本號碼</summary>
		// public static GenericVersionInfo CoreVersionInfo { get; private set; }
		//
		// /// <summary>當基礎類別初始化時, 若無法使用預設DB連線器, 則自動寫入Log警示</summary>
		// public static Boolean AutoLogWarningForDefaultDataBaseConnectionAbility { get; set; }
		//
		// static SyscomUtils()
		// {
		// 	AutoLogWarningForDefaultDataBaseConnectionAbility = true;
		// 	CoreVersionInfo = typeof( SyscomUtils ).Assembly.GetDllVersionInfo();
		// }
		//
		//
		// internal static BaseConfig _BaseConfig_Current;
		// internal static BaseConfig _BaseConfig_Machine;
		//
		// internal static ConfigOfSyscom _SyscomConfig_Current;
		// internal static ConfigOfSyscom _SyscomConfig_Machine;
		//
		// public static List<BaseConfig> GetCurrentConfigs()
		// {
		// 	var list = new List<BaseConfig>();
		//
		// 	var currentConfig = ConfigUtils.GetCurrentConfig();
		// 	if ( currentConfig != null )
		// 	{
		// 		list.Add( currentConfig );
		// 		_BaseConfig_Current = currentConfig;
		// 		_SyscomConfig_Current = _BaseConfig_Current.GetSectionOfSyscom( false );
		// 		if ( _SyscomConfig_Current != null )
		// 		{
		// 			if ( String.IsNullOrEmpty( _SyscomConfig_Current.Environment.MachineConfigPath ) )
		// 			{
		// 				list.Add( currentConfig );
		// 				return list;
		// 			}
		//
		// 			var exist = File.Exists( _SyscomConfig_Current.Environment.MachineConfigPath );
		// 			if ( !exist ) throw new ConfigurationErrorsException( "在當前Config中有找到MachineConfigPath[" + _SyscomConfig_Current.Environment.MachineConfigPath + "], 但該檔案並不存在於該路徑" );
		//
		// 			//Ps. if both config exist, first add machine config
		// 			_BaseConfig_Machine = ConfigUtils.GetConfigBy( _SyscomConfig_Current.Environment.MachineConfigPath );
		// 			_SyscomConfig_Machine = _BaseConfig_Machine.GetSectionOfSyscom( false );
		// 			list.Add( _BaseConfig_Machine );
		// 		}
		// 		else
		// 		{
		// 			//"請確認當前Config檔是否有定義<Syscom>區段及設定好<ConfigSections>段區之設定"
		// 		}
		// 	}
		//
		// 	return list;
		// }
		//
		//
		// /// <summary>取得Config中的Syscom區段</summary>
		// public static ConfigOfSyscom GetSectionOfSyscom( this BaseConfig config, Boolean throwNotFound = true )
		// {
		// 	var section = config.GetSectionOrNullBy<ConfigOfSyscom>( "syscom", false );
		// 	if ( section == null && throwNotFound ) throw new ConfigurationErrorsException( "在Config中找不到Syscom區段,請確認已在Config中設定了<syscom>區段" );
		// 	return section;
		// }
		//
		//
		// public static BaseConfig GetCurrentConfigFile( Boolean throwNotFound = true )
		// {
		// 	if ( _BaseConfig_Current != null ) return _BaseConfig_Current;
		// 	var configs = GetCurrentConfigs();
		// 	if ( _BaseConfig_Current == null && throwNotFound ) throw new ConfigurationErrorsException( "無法找到Config檔案" );
		// 	return _BaseConfig_Current;
		// }
		//
		// /// <summary>自動識別並取出本地Config之Syscom區段</summary>
		// public static ConfigOfSyscom GetCurrentSyscomSection( Boolean throwNotFound = true )
		// {
		// 	if ( _SyscomConfig_Current != null ) return _SyscomConfig_Current;
		// 	var configs = GetCurrentConfigs();
		// 	if ( _SyscomConfig_Current == null && throwNotFound ) throw new ConfigurationErrorsException( "在Config中找不到Syscom區段,請確認已在Config中設定了<syscom>區段" );
		// 	return _SyscomConfig_Current;
		// }
		//
		// public static BaseConfig GetMachineSyscomConfigFile( Boolean throwNotFound = true )
		// {
		// 	if ( _BaseConfig_Machine != null ) return _BaseConfig_Machine;
		// 	var configs = GetCurrentConfigs();
		// 	if ( _BaseConfig_Machine == null && throwNotFound ) throw new ConfigurationErrorsException( "無法找到 Machine syscom.configConfig 檔案, 請確認您已在Config中設定了對應路徑(Syscom->Environment->MachineConfigPath)" );
		// 	return _BaseConfig_Machine;
		// }
		//
		// /// <summary>自動識別並取出MachineConfig</summary>
		// public static ConfigOfSyscom GetMachineSyscomSection( Boolean throwNotFound = true )
		// {
		// 	if ( _SyscomConfig_Machine != null ) return _SyscomConfig_Machine;
		// 	var configs = GetCurrentConfigs();
		// 	if ( _SyscomConfig_Machine == null && throwNotFound ) throw new ConfigurationErrorsException( "無法找到 Machine syscom.configConfig 檔案, 請確認您已在Config中設定了對應路徑(Syscom->Environment->MachineConfigPath)" );
		// 	return _SyscomConfig_Machine;
		// }
	}

	public static partial class SyscomUtils
	{
		static readonly Object providerMutex = new Object();
		static IIocProvider _provider;

		public static IIocProvider IoC
		{
			get
			{
				lock ( providerMutex )
				{
					if ( _provider != null ) return _provider;
					_provider = new DryIocProvider();
				}

				return _provider;
			}
		}
	}
}
