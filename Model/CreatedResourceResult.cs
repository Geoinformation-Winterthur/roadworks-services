namespace roadwork_portal_service.Model
{
    /// <summary>
    /// API response object for a newly created resource.
    /// </summary>
    public class CreatedResourceResult
    {
        /// <summary>
        /// UUID of the created resource.
        /// </summary>
        public Guid uuid { get; set; }

        /// <summary>
        /// Error message in case of an error. Empty means no error.
        /// </summary>
        public string errorMessage { get; set; } = "";
    }
}
