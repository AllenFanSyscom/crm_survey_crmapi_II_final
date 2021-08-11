using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using libs.Json.Linq;
using Shortener.Models;
using StackExchange.Redis;
using syscom;

namespace Shortener.Processor
{
	public static partial class Res
	{
		static readonly syscom.ILogger log = syscom.LogUtils.GetLogger("Proc:Res");

		// 固定型:
		//
		// key: su-p-{urlCode} => (hash)
		internal static void AddBy(String urlCode, Int32 domainId, Boolean isSecure, String urlDst, String dateS, String dateE)
		{
			Data.CheckStatus();

			if (!DateTime.TryParseExact(dateS, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var DateS)) throw new NotSupportedException($"dateS 無法識別的格式[{dateS}]");
			if (!DateTime.TryParseExact(dateE, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var DateE)) throw new NotSupportedException($"dateE 無法識別的格式[{dateE}]");

			var key = $"su-p-{urlCode}";

			var entries = new List<HashEntry>
			{
				new HashEntry( "DomainId", domainId ),
				new HashEntry( "IsSecure", isSecure ? "1" : "0" ),
				new HashEntry( "UrlDst", urlDst ),
				new HashEntry( "DateStart", DateS.ToString( "yyyy-MM-dd" ) ),
				new HashEntry( "DateEnd", DateE.ToString( "yyyy-MM-dd" ) ),
				//new HashEntry( "TotalHits", 0 )
			};

			var db = RedisHelper.DB;
			db.HashSet(key, entries.ToArray());

			//key結束時間
			//sam 加上結束日期23:59
			var DEnd = DateTime.Parse(DateE.ToString("yyyy-MM-dd") + " 23:59:59");
			log.Info($"[DEnd] [ {DEnd} ]");
			log.Info($"[DEnd - DateTime.Now] [ {DEnd - DateTime.Now} ]");
			db.KeyExpire(key, (DEnd - DateTime.Now));

			
			//------------------------------------------------------------------------------
			// BakJson
			//------------------------------------------------------------------------------
			var di = Data.ShareFolderInfo;

			//處理map檔
			var fiMap = new FileInfo(Path.Combine(di.FullName, $"{key}.surl"));
			if (fiMap.Exists) fiMap.Delete();

			//寫入
			var content = ConvertToJson(entries);
			FileUtils.AppendToFileBy(fiMap.FullName, content, false);
		}

		/// <summary>刪除固定式</summary>
		internal static void DelBy(String code)
		{
			var key = $"su-p-{code}";

			var db = RedisHelper.DB;
			db.KeyDelete(key);

			//------------------------------------------------------------------------------
			// BakJson
			//------------------------------------------------------------------------------
			var di = Data.ShareFolderInfo;

			//處理map檔
			var fiMap = new FileInfo(Path.Combine(di.FullName, $"{key}.surl"));
			if (fiMap.Exists) fiMap.Delete();
		}

