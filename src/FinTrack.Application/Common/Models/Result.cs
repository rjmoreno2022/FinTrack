using System;
using System.Linq;

namespace FinTrack.Application.Common.Models;

public class Result
{
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public string? Error { get; }
    public string[] Errors { get; }

    protected Result(bool isSuccess, string? error = null, string[]? errors = null)
    {
        IsSuccess = isSuccess;
        Error = error;
        Errors = errors ?? (error != null ? new[] { error } : Array.Empty<string>());
    }

    public static Result Ok() => new(true);
    public static Result Fail(string error) => new(false, error, new[] { error });
    public static Result Fail(string[] errors) => new(false, errors.FirstOrDefault(), errors);
}

public class Result<T> : Result
{
    public T? Value { get; }

    protected Result(bool isSuccess, T? value = default, string? error = null, string[]? errors = null)
        : base(isSuccess, error, errors)
    {
        Value = value;
    }

    public static Result<T> Ok(T value) => new(true, value);
    public static new Result<T> Fail(string error) => new(false, default, error, new[] { error });
    public static new Result<T> Fail(string[] errors) => new(false, default, errors.FirstOrDefault(), errors);
}
