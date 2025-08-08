using System;
using System.Text;
using System.Text.Json;


namespace roadwork_portal_service.ElasticsearchLogger
{
    public class ElasticsearchLogger: IElasticsearchLogger
    {
        private readonly HttpClient _httpClient;
        private readonly string _endpointUrl;
        private readonly string _environment;
        private readonly string _elasticApplication;

        public ElasticsearchLogger(string endpointUrl, string environment, string elasticApplication)
        {
            _endpointUrl = endpointUrl;
            _environment = environment;
            _elasticApplication = elasticApplication;

            if (string.IsNullOrEmpty(_endpointUrl))
            {
                throw new ArgumentException("Elasticsearch endpoint URL is missing or empty.");
            }

            if (string.IsNullOrEmpty(_environment))
            {
                throw new ArgumentException("Environment configuration is missing or empty.");
            }

            if (string.IsNullOrEmpty(_elasticApplication))
            {
                throw new ArgumentException("Elastic Application name is missing or empty.");
            }

            var handler = new HttpClientHandler();
            /* {
                ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true
            }; */

            _httpClient = new HttpClient(handler);

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Hi from Elasticsearch logger");                  
            Console.ResetColor();

        }

        public async Task<bool> SendLogAsync(string level, string message)
        {
            var messageParts = message.Split(";");

            var action = messageParts.Length > 0 ? messageParts[0] : "";
            var email = messageParts.Length > 1 ? messageParts[1] : "";
            var role = messageParts.Length > 2 ? messageParts[2] : "";                                    

            var logEntry = new
            {
                @timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                level,
                Application = _elasticApplication,
                Environment = _environment,
                message = message,
                user = new {
                    action = action,
                    email = email,
                    role = role
                }
            };

            var json = JsonSerializer.Serialize(logEntry);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            try
            {                
                var response = await _httpClient.PostAsync(_endpointUrl, content);
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }
        public void LogInformation(string message)
        {
            SendLogAsync("Information", message).GetAwaiter().GetResult();
        }
        public void LogWarning(string message)
        {
            SendLogAsync("Warning", message).GetAwaiter().GetResult();
        }
        public void LogError(string message)
        {
            SendLogAsync("Error", message).GetAwaiter().GetResult();
        }
        public void LogCritical(string message)
        {
            SendLogAsync("Critical", message).GetAwaiter().GetResult();
        }
    }
}
