using System;
using System.Collections.Generic;
using System.Text;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Urbanflow.src.backend.models.util
{
	public sealed class Result<T>
	{
		private readonly T _value;

		public bool IsSuccess { get; }
		public bool IsFailure => !IsSuccess;

		public string Error { get; }
		public string ErrorCode { get; }

		private Result(T value)
		{
			IsSuccess = true;
			_value = value;
			Error = null;
			ErrorCode = null;
		}

		private Result(string error, string errorCode = null)
		{
			IsSuccess = false;
			Error = error ?? throw new ArgumentNullException(nameof(error));
			ErrorCode = errorCode;
			_value = default!;
		}

		public T Value =>
			IsSuccess
				? _value
				: throw new InvalidOperationException("Cannot access Value of a failed result.");

		public static Result<T> Success(T value) => new(value);

		public static Result<T> Failure(string error, string errorCode = null)
			=> new(error, errorCode);

		public static implicit operator Result<T>(T value)
			=> Success(value);

		public TResult Match<TResult>(
			Func<T, TResult> onSuccess,
			Func<string, TResult> onFailure)
		{
			return IsSuccess
				? onSuccess(_value)
				: onFailure(Error);
		}

		public Result<T> OnSuccess(Action<T> action)
		{
			if (IsSuccess)
				action(_value);

			return this;
		}

		public Result<T> OnFailure(Action<string> action)
		{
			if (IsFailure)
				action(Error);

			return this;
		}

		// Helper for "void" results
		public readonly struct Unit
		{
			public static readonly Unit Value = new();
		}
	}

}
