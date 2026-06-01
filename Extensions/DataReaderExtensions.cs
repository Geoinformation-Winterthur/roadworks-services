using System.Data;

namespace roadwork_portal_service.Extensions
{
    /// <summary>
    /// Some helpfull extensions for IDataReader.
    /// </summary>
    public static class DataReaderExtensions
    {
        /// <summary>
        /// Returns the string value of the field or an empty string for NULL.
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="columnName"></param>
        /// <returns></returns>
        public static string GetStringOrEmpty(this IDataReader reader, string columnName)
        {
            int fieldIndex = reader.GetOrdinal(columnName);
            return reader.IsDBNull(fieldIndex) ? "" : reader.GetString(fieldIndex);
        }

        /// <summary>
        /// Returns the GUID value of the field or NULL.
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="columnName"></param>
        /// <returns></returns>
        public static Guid? GetNullableGuid(this IDataReader reader, string columnName)
        {
            int fieldIndex = reader.GetOrdinal(columnName);
            return reader.IsDBNull(fieldIndex) ? null : reader.GetGuid(fieldIndex);
        }

        /// <summary>
        /// Returns the GUID value as string of the field or an empty string for NULL.
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="columnName"></param>
        /// <returns></returns>
        public static string GetGuidAsStringOrEmpty(this IDataReader reader, string columnName)
        {
            int fieldIndex = reader.GetOrdinal(columnName);
            return reader.IsDBNull(fieldIndex) ? "" : reader.GetGuid(fieldIndex).ToString();
        }

        /// <summary>
        /// Returns the boolean value of the field or NULL.
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="columnName"></param>
        /// <returns></returns>
        public static bool? GetNullableBoolean(this IDataReader reader, string columnName)
        {
            int fieldIndex = reader.GetOrdinal(columnName);
            return reader.IsDBNull(fieldIndex) ? null : reader.GetBoolean(fieldIndex);
        }

        /// <summary>
        /// Returns the boolean value of the field or TRUE for NULL.
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="columnName"></param>
        /// <returns></returns>
        public static bool GetBooleanOrTrue(this IDataReader reader, string columnName)
        {
            int fieldIndex = reader.GetOrdinal(columnName);
            return reader.IsDBNull(fieldIndex) ? true : reader.GetBoolean(fieldIndex);
        }

        /// <summary>
        /// Returns the boolean value of the field or FALSE for NULL.
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="columnName"></param>
        /// <returns></returns>
        public static bool GetBooleanOrFalse(this IDataReader reader, string columnName)
        {
            int fieldIndex = reader.GetOrdinal(columnName);
            return reader.IsDBNull(fieldIndex) ? false : reader.GetBoolean(fieldIndex);
        }

        /// <summary>
        /// Returns the DateTime value of the field or NULL.
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="columnName"></param>
        /// <returns></returns>
        public static DateTime? GetNullableDateTime(this IDataReader reader, string columnName)
        {
            int fieldIndex = reader.GetOrdinal(columnName);
            return reader.IsDBNull(fieldIndex) ? null : reader.GetDateTime(fieldIndex);
        }

        /// <summary>
        /// Returns the DateTime value of the field or DateTime.MinValue for NULL.
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="columnName"></param>
        /// <returns></returns>
        public static DateTime GetDateTimeOrMinValue(this IDataReader reader, string columnName)
        {
            int fieldIndex = reader.GetOrdinal(columnName);
            return reader.IsDBNull(fieldIndex) ? DateTime.MinValue : reader.GetDateTime(fieldIndex);
        }

        /// <summary>
        /// Returns the DateTime value of the field or passed default value for NULL.
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="columnName"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public static DateTime GetDateTimeOrDefault(this IDataReader reader, string columnName, DateTime defaultValue)
        {
            int fieldIndex = reader.GetOrdinal(columnName);
            return reader.IsDBNull(fieldIndex) ? defaultValue : reader.GetDateTime(fieldIndex);
        }

        /// <summary>
        /// Returns the short value of the field or NULL.
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="columnName"></param>
        /// <returns></returns>
        public static short? GetNullableShort(this IDataReader reader, string columnName)
        {
            int fieldIndex = reader.GetOrdinal(columnName);
            return reader.IsDBNull(fieldIndex) ? null : reader.GetInt16(fieldIndex);
        }

        /// <summary>
        /// Returns the integer value of the field or NULL.
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="columnName"></param>
        /// <returns></returns>
        public static int? GetNullableInt(this IDataReader reader, string columnName)
        {
            int fieldIndex = reader.GetOrdinal(columnName);
            return reader.IsDBNull(fieldIndex) ? null : reader.GetInt32(fieldIndex);
        }

        /// <summary>
        /// Returns the decimal value of the field or NULL.
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="columnName"></param>
        /// <returns></returns>
        public static decimal? GetNullableDecimal(this IDataReader reader, string columnName)
        {
            int fieldIndex = reader.GetOrdinal(columnName);
            return reader.IsDBNull(fieldIndex) ? null : reader.GetDecimal(fieldIndex);
        }

        /// <summary>
        /// Returns the short value of the field or passed default value for NULL.
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="columnName"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public static short GetShortOrDefault(this IDataReader reader, string columnName, short defaultValue)
        {
            int fieldIndex = reader.GetOrdinal(columnName);
            return reader.IsDBNull(fieldIndex) ? defaultValue : reader.GetInt16(fieldIndex);
        }

        /// <summary>
        /// Returns the integer value of the field or passed default value for NULL.
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="columnName"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public static int GetIntOrDefault(this IDataReader reader, string columnName, int defaultValue)
        {
            int fieldIndex = reader.GetOrdinal(columnName);
            return reader.IsDBNull(fieldIndex) ? defaultValue : reader.GetInt32(fieldIndex);
        }

        /// <summary>
        /// Returns the long value of the field or NULL.
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="columnName"></param>
        /// <returns></returns>
        public static long? GetNullableLong(this IDataReader reader, string columnName)
        {
            int fieldIndex = reader.GetOrdinal(columnName);
            return reader.IsDBNull(fieldIndex) ? null : reader.GetInt64(fieldIndex);
        }

        /// <summary>
        /// Returns the long value of the field or passed default value for NULL.
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="columnName"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public static long GetLongOrDefault(this IDataReader reader, string columnName, long defaultValue)
        {
            int fieldIndex = reader.GetOrdinal(columnName);
            return reader.IsDBNull(fieldIndex) ? defaultValue : reader.GetInt64(fieldIndex);
        }

        /// <summary>
        /// Returns the value of the field or the passed default value for NULL.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="reader"></param>
        /// <param name="columnName"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public static T GetValueOrDefault<T>(this IDataReader reader, string columnName, T defaultValue)
            where T: class
        {
            int fieldIndex = reader.GetOrdinal(columnName);

            if (reader.IsDBNull(fieldIndex))
            {
                return defaultValue;
            }
            else
            {
                return reader.GetValue(fieldIndex) as T ?? defaultValue;
            }
        }
    }
}
