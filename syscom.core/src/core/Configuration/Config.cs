using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using libs.Json;
using libs.Json.Linq;

namespace syscom.config
{
	public class Config
	{
		protected static readonly ILogger log = syscom.LogUtils.GetLoggerForCurrentClass();

		public readonly Dictionary<String, String> AppSettings = new Dictionary<String, String>();
		public readonly Dictionary<String, String> ConnTypes = new Dictionary<String, String>();
		public readonly Dictionary<String, String> ConnStrings = new Dictionary<String, String>();
		public List<String> ConnectionStrings => ConnStrings.Values.ToList();

		public JObject Json { get; private set; }

		public Config( String content )
		{
			if ( content.Contains( "<?xml" ) || content.Contains( "<configuration>" ) )
			{
				var doc = new XmlDocument();
				doc.LoadXml( content );
				var jsonText = JsonConvert.SerializeXmlNode( doc );

				Json = JObject.Parse( jsonText );
			}
			else
			{
				Json = JObject.Parse( content );
			}

			this.ParseJson();
		}

		void ParseJson()
		{
			var conf = Json["configuration"];

			//Log.LogFile( $"conf: {conf.ToJson()}" );

			var conns = conf.GetNullOr( con => con["connectionStrings"].GetNullOr( coSet => coSet["add"] ) );
			if ( conns != null )
			{
				if ( conns.GetType() == typeof( JArray ) )
				{
					foreach ( var item in conns )
					{
						var key = item["@name"].GetNullOr( v => v.ToString() );
						var val = item["@connectionString"].GetNullOr( v => v.ToString() );
						var typ = item["@providerName"].HasValues ? item["@providerName"].Value<String>() : null;
						if ( typ != null ) ConnTypes[key] = typ;
						ConnStrings[key] = val;
					}
				}
				else
				{
					var key = conns["@name"].GetNullOr( v => v.ToString() );
					var val = conns["@connectionString"].GetNullOr( v => v.ToString() );
					var typ = conns["@providerName"].HasValues ? conns["@providerName"].Value<String>() : null;
					if ( typ != null ) ConnTypes[key] = typ;

					ConnStrings[key] = val;
				}
			}

			var sets = conf.GetNullOr( con => con["appSettings"].GetNullOr( apSet => apSet["add"] ) );
			if ( sets != null )
			{
				if ( sets.GetType() == typeof( JArray ) )
				{
					foreach ( var item in sets )
					{
						var key = item["@key"].GetNullOr( v => v.ToString() );
						var val = item["@value"].GetNullOr( v => v.ToString() );

						AppSettings[key] = val;
					}
				}
				else
				{
					var key = sets["@key"].GetNullOr( v => v.ToString() );
					var val = sets["@value"].GetNullOr( v => v.ToString() );

					AppSettings[key] = val;
				}
			}

			//Log.LogFile( $"appSettings: { AppSettings.ToJson() }" );
		}
	}
}
