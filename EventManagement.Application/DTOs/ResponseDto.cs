namespace EventManagement.Application.DTOs
{
    public class ResponseDto<T>
    {
        public bool IsSuccess { get; set; }
        public string? Message { get; set; }
        public T? Data { get; set; }
        public List<string>? Errors { get; set; }
        
        public static ResponseDto<T> Success(T data, string? message = null)
        {
            return new ResponseDto<T>
            {
                IsSuccess = true,
                Message = message,
                Data = data,
                Errors = null
            };
        }
        
        public static ResponseDto<T> Fail(string message, List<string>? errors = null)
        {
            return new ResponseDto<T>
            {
                IsSuccess = false,
                Message = message,
                Data = default,
                Errors = errors
            };
        }
    }
} 