using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Shortener.Models;
using StackExchange.Redis;

namespace Shortener.Processor
{
	public class ALogsHolder
	{
		public String Key;
		public List<AnalyzeRecord> Records;
		public Action OnSuccess;
		public Action OnFailed;
	}

	public class ALog
	{
		static readonly syscom.ILogger log = syscom.LogUtils.GetLogger( "ALog" );
		public static readonly String PathOfStoreError = Path.Combine( Data.ShareFolderInfo.FullName, "alog-failed.log" );
		//redis 有效token
		static readonly RedisValue Token = Environment.MachineName;
		public static ALogsHolder FetchNextLogs()
		{
			ALogsHolder logs = null;
			log.Info( "Trying Fetch NextLogs..." );

			logs = FetchFromRedis();
			if ( logs == null ) logs = FetchFromShareFolder();

			if ( logs != null ) log.Info( $"Fetched Logs [{logs.Key}] count[{ logs.Records.Count }]" );

			return logs;
		}

		static ALogsHolder FetchFromShareFolder()
		{
			var file = Data.ShareFolderInfo.GetFiles( "su-r-*", SearchOption.TopDirectoryOnly ).FirstOrDefault();
			if ( file == null ) return null;

			var holder = new ALogsHolder
			{
				Key = file.Name,
				Records = new List<AnalyzeRecord>(),
			};

			holder.OnSuccess += () =>
			{
				File.Delete( file.FullName );
			};
			holder.OnFailed += () => { };


			var lines = File.ReadAllLines( file.FullName );

			using ( var WriterOfError = new StreamWriter( ALog.PathOfStoreError, true, Encoding.UTF8 ) )
			{
				for ( var idx = 0; idx < lines.Length; idx++ )
				{
					var value = lines[idx];

					log.Info( $"[alog] value: {value}" );
					if ( String.IsNullOrWhiteSpace( value ) ) continue;

					var values = value.SplitBy( "||" );

					// parse datetime
					var parsed = DateTime.TryParseExact( values[0], "yyyyMMdd-HHmmssfff", CultureInfo.InvariantCulture, DateTimeStyles.None, out var datetime );
					if ( !parsed )
					{
						WriterOfError.WriteLine( value );
						continue;
					}

					try
					{
						var ip = values[1];                // ip
						var ag = values[2];                // agent
						var dm = values[3];                // domain
						var co = values[4];                // code
						var ok = values[5] == "1" ? 1 : 0; // is success

						var record = new AnalyzeRecord
						{
							IP = ip, Agent = ag, Code = co, Domain = dm, Success = ok, DateTime = datetime
						};
						holder.Records.Add( record );
					}
					catch ( Exception ex )
					{
						log.Error( $"資料轉換失敗({ex.Message}), 存入StoreError中, value[{value}]" );
						WriterOfError.WriteLine( value );
					}
				}
			}

			if ( holder.Records.Count <= 0 )
			{
				log.Warn( $"[alog] not any validate records from [{file.FullName}]" );
				try
				{
					File.Delete( file.FullName );
				}
				catch ( Exception ex )
				{
					log.Error( $"刪除檔案[{file.FullName}]異常", ex );
				}

				return null;
			}


			return holder;
		}

		static ALogsHolder FetchFromRedis()
		{
			try
			{
				//redis key 有效時間
				var tp = TimeSpan.MaxValue;
				
				var key = RedisHelper.GetNeedProcessRecordKey();
				if ( String.IsNullOrEmpty( key ) ) return null;

				var holder = new ALogsHolder
				{
					Key = key,
					Records = new List<AnalyzeRecord>(),
				};

				var db = RedisHelper.DB;


				holder.OnSuccess += () =>
				{
					RedisHelper.DB.KeyDelete(key);
				};
				holder.OnFailed += () =>
				{
					//RedisHelper.DB.KeyDelete(key);
				};

				//加上key lock
				log.Info($"from key[{key}] Token[{Token}] tp[{tp}]");
				
					var count = db.ListLength(key);

					log.Info("====================================================================================");
					log.Info($"from key[{key}] count[{count}]");

					
						using (var WriterOfError = new StreamWriter(ALog.PathOfStoreError, true, Encoding.UTF8))
						{
							for (var idx = 0; idx < count; idx++)
							{
								var value = db.ListLeftPop(key).ToString();

								log.Info($"[alog] value: {value}");
								if (String.IsNullOrWhiteSpace(value)) continue;

								var values = value.SplitBy("||");

								// parse datetime
								var parsed = DateTime.TryParseExact(values[0], "yyyyMMdd-HHmmssfff", CultureInfo.InvariantCulture, DateTimeStyles.None, out var datetime);
								if (!parsed)
								{
									WriterOfError.WriteLine(value);
									continue;
								}

								try
								{
									var ip = values[1];                // ip
									var ag = values[2];                // agent
									var dm = values[3];                // domain
									var co = values[4];                // code
									var ok = values[5] == "1" ? 1 : 0; // is success

									var record = new AnalyzeRecord
									{
										IP = ip,
										Agent = ag,
										Code = co,
										Domain = dm,
										Success = ok,
										DateTime = datetime
									};
									holder.Records.Add(record);
								}
								catch (Exception ex)
								{
									log.Error($"資料轉換失敗({ex.Message}), 存入StoreError中, value[{value}]");
									WriterOfError.WriteLine(value);
								}
							}

						}
					
					
					if (holder.Records.Count <= 0)
					{
						try
						{
							RedisHelper.DB.KeyDelete(key);
						}
						catch (Exception ex)
						{
							log.Error($"刪除Redis異常", ex);
						}

						return null;
					}
					
				
				return holder;

			}
			catch ( Exception ex )
			{
				log.Error( $"無法獲取Redis的Logs資料, {ex.Message}", ex );
				return null;
			}
		}
	}
}
