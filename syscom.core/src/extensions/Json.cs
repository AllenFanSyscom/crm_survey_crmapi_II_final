using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using libs.Json;
using libs.Json.Linq;
using libs.Json.Serialization;
using syscom;

namespace System
{
    public class DynamicIgnoreContractResolver : DefaultContractResolver
    {
        //public new static readonly DynamicIgnoreContractResolver Instance = new DynamicIgnoreContractResolver();
        readonly String[] _ignoreNames;

        public DynamicIgnoreContractResolver(params String[] ignorePropertyNames) { _ignoreNames = ignorePropertyNames; }

        protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
        {
            var property = base.CreateProperty(member, memberSerialization);

            if (_ignoreNames.Contains(property.PropertyName)) property.Ignored = true;

            return property;
        }

        protected override IList<JsonProperty> CreateProperties(Type type, MemberSerialization memberSerialization)
        {
            var properties = base.CreateProperties(type, memberSerialization);
            properties = properties.Where(p => !_ignoreNames.Contains(p.PropertyName)).ToList();

            return properties;
        }
    }


    public static partial class JsonExtensions
    {
        static JsonSerializerSettings _default;

        static JsonSerializerSettings DefaultSettings
        {
            get
            {
                if (_default == null)
                {
                    _default = new JsonSerializerSettings();
                    _default.Converters.Add(new IPAddressConverter());
                    _default.Converters.Add(new IPEndPointConverter());
                }

                return _default;
            }
        }

        //============================================================================================================ Json
        public static String ToJson(this Object item)
        {
            if (item == null) return "null";
            return JsonConvert.SerializeObject(item, DefaultSettings);
        }

        public static String ToJson(this Object item, params String[] ignorePropertyNames)
        {
            if (item == null) return "null";

            if (ignorePropertyNames != null && ignorePropertyNames.Length > 0)
            {
                var settings = new JsonSerializerSettings()
                {
                    ContractResolver = new DynamicIgnoreContractResolver(ignorePropertyNames)
                };

                return JsonConvert.SerializeObject(item, settings);
            }

            return item.ToJson();
        }

        public static TModel JsonTo<TModel>(this String json) where TModel : class { return JsonConvert.DeserializeObject<TModel>(json, DefaultSettings); }

        public static JObject JsonToJObject(this String json)
        {
            try
            {
                return JObject.Parse(json);
            }
            catch (Exception ex)
            {
                throw new Exception($"Cannot Deserialize, {ex.Message}, String[{json}]");
            }
        }
    }

    internal class IPAddressConverter : JsonConverter
    {
        public override Boolean CanConvert(Type objectType) { return objectType == typeof(IPAddress); }

        public override void WriteJson(JsonWriter writer, Object? value, JsonSerializer serializer)
        {
            var ip = (IPAddress)value;
            writer.WriteValue(ip.ToString());
        }

        public override Object ReadJson(JsonReader reader, Type objectType, Object? existingValue, JsonSerializer serializer)
        {
            var token = JToken.Load(reader);
            return IPAddress.Parse(token.Value<String>());
        }
    }

    internal class IPEndPointConverter : JsonConverter
    {
        public override Boolean CanConvert(Type objectType) { return objectType == typeof(IPEndPoint); }

        public override void WriteJson(JsonWriter writer, Object? value, JsonSerializer serializer)
        {
            var ep = (IPEndPoint)value;
            writer.WriteStartObject();
            writer.WritePropertyName("Address");
            serializer.Serialize(writer, ep.Address);
            writer.WritePropertyName("Port");
            writer.WriteValue(ep.Port);
            writer.WriteEndObject();
        }

        public override Object ReadJson(JsonReader reader, Type objectType, Object? existingValue, JsonSerializer serializer)
        {
            var jo = JObject.Load(reader);
            var address = jo["Address"].ToObject<IPAddress>(serializer);
            var port = jo["Port"].Value<Int32>();
            return new IPEndPoint(address, port);
        }
    }
}
