using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using libs.Redis;
using syscom;

namespace syscom
{
	public class RedisKeyInServer
	{
		public EndPoint EndPoint { get; internal set; }
		public RedisKey Key { get; internal set; }
		public RedisKeyInServer() { }

		public RedisKeyInServer( EndPoint endpoint, RedisKey key )
		{
			EndPoint = endpoint;
			Key = key;
		}
	}

	/// <summary>RedisUtils: 預設會取得AppSettings中的Redis設定值, 若取不到會使用localhost</summary>
	public static class RedisUtils
	{
		public static readonly String BaseConnectionString;
		static RedisUtils() { BaseConnectionString = ConfigUtils.GetAppSettingOr( "Redis", "localhost:6379" ); }

		/// <summary>取得Redis的連線, 未傳入連線字串將使用Config中的Redis連線, 若config未設定則回傳本機6379連線</summary>
		public static ConnectionMultiplexer CreateNew( String connectionString = null )
		{
			if ( String.IsNullOrEmpty( connectionString ) ) { connectionString = BaseConnectionString; }

			if ( String.IsNullOrEmpty( connectionString ) ) throw new Exception( "[Redis] not have connectionString" );
			return ConnectionMultiplexer.Connect( connectionString );
		}

		public static void CheckStatus()
		{
			try
			{
				using ( var conn = CreateNew() )
				{
					var db = conn.GetDatabase();
					db.KeyExists( "test" );
				}
			}
			catch( Exception ex )
			{
				throw new Exception( $"Redis連線異常, { ex.Message }", ex );
			}
		}

		public static IServer GetServer( this ConnectionMultiplexer conn )
		{
			var endpoint = conn.GetEndPoints().FirstOrDefault();
			if ( endpoint == null ) throw new Exception( "[Redis] 無法取得IServer, 找不到任何的Endpoint" );

			return conn.GetServer( endpoint );
		}

		/// <summary>取得所有Server的Keys, 注意, 此動作在keys很多的情況下會很花費時間</summary>
		public static List<RedisKeyInServer> GetAllServerKeys( this ConnectionMultiplexer conn )
		{
			var allKeys = new List<RedisKeyInServer>();

			var endpoints = conn.GetEndPoints();

			foreach ( var endpoint in endpoints )
			{
				var server = conn.GetServer( endpoint );

				var keys = server.Keys();

				foreach ( var key in keys )
				{
					allKeys.Add( new RedisKeyInServer( endpoint, key ) );
				}
			}

			return allKeys;
		}
	}
}
