using System;
using System.Collections.Generic;
using libs.Json.Linq;
using Shortener.Models;
using StackExchange.Redis;
using syscom;

namespace Shortener.Processor
{
	public static partial class Res
	{
		public static List<HashEntry> ConvertToEntries( IList<ListTypeMember> members )
		{
			var entries = new List<HashEntry>( members.Count );

			foreach ( var m in members )
			{
				var k = m.TxtShort;
				var v = m.MobileNumber.Replace( "-", "" );

				entries.Add( new HashEntry( k, v ) );
			}

			return entries;
		}

		public static String ConvertToJson( IEnumerable<HashEntry> entries )
		{
			var json = new JObject();

			foreach ( var hashEntry in entries )
			{
				json[$"{hashEntry.Name}"] = $"{hashEntry.Value}";
			}

			return json.ToString();
		}
	}
}
