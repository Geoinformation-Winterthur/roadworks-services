namespace roadwork_portal_service.ElasticsearchLogger
{
    public class ElasticsearchLoggerNotUsed: IElasticsearchLogger
    {
        public ElasticsearchLoggerNotUsed()
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("No logs will be sent to Elasticsearch.");      
            Console.ResetColor();
        }

        public void LogInformation(string message) { }
        public void LogWarning(string message) { }
        public void LogError(string message) { }
        public void LogCritical(string message) { }
    }
}