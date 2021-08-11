using syscom.data.UnitOfWork;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

#if netcore
using Microsoft.Data.SqlClient;
using Microsoft.Data.SqlClient.Server;
#else
using System.Data.SqlClient;
#endif


namespace syscom.data
{
	/// <summary>提供DataBase相關存取，以Extensions方式的類別</summary>
	public static partial class DB
	{
		public static DataBaseType GetConnectorType( String typeName )
		{
			typeName = typeName.ToLower();

			switch( typeName )
			{
				case "oracle": return DataBaseType.Oracle;
				case "mysql":
				case "maria":
				case "mariadb":
					return DataBaseType.MySql;

				default:
					return DataBaseType.MsSql;
			}
		}

		public static DataBaseType GetConnectionTypeFromConnectionString( String connStr )
		{
			//todo: 未完成
			return DataBaseType.MsSql;
		}

		//Todo:[Raz] 未完成, 這邊要用反射解決, 如果不想include太多dll的話
		public static Type GetConnectorType( this DataBaseType type )
		{
			Type? connType = null;
			switch ( type )
			{
				case DataBaseType.MsSql:
					connType = typeof( SqlConnection );
					break;
				case DataBaseType.Oracle:
					break;
				case DataBaseType.Sqlite:
					break;
				default: throw Err.Utility( $"資料庫類型[ {type} ]目前尚未實作對應的Connection實例型別" );
			}

			return connType;
		}

		public static IDbConnection CreateConnectionBy( String connectionString )
		{
			var type = GetConnectionTypeFromConnectionString( connectionString );
			return CreateConnectionBy( type, connectionString );
		}

		public static IDbConnection CreateConnectionBy( DataBaseType type, String connectionString )
		{
			var connectionType = GetConnectorType( type );
			IDbConnection conn;
			try
			{
				var newConn = Activator.CreateInstance( connectionType, connectionString );
				conn = newConn as IDbConnection ?? throw new Exception( $"無法實例化型別[ {connectionType} ]" );
				if ( conn == null ) throw Err.Utility( $"無法轉換[{connectionType}]為IDbConnection類型" );
			}
			catch ( Exception ex )
			{
				throw Err.Utility( $"實例化資料庫連線 型別[ {type} ] 連線字串[ {connectionString} ] 時發生異常, " + ex.Message, ex );
			}

			return conn;
		}
	}
}