		// 名單型:
		//
		// 索引檔: (hash)
		// su-index => { 'snS-snE': 'su-l-10000-19999' }
		//
		// 對應檔: (hash)
		// su-l-10000-19999 => (hash)
		// {
		//		'IsSecure': '0/1',			//固定項目
		//		'UrlDst': '{url}',			//固定項目
		// 		'7碼code': '{phone}'
		// }
		internal static void AddBy(IList<ListTypeMember> members, Int64 snS, Int64 snE, Int32 domainId, Boolean isSecure, String urlDst, String dateS, String dateE)
		{
			Data.CheckStatus();

			if (!DateTime.TryParseExact(dateS, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var DateS)) throw new NotSupportedException($"dateS 無法識別的格式[{dateS}]");
			if (!DateTime.TryParseExact(dateE, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var DateE)) throw new NotSupportedException($"dateE 無法識別的格式[{dateE}]");

			//資料結構
			var index_k = $"{snS}-{snE}";
			var index_v = $"su-l-{index_k}";
			

			var entries = ConvertToEntries(members);
			


			entries.Add(new HashEntry("DomainId", domainId));
			entries.Add(new HashEntry("IsSecure", isSecure ? "1" : "0"));
			entries.Add(new HashEntry("UrlDst", urlDst));
			entries.Add(new HashEntry("DateStart", DateS.ToString("yyyy-MM-dd")));
			entries.Add(new HashEntry("DateEnd", DateE.ToString("yyyy-MM-dd")));
			//entries.Add(new HashEntry("TotalHits", 0));

			//測試帳號
			entries.Add(new HashEntry($"{LogicUtils.ToStrSN(snE)}", "0988123456"));

			//------------------------------------------------------------------------------
			// redis
			//------------------------------------------------------------------------------
			var db = RedisHelper.DB;
			//index
			db.HashSet("su-index", new[] { new HashEntry(index_k, index_v) });

            //map
            //db.HashSet(index_v, entries.ToArray());
            var membercount = 0;
            foreach (var entrie in entries)
            {
                membercount++;
                db.HashSetAsync(index_v, entrie.Name, entrie.Value);
                if (membercount % 100000 == 0) log.Info($"[res][List] key[{index_v}] adding 共 [ {membercount} ]筆");
            }


            //key結束時間
            //sam 加上結束日期23:59
            var DEnd = DateTime.Parse(DateE.ToString("yyyy-MM-dd") + " 23:59:59");
			log.Info($"[DEnd] [ {DEnd} ]");
			log.Info($"[DEnd - DateTime.Now] [ {DEnd - DateTime.Now} ]");
			db.KeyExpire(index_v, (DEnd - DateTime.Now));


			//------------------------------------------------------------------------------
			// BakJson
			//------------------------------------------------------------------------------
			var di = Data.ShareFolderInfo;

			//處理index檔
			var fiIndex = new FileInfo(Path.Combine(di.FullName, "su-index.surl"));
			if (!fiIndex.Exists)
			{
				FileUtils.AppendToFileBy(fiIndex.FullName, $"{{ \"{index_k}\":\"{index_v}\" }}");
			}
			else
			{
				var txt = File.ReadAllText(fiIndex.FullName);
				var json = JObject.Parse(txt);

				json[index_k] = index_v;

				FileUtils.AppendToFileBy(fiIndex.FullName, json.ToString(), false);
			}

			//處理map檔
			var fiMap = new FileInfo(Path.Combine(di.FullName, $"{index_v}.surl"));
			if (fiMap.Exists) fiMap.Delete();

			var content = ConvertToJson(entries);
			FileUtils.AppendToFileBy(fiMap.FullName, content, false);
		}

		/// <summary>刪除名單式</summary>
		internal static void DelBy(Int64 snS, Int64 snE)
		{
			var index_k = $"{snS}-{snE}";
			var index_v = $"su-l-{snS}-{snE}";

			var db = RedisHelper.DB;
			db.HashDelete("su-index", index_k);

			db.KeyDelete(index_v);


			//------------------------------------------------------------------------------
			// BakJson
			//------------------------------------------------------------------------------
			var di = Data.ShareFolderInfo;

			//處理index檔
			var fiIndex = new FileInfo(Path.Combine(di.FullName, "su-index.surl"));
			if (fiIndex.Exists)
			{
				var txt = File.ReadAllText(fiIndex.FullName);
				var json = JObject.Parse(txt);

				json.Remove(index_k);

				FileUtils.AppendToFileBy(fiIndex.FullName, json.ToString(), false);
			}

			//處理map檔
			var fiMap = new FileInfo(Path.Combine(di.FullName, $"{index_v}.surl"));
			if (fiMap.Exists) fiMap.Delete();
		}

		//取的redis的 list count
		internal static long GetTotalHits(string key)
        {
			var db = RedisHelper.DB;
			var tmp = db.ListLength(key);

			return tmp;
        }
	}
}