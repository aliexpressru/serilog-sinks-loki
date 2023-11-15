namespace WebApi.Models;

public class ErrorResponse
{
    public string Message { get; set; }
    
    public string StackTrace { get; set; }
}