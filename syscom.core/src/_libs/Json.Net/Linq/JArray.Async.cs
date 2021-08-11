#if HAVE_ASYNC

using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using libs.Json.Utilities;

namespace libs.Json.Linq
{
    public partial class JArray
    {
        /// <summary>
        /// Writes this token to a <see cref="JsonWriter"/> asynchronously.
        /// </summary>
        /// <param name="writer">A <see cref="JsonWriter"/> into which this method will write.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <param name="converters">A collection of <see cref="JsonConverter"/> which will be used when writing the token.</param>
        /// <returns>A <see cref="Task"/> that represents the asynchronous write operation.</returns>
        public override async Task WriteToAsync(JsonWriter writer, CancellationToken cancellationToken, params JsonConverter[] converters)
        {
            await writer.WriteStartArrayAsync(cancellationToken).ConfigureAwait(false);

            for (int i = 0; i < _values.Count; i++)
            {
                await _values[i].WriteToAsync(writer, cancellationToken, converters).ConfigureAwait(false);
            }

            await writer.WriteEndArrayAsync(cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Asynchronously loads a <see cref="JArray"/> from a <see cref="JsonReader"/>.
        /// </summary>
        /// <param name="reader">A <see cref="JsonReader"/> that will be read for the content of the <see cref="JArray"/>.
        /// If this is <c>null</c>, default load settings will be used.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None"/>.</param>
        /// <returns>A <see cref="Task{TResult}"/> representing the asynchronous load. The <see cref="Task{TResult}.Result"/> property contains the JSON that was read from the specified <see cref="JsonReader"/>.</returns>
        public new static Task<JArray> LoadAsync(JsonReader reader, CancellationToken cancellationToken = default(CancellationToken))
        {
            return LoadAsync(reader, null, cancellationToken);
        }

        /// <summary>
        /// Asynchronously loads a <see cref="JArray"/> from a <see cref="JsonReader"/>.
        /// </summary>
        /// <param name="reader">A <see cref="JsonReader"/> that will be read for the content of the <see cref="JArray"/>.</param>
        /// <param name="settings">The <see cref="JsonLoadSettings"/> used to load the JSON.
        /// If this is <c>null</c>, default load settings will be used.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None"/>.</param>
        /// <returns>A <see cref="Task{TResult}"/> representing the asynchronous load. The <see cref="Task{TResult}.Result"/> property contains the JSON that was read from the specified <see cref="JsonReader"/>.</returns>
        public new static async Task<JArray> LoadAsync(JsonReader reader, JsonLoadSettings settings, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (reader.TokenType == JsonToken.None)
            {
                if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                {
                    throw JsonReaderException.Create(reader, "Error reading JArray from JsonReader.");
                }
            }

            await reader.MoveToContentAsync(cancellationToken).ConfigureAwait(false);

            if (reader.TokenType != JsonToken.StartArray)
            {
                throw JsonReaderException.Create(reader, "Error reading JArray from JsonReader. Current JsonReader item is not an array: {0}".FormatWith(CultureInfo.InvariantCulture, reader.TokenType));
            }

            JArray a = new JArray();
            a.SetLineInfo(reader as IJsonLineInfo, settings);

            await a.ReadTokenFromAsync(reader, settings, cancellationToken).ConfigureAwait(false);

            return a;
        }
    }
}

#endif
