#if HAVE_ASYNC

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using libs.Json.Utilities;

namespace libs.Json
{
    public abstract partial class JsonReader
    {
        /// <summary>
        /// Asynchronously reads the next JSON token from the source.
        /// </summary>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None"/>.</param>
        /// <returns>A <see cref="Task{TResult}"/> that represents the asynchronous read. The <see cref="Task{TResult}.Result"/>
        /// property returns <c>true</c> if the next token was read successfully; <c>false</c> if there are no more tokens to read.</returns>
        /// <remarks>The default behaviour is to execute synchronously, returning an already-completed task. Derived
        /// classes can override this behaviour for true asychronousity.</remarks>
        public virtual Task<bool> ReadAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            return cancellationToken.CancelIfRequestedAsync<bool>() ?? Read().ToAsync();
        }

        /// <summary>
        /// Asynchronously skips the children of the current token.
        /// </summary>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None"/>.</param>
        /// <returns>A <see cref="Task"/> that represents the asynchronous operation.</returns>
        /// <remarks>The default behaviour is to execute synchronously, returning an already-completed task. Derived
        /// classes can override this behaviour for true asychronousity.</remarks>
        public async Task SkipAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            if (TokenType == JsonToken.PropertyName)
            {
                await ReadAsync(cancellationToken).ConfigureAwait(false);
            }

