namespace roadwork_portal_service.ElasticsearchLogger
{
    public interface IElasticsearchLogger
    {
        void LogInformation(string message);
        void LogWarning(string message);
        void LogError(string message);
        void LogCritical(string message);
    }
}
