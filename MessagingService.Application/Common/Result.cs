namespace MessagingService.Application.Common
{
    /// <summary>
    /// Generic result wrapper for operations.
    /// Provides a consistent way to return success/failure without exceptions.
    /// Includes optional error messages and data payload.
    /// </summary>
    public record Result<T>
    {
        public bool IsSuccess { get; init; }
        public T? Data { get; init; }
        public string Message { get; init; } = string.Empty;
        public List<string> Errors { get; init; } = [];

        public static Result<T> Success(T data,string message="Operation succesful")
        {
            return new Result<T>
            {
                IsSuccess = true,
                Data = data,
                Message = message
            };
        }
        public static Result<T> Failure(string message,List<string>? errors=null)
        {
            return new Result<T>
            {
                IsSuccess = false,
                Message = message,
                Errors = errors ?? []
            };
        }
    }
}