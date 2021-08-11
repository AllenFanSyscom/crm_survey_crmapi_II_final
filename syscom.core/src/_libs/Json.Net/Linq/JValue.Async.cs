﻿#if HAVE_ASYNC

using System;
using System.Globalization;
#if HAVE_BIG_INTEGER
using System.Numerics;
#endif
using System.Threading;
using System.Threading.Tasks;
using libs.Json.Utilities;

namespace libs.Json.Linq
{
    public partial class JValue
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
            if (converters != null && converters.Length > 0 && _value != null)
            {
                JsonConverter matchingConverter = JsonSerializer.GetMatchingConverter(converters, _value.GetType());
                if (matchingConverter != null && matchingConverter.CanWrite)
                {
                    // TODO: Call WriteJsonAsync when it exists.
                    matchingConverter.WriteJson(writer, _value, JsonSerializer.CreateDefault());
                    return AsyncUtils.CompletedTask;
                }
            }

            switch (_valueType)
            {
                case JTokenType.Comment:
                    return writer.WriteCommentAsync(_value?.ToString(), cancellationToken);
                case JTokenType.Raw:
                    return writer.WriteRawValueAsync(_value?.ToString(), cancellationToken);
                case JTokenType.Null:
                    return writer.WriteNullAsync(cancellationToken);
                case JTokenType.Undefined:
                    return writer.WriteUndefinedAsync(cancellationToken);
                case JTokenType.Integer:
                    if (_value is int)
                    {
                        return writer.WriteValueAsync((int)_value, cancellationToken);
                    }

                    if (_value is long)
                    {
                        return writer.WriteValueAsync((long)_value, cancellationToken);
                    }

                    if (_value is ulong)
                    {
                        return writer.WriteValueAsync((ulong)_value, cancellationToken);
                    }

#if HAVE_BIG_INTEGER
                    if (_value is BigInteger)
                    {
                        return writer.WriteValueAsync((BigInteger)_value, cancellationToken);
                    }
#endif

                    return writer.WriteValueAsync(Convert.ToInt64(_value, CultureInfo.InvariantCulture), cancellationToken);
                case JTokenType.Float:
                    if (_value is decimal)
                    {
                        return writer.WriteValueAsync((decimal)_value, cancellationToken);
                    }

                    if (_value is double)
                    {
                        return writer.WriteValueAsync((double)_value, cancellationToken);
                    }

                    if (_value is float)
                    {
                        return writer.WriteValueAsync((float)_value, cancellationToken);
                    }

                    return writer.WriteValueAsync(Convert.ToDouble(_value, CultureInfo.InvariantCulture), cancellationToken);
                case JTokenType.String:
                    return writer.WriteValueAsync(_value?.ToString(), cancellationToken);
                case JTokenType.Boolean:
                    return writer.WriteValueAsync(Convert.ToBoolean(_value, CultureInfo.InvariantCulture), cancellationToken);
                case JTokenType.Date:
                    if (_value is DateTimeOffset)
                    {
                        return writer.WriteValueAsync((DateTimeOffset)_value, cancellationToken);
                    }

                    return writer.WriteValueAsync(Convert.ToDateTime(_value, CultureInfo.InvariantCulture), cancellationToken);
                case JTokenType.Bytes:
                    return writer.WriteValueAsync((byte[])_value, cancellationToken);
                case JTokenType.Guid:
                    return writer.WriteValueAsync(_value != null ? (Guid?)_value : null, cancellationToken);
                case JTokenType.TimeSpan:
                    return writer.WriteValueAsync(_value != null ? (TimeSpan?)_value : null, cancellationToken);
                case JTokenType.Uri:
                    return writer.WriteValueAsync((Uri)_value, cancellationToken);
            }

            throw MiscellaneousUtils.CreateArgumentOutOfRangeException(nameof(Type), _valueType, "Unexpected token type.");
        }
    }
}

#endif
