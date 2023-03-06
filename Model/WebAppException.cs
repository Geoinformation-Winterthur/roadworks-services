namespace roadwork_portal_service.Model;

public class WebAppException
{
    public string exceptionMessage { get; set; } = "";

    public WebAppException(string exceptionMessage)
    {
        this.exceptionMessage = exceptionMessage;
    }
}
