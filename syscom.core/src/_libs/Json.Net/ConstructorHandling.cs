namespace libs.Json
{
    /// <summary>
    /// Specifies how constructors are used when initializing objects during deserialization by the <see cref="JsonSerializer"/>.
    /// </summary>
    public enum ConstructorHandling
    {
        /// <summary>
        /// First attempt to use the public default constructor, then fall back to a single parameterized constructor, then to the non-public default constructor.
        /// </summary>
        Default = 0,

        /// <summary>
        /// Json.NET will use a non-public default constructor before falling back to a parameterized constructor.
        /// </summary>
        AllowNonPublicDefaultConstructor = 1
    }
}
