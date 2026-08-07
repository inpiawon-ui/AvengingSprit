namespace GameFramework.Core.Module.Network
{
    public sealed class NetworkResponse<T>
    {
        public bool   IsSuccess  { get; }
        public int    StatusCode { get; }
        public T      Data       { get; }
        public string Error      { get; }

        private NetworkResponse(bool success, int statusCode, T data, string error)
        {
            IsSuccess  = success;
            StatusCode = statusCode;
            Data       = data;
            Error      = error;
        }

        public static NetworkResponse<T> Success(int statusCode, T data) =>
            new(true, statusCode, data, null);

        public static NetworkResponse<T> Failure(int statusCode, string error) =>
            new(false, statusCode, default, error);
    }
}
