#if HAVE_ASYNC

using System.Threading;
using System.Threading.Tasks;
using libs.Json.Utilities;

namespace libs.Json.Linq
{
    public partial class JTokenWriter
    {
        // This is the only method that can benefit from Task-based asynchronicity, and that only when
        // the reader provides it.
        internal override Task WriteTokenAsync(JsonReader reader, bool writeChildren, bool writeDateConstructorAsDate, bool writeComments, CancellationToken cancellationToken)
        {
            // Since JTokenReader is a common target (and with an optimised path) and since it can't
            // read truly async, catch that case.
            if (reader is JTokenReader)
            {
                WriteToken(reader, writeChildren, writeDateConstructorAsDate, writeComments);
                return AsyncUtils.CompletedTask;
            }

            return WriteTokenSyncReadingAsync(reader, cancellationToken);
        }
    }
}

#endif
