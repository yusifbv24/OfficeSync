namespace UserManagementService.Application.Common
{
    /// <summary>
    /// Generic result wrapper for operations that can succeed or fail.
    /// This follows the Result pattern, avoiding exceptions for expected failures.
    /// Using a result type makes error handling explicit and type-safe.
    /// </summary>
    public record Result<T>
    {
        public bool IsSuccess { get; init; }
        public T? Data { get; init; }
        public string? Message { get; init; }
        public List<string> Errors { get; init; } = [];

        /// <summary>
        /// Create a successful result with data.
        /// </summary>
        public static Result<T> Success(T data,string message="Operation succesfull")
        {
            return new Result<T>
            {
                IsSuccess = true,
                Data = data,
                Message=message
            };
        }


        /// <summary>
        /// Create a failed result with error messages.
        /// </summary>
        public static Result<T> Failure(string message, List<string>? errors = null)
        {
            return new Result<T>
            {
                IsSuccess = false,
                Message=message,
                Errors = errors ?? []
            };
        }
    }
}