namespace Campaign.Application.Common;

/// <summary>
/// An expected use-case outcome without a return value.
/// </summary>
public sealed class OperationResult
{
    private OperationResult(bool isSuccess, string? errorCode, string? message)
    {
        IsSuccess = isSuccess;
        ErrorCode = errorCode;
        Message = message;
    }

    /// <summary>
    /// Gets a value indicating whether the operation succeeded.
    /// </summary>
    public bool IsSuccess { get; }

    /// <summary>
    /// Gets the stable error code when the operation failed.
    /// </summary>
    public string? ErrorCode { get; }

    /// <summary>
    /// Gets a safe explanation when the operation failed.
    /// </summary>
    public string? Message { get; }

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    /// <returns>A successful result.</returns>
    public static OperationResult Success()
    {
        return new OperationResult(true, null, null);
    }

    /// <summary>
    /// Creates a failed result.
    /// </summary>
    /// <param name="errorCode">The stable error code.</param>
    /// <param name="message">A safe explanation.</param>
    /// <returns>A failed result.</returns>
    public static OperationResult Failure(string errorCode, string message)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(errorCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(message);
        return new OperationResult(false, errorCode, message);
    }
}

/// <summary>
/// An expected use-case outcome with a return value.
/// </summary>
/// <typeparam name="T">The success value type.</typeparam>
public sealed class OperationResult<T>
{
    internal OperationResult(bool isSuccess, T? value, string? errorCode, string? message)
    {
        IsSuccess = isSuccess;
        Value = value;
        ErrorCode = errorCode;
        Message = message;
    }

    /// <summary>
    /// Gets a value indicating whether the operation succeeded.
    /// </summary>
    public bool IsSuccess { get; }

    /// <summary>
    /// Gets the success value when <see cref="IsSuccess"/> is <see langword="true"/>.
    /// </summary>
    public T? Value { get; }

    /// <summary>
    /// Gets the stable error code when the operation failed.
    /// </summary>
    public string? ErrorCode { get; }

    /// <summary>
    /// Gets a safe explanation when the operation failed.
    /// </summary>
    public string? Message { get; }
}

/// <summary>
/// Factory methods for <see cref="OperationResult{T}"/>. Kept off the generic type to satisfy CA1000.
/// </summary>
public static class OperationResults
{
    /// <summary>
    /// Creates a successful result.
    /// </summary>
    /// <typeparam name="T">The success value type.</typeparam>
    /// <param name="value">The success value.</param>
    /// <returns>A successful result.</returns>
    public static OperationResult<T> Success<T>(T value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return new OperationResult<T>(true, value, null, null);
    }

    /// <summary>
    /// Creates a failed result.
    /// </summary>
    /// <typeparam name="T">The success value type.</typeparam>
    /// <param name="errorCode">The stable error code.</param>
    /// <param name="message">A safe explanation.</param>
    /// <returns>A failed result.</returns>
    public static OperationResult<T> Failure<T>(string errorCode, string message)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(errorCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(message);
        return new OperationResult<T>(false, default, errorCode, message);
    }
}
