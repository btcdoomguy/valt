namespace Valt.App.Kernel;

/// <summary>
/// Represents the result of an operation that can succeed with a value or fail with an error.
/// </summary>
public abstract record Result<T>
{
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public T? Value { get; }
    public Error? Error { get; }

    private protected Result(bool isSuccess, T? value, Error? error)
    {
        IsSuccess = isSuccess;
        Value = value;
        Error = error;
    }

    public static Result<T> Success(T value) => new SuccessResult<T>(value);

    public static Result<T> Failure(Error error) => new FailureResult<T>(error);

    public static Result<T> Failure(string code, string message) =>
        new FailureResult<T>(new Error(code, message));

    public static Result<T> ValidationFailure(Dictionary<string, string[]> errors) =>
        new FailureResult<T>(Error.Validation("Validation failed", errors));

    public static Result<T> NotFound(string entityType, string id) =>
        new FailureResult<T>(Error.NotFound(entityType, id));

    public TResult Match<TResult>(Func<T, TResult> onSuccess, Func<Error, TResult> onFailure) =>
        IsSuccess ? onSuccess(Value!) : onFailure(Error!);

    public Result<TNew> Map<TNew>(Func<T, TNew> mapper) =>
        IsSuccess ? Result<TNew>.Success(mapper(Value!)) : Result<TNew>.Failure(Error!);

    public async Task<Result<TNew>> MapAsync<TNew>(Func<T, Task<TNew>> mapper) =>
        IsSuccess ? Result<TNew>.Success(await mapper(Value!)) : Result<TNew>.Failure(Error!);

    public Result<TNew> Bind<TNew>(Func<T, Result<TNew>> binder) =>
        IsSuccess ? binder(Value!) : Result<TNew>.Failure(Error!);

    public async Task<Result<TNew>> BindAsync<TNew>(Func<T, Task<Result<TNew>>> binder) =>
        IsSuccess ? await binder(Value!) : Result<TNew>.Failure(Error!);

    public T ValueOrThrow() =>
        IsSuccess ? Value! : throw new InvalidOperationException($"Result is a failure: {Error}");

    public T ValueOrDefault(T defaultValue) =>
        IsSuccess ? Value! : defaultValue;
}

internal sealed record SuccessResult<T> : Result<T>
{
    public SuccessResult(T value) : base(true, value, null) { }
}

internal sealed record FailureResult<T> : Result<T>
{
    public FailureResult(Error error) : base(false, default, error) { }
}

/// <summary>
/// Static helper for Result operations.
/// </summary>
public static class Result
{
    public static Result<Unit> Success() => Result<Unit>.Success(Unit.Value);

    public static Result<T> Success<T>(T value) => Result<T>.Success(value);

    public static Result<T> Failure<T>(Error error) => Result<T>.Failure(error);

    public static Result<T> Failure<T>(string code, string message) =>
        Result<T>.Failure(code, message);
}
