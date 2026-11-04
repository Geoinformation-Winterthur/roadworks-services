namespace roadwork_portal_service.Model
{
    /// <summary>
    /// API response object for responses with a row count. E.g. sql insert.
    /// </summary>
    public class AffectedRowsResult
    {
        /// <summary>
        /// Nummber of affected rows.
        /// </summary>
        public int affectedRows { get; set; }

        /// <summary>
        /// Error message in case of an error. Empty means no error.
        /// </summary>
        public string errorMessage { get; set; } = "";
    }
}
