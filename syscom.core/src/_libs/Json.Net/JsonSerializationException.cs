using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace libs.Json
{
    /// <summary>
    /// The exception thrown when an error occurs during JSON serialization or deserialization.
    /// </summary>
#if HAVE_BINARY_EXCEPTION_SERIALIZATION
    [Serializable]
#endif
    public class JsonSerializationException : JsonException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="JsonSerializationException"/> class.
        /// </summary>
        public JsonSerializationException()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="JsonSerializationException"/> class
        /// with a specified error message.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        public JsonSerializationException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="JsonSerializationException"/> class
        /// with a specified error message and a reference to the inner exception that is the cause of this exception.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="innerException">The exception that is the cause of the current exception, or <c>null</c> if no inner exception is specified.</param>
        public JsonSerializationException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

#if HAVE_BINARY_EXCEPTION_SERIALIZATION
        /// <summary>
        /// Initializes a new instance of the <see cref="JsonSerializationException"/> class.
        /// </summary>
        /// <param name="info">The <see cref="SerializationInfo"/> that holds the serialized object data about the exception being thrown.</param>
        /// <param name="context">The <see cref="StreamingContext"/> that contains contextual information about the source or destination.</param>
        /// <exception cref="ArgumentNullException">The <paramref name="info"/> parameter is <c>null</c>.</exception>
        /// <exception cref="SerializationException">The class name is <c>null</c> or <see cref="Exception.HResult"/> is zero (0).</exception>
        public JsonSerializationException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
#endif

        internal static JsonSerializationException Create(JsonReader reader, string message)
        {
            return Create(reader, message, null);
        }

        internal static JsonSerializationException Create(JsonReader reader, string message, Exception ex)
        {
            return Create(reader as IJsonLineInfo, reader.Path, message, ex);
        }

        internal static JsonSerializationException Create(IJsonLineInfo lineInfo, string path, string message, Exception ex)
        {
            message = JsonPosition.FormatMessage(lineInfo, path, message);

            return new JsonSerializationException(message, ex);
        }
    }
}
