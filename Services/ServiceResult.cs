namespace Auction_Portal_Clone.Services
{
    public class ServiceResult<T>
    {
        public bool Succeeded { get; private set; }
        public string? ErrorMessage { get; private set; }
        public T? Data { get; private set; }

        public static ServiceResult<T> Success(T data) => new() { Succeeded = true, Data = data };

        public static ServiceResult<T> Failure(string errorMessage) => new() { Succeeded = false, ErrorMessage = errorMessage };
    }
}