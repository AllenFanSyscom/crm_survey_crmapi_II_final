#if HAVE_ASYNC

using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using libs.Json.Utilities;

namespace libs.Json.Linq
{
    public partial class JProperty
    {
        /// <summary>
        /// Writes this token to a <see cref="JsonWriter"/> asynchronously.
        /// </summary>
        /// <param name="writer">A <see cref="JsonWriter"/> into which this method will write.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <param name="converters">A collection of <see cref="JsonConverter"/> which will be used when writing the token.</param>
        /// <returns>A <see cref="Task"/> that represents the asynchronous write operation.</returns>
        public override Task WriteToAsync(JsonWriter writer, CancellationToken cancellationToken, params JsonConverter[] converters)
        {
            Task task = writer.WritePropertyNameAsync(_name, cancellationToken);
            if (task.Status == TaskStatus.RanToCompletion)
            {
                return WriteValueAsync(writer, cancellationToken, converters);
            }

            return WriteToAsync(task, writer, cancellationToken, converters);
        }

        private async Task WriteToAsync(Task task, JsonWriter writer, CancellationToken cancellationToken, params JsonConverter[] converters)
        {
            await task.ConfigureAwait(false);

            await WriteValueAsync(writer, cancellationToken, converters).ConfigureAwait(false);
        }

        private Task WriteValueAsync(JsonWriter writer, CancellationToken cancellationToken, JsonConverter[] converters)
        {
            JToken value = Value;
            return value != null
                ? value.WriteToAsync(writer, cancellationToken, converters)
                : writer.WriteNullAsync(cancellationToken);
        }

        /// <summary>
        /// Asynchronously loads a <see cref="JProperty"/> from a <see cref="JsonReader"/>.
        /// </summary>
        /// <param name="reader">A <see cref="JsonReader"/> that will be read for the content of the <see cref="JProperty"/>.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None"/>.</param>
        /// <returns>A <see cref="Task{TResult}"/> representing the asynchronous creation. The <see cref="Task{TResult}.Result"/>
        /// property returns a <see cref="JProperty"/> that contains the JSON that was read from the specified <see cref="JsonReader"/>.</returns>
        public new static Task<JProperty> LoadAsync(JsonReader reader, CancellationToken cancellationToken = default(CancellationToken))
        {
            return LoadAsync(reader, null, cancellationToken);
        }

        /// <summary>
        /// Asynchronously loads a <see cref="JProperty"/> from a <see cref="JsonReader"/>.
        /// </summary>
        /// <param name="reader">A <see cref="JsonReader"/> that will be read for the content of the <see cref="JProperty"/>.</param>
        /// <param name="settings">The <see cref="JsonLoadSettings"/> used to load the JSON.
        /// If this is <c>null</c>, default load settings will be used.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None"/>.</param>
        /// <returns>A <see cref="Task{TResult}"/> representing the asynchronous creation. The <see cref="Task{TResult}.Result"/>
        /// property returns a <see cref="JProperty"/> that contains the JSON that was read from the specified <see cref="JsonReader"/>.</returns>
        public new static async Task<JProperty> LoadAsync(JsonReader reader, JsonLoadSettings settings, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (reader.TokenType == JsonToken.None)
            {
                if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                {
                    throw JsonReaderException.Create(reader, "Error reading JProperty from JsonReader.");
                }
            }

            await reader.MoveToContentAsync(cancellationToken).ConfigureAwait(false);

            if (reader.TokenType != JsonToken.PropertyName)
            {
                throw JsonReaderException.Create(reader, "Error reading JProperty from JsonReader. Current JsonReader item is not a property: {0}".FormatWith(CultureInfo.InvariantCulture, reader.TokenType));
            }

            JProperty p = new JProperty((string)reader.Value);
            p.SetLineInfo(reader as IJsonLineInfo, settings);

            await p.ReadTokenFromAsync(reader, settings, cancellationToken).ConfigureAwait(false);

            return p;
        }
    }
}

#endif
