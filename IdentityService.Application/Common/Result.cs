namespace IdentityService.Application.Common
{
    public record Result<T>
    {
        public bool IsSuccess { get; init; }
        public string Message { get; init; } = string.Empty;
        public T? Data { get;init;  }
        public List<string> Errors { get; init; } = [];

        public static Result<T> Success(T data,string message="Operation succesfull")
        {
            return new Result<T>
            {
                IsSuccess = true,
                Message = message,
                Data = data
            };
        }

        public static Result<T> Failure(string message, List<string>? errors = null)
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