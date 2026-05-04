namespace roadwork_portal_service.Extensions
{
    /// <summary>
    /// Some helpfull extensions for strings.
    /// </summary>
    public static class StringExtensions
    {
        /// <summary>
        /// Converts a string to a GUID or null.
        /// Might throw an Exception for invalid values.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static Guid ToGuid(this string value)
        {
            return new Guid(value);
        }

        /// <summary>
        /// Converts a string to a GUID or null.
        /// Might throw an Exception for invalid values.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static Guid? ToNullableGuid(this string value)
        {
            return string.IsNullOrEmpty(value) ? null : new Guid(value);
        }

        /// <summary>
        /// Converts a string to a guid or creates a new GUID for NULL.
        /// Might throw an Exception for invalid values.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static Guid ToGuidOrNewGuid(this string value)
        {
            return string.IsNullOrEmpty(value) ? Guid.NewGuid() : new Guid(value);
        }
    }
}
