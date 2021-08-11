using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using syscom;
using syscom.data;
using syscom.data.UnitOfWork;

namespace syscom.data
{
	/// <summary>提供DataBase相關存取的類別</summary>
	public static partial class DB
	{
		internal const String KEY_ModuleName = @"DB";

		static readonly String DefaultConnectionString;

		static DB()
		{
			DefaultConnectionString = ConfigUtils.Current.ConnectionStrings[0].Replace("Passingword", "Password"); ;
		}

		
		//==========================================================================================
		// Connection相關
		//==========================================================================================

		/// <summary>
		/// 使用預設的連線字串進行實例化連線 (注意, 若設定檔不完整或對應不到將拋出Exception)
		/// </summary>
		public static IDbConnection CreateConnection()
		{
			if ( String.IsNullOrEmpty( DefaultConnectionString ) ) throw Err.Module( KEY_ModuleName, "找不到預設連線字串, 請確認設定檔是否正確." );

			return CreateConnectionBy( DefaultConnectionString );
		}

		public static IDbConnection CreateConnection(string connStr)
		{
			if (String.IsNullOrEmpty(connStr)) throw Err.Module(KEY_ModuleName, "找不到連線字串, 請確認設定檔是否正確.");

			return CreateConnectionBy(connStr);
		}


		//==========================================================================================
		// DbUnitOfWork相關
		//==========================================================================================

		public static IDbUnitOfWork CreateUnitOfWork()
		{
			var conn = CreateConnection();
			var uow = new GenericDbUnitOfWork( conn );
			uow.OnDispose += () => { conn.Close(); };
			return uow;
		}

		public static IDbUnitOfWork CreateUnitOfWork(string connStr)
		{
			var conn = CreateConnection(connStr);
			var uow = new GenericDbUnitOfWork(conn);
			uow.OnDispose += () => { conn.Close(); };
			return uow;
		}

		/// <summary>
		/// 使用預設的ConnectionString建立DbUnitOfWork
		/// <para>注意, 若過程中發生對應不到、無法轉型等錯誤將拋出Exception</para>
		/// </summary>
		public static IDbUnitOfWork CreateUnitOfWork( Boolean useTransaction = false )
		{
			var conn = CreateConnection();
			var uow = new GenericDbUnitOfWork( conn, useTransaction );
			uow.OnDispose += () => { conn.Close(); };
			return uow;
		}

		public static IDbUnitOfWork CreateUnitOfWork( IDbConnection connection )
		{
			var uow = new GenericDbUnitOfWork( connection );
			return uow;
		}
	}
}