            if (JsonTokenUtils.IsStartToken(TokenType))
            {
                int depth = Depth;

                while (await ReadAsync(cancellationToken).ConfigureAwait(false) && depth < Depth)
                {
                }
            }
        }

        internal async Task ReaderReadAndAssertAsync(CancellationToken cancellationToken)
        {
            if (!await ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                throw CreateUnexpectedEndException();
            }
        }

        /// <summary>
        /// Asynchronously reads the next JSON token from the source as a <see cref="Nullable{T}"/> of <see cref="bool"/>.
        /// </summary>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None"/>.</param>
        /// <returns>A <see cref="Task{TResult}"/> that represents the asynchronous read. The <see cref="Task{TResult}.Result"/>
        /// property returns the <see cref="Nullable{T}"/> of <see cref="bool"/>. This result will be <c>null</c> at the end of an array.</returns>
        /// <remarks>The default behaviour is to execute synchronously, returning an already-completed task. Derived
        /// classes can override this behaviour for true asychronousity.</remarks>
        public virtual Task<bool?> ReadAsBooleanAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            return cancellationToken.CancelIfRequestedAsync<bool?>() ?? Task.FromResult(ReadAsBoolean());
        }

        /// <summary>
        /// Asynchronously reads the next JSON token from the source as a <see cref="byte"/>[].
        /// </summary>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None"/>.</param>
        /// <returns>A <see cref="Task{TResult}"/> that represents the asynchronous read. The <see cref="Task{TResult}.Result"/>
        /// property returns the <see cref="byte"/>[]. This result will be <c>null</c> at the end of an array.</returns>
        /// <remarks>The default behaviour is to execute synchronously, returning an already-completed task. Derived
        /// classes can override this behaviour for true asychronousity.</remarks>
        public virtual Task<byte[]> ReadAsBytesAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            return cancellationToken.CancelIfRequestedAsync<byte[]>() ?? Task.FromResult(ReadAsBytes());
        }

        internal async Task<byte[]> ReadArrayIntoByteArrayAsync(CancellationToken cancellationToken)
        {
            List<byte> buffer = new List<byte>();

            while (true)
            {
                if (!await ReadAsync(cancellationToken).ConfigureAwait(false))
                {
                    SetToken(JsonToken.None);
                }

                if (ReadArrayElementIntoByteArrayReportDone(buffer))
                {
                    byte[] d = buffer.ToArray();
                    SetToken(JsonToken.Bytes, d, false);
                    return d;
                }
            }
        }

        /// <summary>
        /// Asynchronously reads the next JSON token from the source as a <see cref="Nullable{T}"/> of <see cref="DateTime"/>.
        /// </summary>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None"/>.</param>
        /// <returns>A <see cref="Task{TResult}"/> that represents the asynchronous read. The <see cref="Task{TResult}.Result"/>
        /// property returns the <see cref="Nullable{T}"/> of <see cref="DateTime"/>. This result will be <c>null</c> at the end of an array.</returns>
        /// <remarks>The default behaviour is to execute synchronously, returning an already-completed task. Derived
        /// classes can override this behaviour for true asychronousity.</remarks>
        public virtual Task<DateTime?> ReadAsDateTimeAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            return cancellationToken.CancelIfRequestedAsync<DateTime?>() ?? Task.FromResult(ReadAsDateTime());
        }

        /// <summary>
        /// Asynchronously reads the next JSON token from the source as a <see cref="Nullable{T}"/> of <see cref="DateTimeOffset"/>.
        /// </summary>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None"/>.</param>
        /// <returns>A <see cref="Task{TResult}"/> that represents the asynchronous read. The <see cref="Task{TResult}.Result"/>
        /// property returns the <see cref="Nullable{T}"/> of <see cref="DateTimeOffset"/>. This result will be <c>null</c> at the end of an array.</returns>
        /// <remarks>The default behaviour is to execute synchronously, returning an already-completed task. Derived
        /// classes can override this behaviour for true asychronousity.</remarks>
        public virtual Task<DateTimeOffset?> ReadAsDateTimeOffsetAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            return cancellationToken.CancelIfRequestedAsync<DateTimeOffset?>() ?? Task.FromResult(ReadAsDateTimeOffset());
        }

        /// <summary>
        /// Asynchronously reads the next JSON token from the source as a <see cref="Nullable{T}"/> of <see cref="decimal"/>.
        /// </summary>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None"/>.</param>
        /// <returns>A <see cref="Task{TResult}"/> that represents the asynchronous read. The <see cref="Task{TResult}.Result"/>
        /// property returns the <see cref="Nullable{T}"/> of <see cref="decimal"/>. This result will be <c>null</c> at the end of an array.</returns>
        /// <remarks>The default behaviour is to execute synchronously, returning an already-completed task. Derived
        /// classes can override this behaviour for true asychronousity.</remarks>
        public virtual Task<decimal?> ReadAsDecimalAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            return cancellationToken.CancelIfRequestedAsync<decimal?>() ?? Task.FromResult(ReadAsDecimal());
        }

        /// <summary>
        /// Asynchronously reads the next JSON token from the source as a <see cref="Nullable{T}"/> of <see cref="double"/>.
        /// </summary>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None"/>.</param>
        /// <returns>A <see cref="Task{TResult}"/> that represents the asynchronous read. The <see cref="Task{TResult}.Result"/>
        /// property returns the <see cref="Nullable{T}"/> of <see cref="double"/>. This result will be <c>null</c> at the end of an array.</returns>
        /// <remarks>The default behaviour is to execute synchronously, returning an already-completed task. Derived
        /// classes can override this behaviour for true asychronousity.</remarks>
        public virtual Task<double?> ReadAsDoubleAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            return Task.FromResult(ReadAsDouble());
        }

        /// <summary>
        /// Asynchronously reads the next JSON token from the source as a <see cref="Nullable{T}"/> of <see cref="int"/>.
        /// </summary>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None"/>.</param>
        /// <returns>A <see cref="Task{TResult}"/> that represents the asynchronous read. The <see cref="Task{TResult}.Result"/>
        /// property returns the <see cref="Nullable{T}"/> of <see cref="int"/>. This result will be <c>null</c> at the end of an array.</returns>
        /// <remarks>The default behaviour is to execute synchronously, returning an already-completed task. Derived
        /// classes can override this behaviour for true asychronousity.</remarks>
        public virtual Task<int?> ReadAsInt32Async(CancellationToken cancellationToken = default(CancellationToken))
        {
            return cancellationToken.CancelIfRequestedAsync<int?>() ?? Task.FromResult(ReadAsInt32());
        }

        /// <summary>
        /// Asynchronously reads the next JSON token from the source as a <see cref="string"/>.
        /// </summary>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None"/>.</param>
        /// <returns>A <see cref="Task{TResult}"/> that represents the asynchronous read. The <see cref="Task{TResult}.Result"/>
        /// property returns the <see cref="string"/>. This result will be <c>null</c> at the end of an array.</returns>
        /// <remarks>The default behaviour is to execute synchronously, returning an already-completed task. Derived
        /// classes can override this behaviour for true asychronousity.</remarks>
        public virtual Task<string> ReadAsStringAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            return cancellationToken.CancelIfRequestedAsync<string>() ?? Task.FromResult(ReadAsString());
        }

        internal async Task<bool> ReadAndMoveToContentAsync(CancellationToken cancellationToken)
        {
            return await ReadAsync(cancellationToken).ConfigureAwait(false) && await MoveToContentAsync(cancellationToken).ConfigureAwait(false);
        }

        internal Task<bool> MoveToContentAsync(CancellationToken cancellationToken)
        {
            switch (TokenType)
            {
                case JsonToken.None:
                case JsonToken.Comment:
                    return MoveToContentFromNonContentAsync(cancellationToken);
                default:
                    return AsyncUtils.True;
            }
        }

        private async Task<bool> MoveToContentFromNonContentAsync(CancellationToken cancellationToken)
        {
            while (true)
            {
                if (!await ReadAsync(cancellationToken).ConfigureAwait(false))
                {
                    return false;
                }

                switch (TokenType)
                {
                    case JsonToken.None:
                    case JsonToken.Comment:
                        break;
                    default:
                        return true;
                }
            }
        }
    }
}

#endif
