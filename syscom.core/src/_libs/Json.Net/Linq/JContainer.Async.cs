#if HAVE_ASYNC

using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using libs.Json.Utilities;

namespace libs.Json.Linq
{
    public abstract partial class JContainer
    {
        internal async Task ReadTokenFromAsync(JsonReader reader, JsonLoadSettings options, CancellationToken cancellationToken = default(CancellationToken))
        {
            ValidationUtils.ArgumentNotNull(reader, nameof(reader));
            int startDepth = reader.Depth;

            if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                throw JsonReaderException.Create(reader, "Error reading {0} from JsonReader.".FormatWith(CultureInfo.InvariantCulture, GetType().Name));
            }

            await ReadContentFromAsync(reader, options, cancellationToken).ConfigureAwait(false);

            if (reader.Depth > startDepth)
            {
                throw JsonReaderException.Create(reader, "Unexpected end of content while loading {0}.".FormatWith(CultureInfo.InvariantCulture, GetType().Name));
            }
        }

        private async Task ReadContentFromAsync(JsonReader reader, JsonLoadSettings settings, CancellationToken cancellationToken = default(CancellationToken))
        {
            IJsonLineInfo lineInfo = reader as IJsonLineInfo;

            JContainer parent = this;

            do
            {
                if ((parent as JProperty)?.Value != null)
                {
                    if (parent == this)
                    {
                        return;
                    }

                    parent = parent.Parent;
                }

                switch (reader.TokenType)
                {
                    case JsonToken.None:
                        // new reader. move to actual content
                        break;
                    case JsonToken.StartArray:
                        JArray a = new JArray();
                        a.SetLineInfo(lineInfo, settings);
                        parent.Add(a);
                        parent = a;
                        break;

                    case JsonToken.EndArray:
                        if (parent == this)
                        {
                            return;
                        }

                        parent = parent.Parent;
                        break;
                    case JsonToken.StartObject:
                        JObject o = new JObject();
                        o.SetLineInfo(lineInfo, settings);
                        parent.Add(o);
                        parent = o;
                        break;
                    case JsonToken.EndObject:
                        if (parent == this)
                        {
                            return;
                        }

                        parent = parent.Parent;
                        break;
                    case JsonToken.StartConstructor:
                        JConstructor constructor = new JConstructor(reader.Value.ToString());
                        constructor.SetLineInfo(lineInfo, settings);
                        parent.Add(constructor);
                        parent = constructor;
                        break;
                    case JsonToken.EndConstructor:
                        if (parent == this)
                        {
                            return;
                        }

                        parent = parent.Parent;
                        break;
                    case JsonToken.String:
                    case JsonToken.Integer:
                    case JsonToken.Float:
                    case JsonToken.Date:
                    case JsonToken.Boolean:
                    case JsonToken.Bytes:
                        JValue v = new JValue(reader.Value);
                        v.SetLineInfo(lineInfo, settings);
                        parent.Add(v);
                        break;
                    case JsonToken.Comment:
                        if (settings != null && settings.CommentHandling == CommentHandling.Load)
                        {
                            v = JValue.CreateComment(reader.Value.ToString());
                            v.SetLineInfo(lineInfo, settings);
                            parent.Add(v);
                        }
                        break;
                    case JsonToken.Null:
                        v = JValue.CreateNull();
                        v.SetLineInfo(lineInfo, settings);
                        parent.Add(v);
                        break;
                    case JsonToken.Undefined:
                        v = JValue.CreateUndefined();
                        v.SetLineInfo(lineInfo, settings);
                        parent.Add(v);
                        break;
                    case JsonToken.PropertyName:
                        string propertyName = reader.Value.ToString();
                        JProperty property = new JProperty(propertyName);
                        property.SetLineInfo(lineInfo, settings);
                        JObject parentObject = (JObject)parent;
                        // handle multiple properties with the same name in JSON
                        JProperty existingPropertyWithName = parentObject.Property(propertyName);
                        if (existingPropertyWithName == null)
                        {
                            parent.Add(property);
                        }
                        else
                        {
                            existingPropertyWithName.Replace(property);
                        }
                        parent = property;
                        break;
                    default:
                        throw new InvalidOperationException("The JsonReader should not be on a token of type {0}.".FormatWith(CultureInfo.InvariantCulture, reader.TokenType));
                }
            } while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false));
        }
    }
}

#endif
