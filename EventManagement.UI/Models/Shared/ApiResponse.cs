namespace EventManagement.UI.Models.Shared
{
    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public T? Data { get; set; }
        public int StatusCode { get; set; }
        public object? Errors { get; set; }
        
        // Uyumluluk için IsSuccess property'si ekleyelim
        public bool IsSuccess 
        { 
            get { return Success; }
            set { Success = value; }
        }
    }
    
    public class ResponseModel<T>
    {
        public bool IsSuccess { get; set; }
        public string Message { get; set; } = string.Empty;
        public T? Data { get; set; }
        
        public static ResponseModel<T> Success(T data, string message = "İşlem başarılı")
        {
            return new ResponseModel<T>
            {
                IsSuccess = true,
                Message = message,
                Data = data
            };
        }
        
        public static ResponseModel<T> Fail(string message = "İşlem başarısız")
        {
            return new ResponseModel<T>
            {
                IsSuccess = false,
                Message = message,
                Data = default
            };
        }
    }
} 