namespace FileService.Domain.Common
{
    public record Result<T> 
    {
        public T? Value { get; }
        public bool IsSuccess { get; }
        public string Error { get; }
        private Result(T value)
        {
            Value = value; 
            IsSuccess = true;
            Error=string.Empty;
        }

        private Result(string error)
        {
            Value = default;
            IsSuccess = false;
            Error=error;
        }

        public static Result<T> Success(T value) => new(value);

        public static Result<T> Failure(string error)=> new(error);
    }
}