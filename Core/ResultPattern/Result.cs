namespace VentasETL.Core.ResultPattern;

public class Result
{
    public bool IsSuccess { get; }
    public string Error { get; }
    public bool IsFailure => !IsSuccess;

    protected Result(bool isSuccess, string error)
    {
        IsSuccess = isSuccess;
        Error = error;
    }

    public static Result Success() => new(true, string.Empty);
    public static Result Failure(string error) => new(false, error);
}

public class Result<T> : Result
{
    public T Value { get; }
    protected Result(bool isSuccess, string error, T value) : base(isSuccess, error) => Value = value;

    public static Result<T> Success(T value) => new(true, string.Empty, value);
    public static new Result<T> Failure(string error) => new(false, error, default!);
}