namespace EventManagement.UI.Models;

public class ErrorViewModel
{
    public string? RequestId { get; set; }

    public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
    
    public int StatusCode { get; set; } = 500;
    public string Title { get; set; } = "Bir Hata Oluştu";
    public string Message { get; set; } = "İşleminiz gerçekleştirilirken beklenmedik bir hata oluştu.";
}
