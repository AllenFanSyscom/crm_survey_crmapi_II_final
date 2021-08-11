#if HAVE_ASYNC

using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace libs.Json.Linq
{
    public partial class JRaw
    {
        /// <summary>
        /// Asynchronously creates an instance of <see cref="JRaw"/> with the content of the reader's current token.
        /// </summary>
        /// <param name="reader">The reader.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None"/>.</param>
        /// <returns>A <see cref="Task{TResult}"/> representing the asynchronous creation. The <see cref="Task{TResult}.Result"/>
        /// property returns an instance of <see cref="JRaw"/> with the content of the reader's current token.</returns>
        public static async Task<JRaw> CreateAsync(JsonReader reader, CancellationToken cancellationToken = default(CancellationToken))
        {
            using (StringWriter sw = new StringWriter(CultureInfo.InvariantCulture))
            using (JsonTextWriter jsonWriter = new JsonTextWriter(sw))
            {
                await jsonWriter.WriteTokenSyncReadingAsync(reader, cancellationToken).ConfigureAwait(false);

                return new JRaw(sw.ToString());
            }
        }
    }
}

#endif
