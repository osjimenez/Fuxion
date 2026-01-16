using System;
using System.Collections.Generic;

namespace Fuxion;

/// <summary>
/// Represents errors that occur during Fuxion application execution.
/// This is the base exception type for all Fuxion-specific exceptions.
/// </summary>
/// <remarks>
/// Use this exception when you need to throw a Fuxion-specific error that doesn't fit into more specialized exception categories.
/// This exception helps distinguish Fuxion framework errors from other application or system errors.
/// </remarks>
public class FuxionException : Exception
{
	/// <summary>
	/// Initializes a new instance of the <see cref="FuxionException"/> class.
	/// </summary>
	public FuxionException() { }
	
	/// <summary>
	/// Initializes a new instance of the <see cref="FuxionException"/> class with a specified error message.
	/// </summary>
	/// <param name="message">The message that describes the error.</param>
	public FuxionException(string message) : base(message) { }
	
	/// <summary>
	/// Initializes a new instance of the <see cref="FuxionException"/> class with a specified error message 
	/// and a reference to the inner exception that is the cause of this exception.
	/// </summary>
	/// <param name="message">The error message that explains the reason for the exception.</param>
	/// <param name="innerException">The exception that is the cause of the current exception, or a null reference if no inner exception is specified.</param>
	public FuxionException(string message, Exception innerException) : base(message, innerException) { }
}

/// <summary>
/// Represents one or more errors that occur during Fuxion application execution.
/// This exception aggregates multiple exceptions into a single exception object.
/// </summary>
/// <remarks>
/// Use this exception when multiple operations fail and you want to capture all failures in a single exception.
/// This is particularly useful in parallel processing scenarios or batch operations where multiple errors can occur simultaneously.
/// </remarks>
/// <example>
/// <code>
/// var exceptions = new List&lt;Exception&gt;();
/// foreach (var item in items)
/// {
///     try
///     {
///         ProcessItem(item);
///     }
///     catch (Exception ex)
///     {
///         exceptions.Add(ex);
///     }
/// }
/// if (exceptions.Any())
/// {
///     throw new FuxionAggregateException("Multiple processing errors occurred", exceptions);
/// }
/// </code>
/// </example>
public class FuxionAggregateException : AggregateException
{
	/// <summary>
	/// Initializes a new instance of the <see cref="FuxionAggregateException"/> class.
	/// </summary>
	public FuxionAggregateException() { }
	
	/// <summary>
	/// Initializes a new instance of the <see cref="FuxionAggregateException"/> class with a specified error message.
	/// </summary>
	/// <param name="message">The message that describes the error.</param>
	public FuxionAggregateException(string message) : base(message) { }
	
	/// <summary>
	/// Initializes a new instance of the <see cref="FuxionAggregateException"/> class with references to the inner exceptions.
	/// </summary>
	/// <param name="innerExceptions">The exceptions that are the cause of the current exception.</param>
	public FuxionAggregateException(Exception[] innerExceptions) : base(innerExceptions) { }
	
	/// <summary>
	/// Initializes a new instance of the <see cref="FuxionAggregateException"/> class with references to the inner exceptions.
	/// </summary>
	/// <param name="innerExceptions">The exceptions that are the cause of the current exception.</param>
	public FuxionAggregateException(IEnumerable<Exception> innerExceptions) : base(innerExceptions) { }
	
	/// <summary>
	/// Initializes a new instance of the <see cref="FuxionAggregateException"/> class with a specified error message 
	/// and a reference to the inner exception.
	/// </summary>
	/// <param name="message">The error message that explains the reason for the exception.</param>
	/// <param name="innerException">The exception that is the cause of the current exception.</param>
	public FuxionAggregateException(string message, Exception innerException) : base(message, innerException) { }
	
	/// <summary>
	/// Initializes a new instance of the <see cref="FuxionAggregateException"/> class with a specified error message 
	/// and references to the inner exceptions.
	/// </summary>
	/// <param name="message">The error message that explains the reason for the exception.</param>
	/// <param name="innerExceptions">The exceptions that are the cause of the current exception.</param>
	public FuxionAggregateException(string message, params Exception[] innerExceptions) : base(message, innerExceptions) { }
	
	/// <summary>
	/// Initializes a new instance of the <see cref="FuxionAggregateException"/> class with a specified error message 
	/// and references to the inner exceptions.
	/// </summary>
	/// <param name="message">The error message that explains the reason for the exception.</param>
	/// <param name="innerExceptions">The exceptions that are the cause of the current exception.</param>
	public FuxionAggregateException(string message, IEnumerable<Exception> innerExceptions) : base(message, innerExceptions) { }
}

/// <summary>
/// Represents one or more errors of a specific exception type that occur during Fuxion application execution.
/// This generic version provides type-safe access to inner exceptions of a specific type.
/// </summary>
/// <typeparam name="TInnerExceptions">The type of exceptions that this aggregate exception contains. Must derive from <see cref="Exception"/>.</typeparam>
/// <remarks>
/// Use this generic version when you want to ensure all aggregated exceptions are of a specific type.
/// This provides compile-time type safety and makes it easier to handle specific exception types uniformly.
/// </remarks>
/// <example>
/// <code>
/// var validationErrors = new List&lt;ValidationException&gt;();
/// foreach (var field in fields)
/// {
///     try
///     {
///         ValidateField(field);
///     }
///     catch (ValidationException ex)
///     {
///         validationErrors.Add(ex);
///     }
/// }
/// if (validationErrors.Any())
/// {
///     throw new FuxionAggregateException&lt;ValidationException&gt;("Validation failed", validationErrors);
/// }
/// </code>
/// </example>
public class FuxionAggregateException<TInnerExceptions> : FuxionAggregateException where TInnerExceptions : Exception
{
	/// <summary>
	/// Initializes a new instance of the <see cref="FuxionAggregateException{TInnerExceptions}"/> class.
	/// </summary>
	public FuxionAggregateException() { }
	
	/// <summary>
	/// Initializes a new instance of the <see cref="FuxionAggregateException{TInnerExceptions}"/> class with a specified error message.
	/// </summary>
	/// <param name="message">The message that describes the error.</param>
	public FuxionAggregateException(string message) : base(message) { }
	
	/// <summary>
	/// Initializes a new instance of the <see cref="FuxionAggregateException{TInnerExceptions}"/> class with references to the inner exceptions.
	/// </summary>
	/// <param name="innerExceptions">The exceptions of type <typeparamref name="TInnerExceptions"/> that are the cause of the current exception.</param>
	public FuxionAggregateException(TInnerExceptions[] innerExceptions) : base(innerExceptions) { }
	
	/// <summary>
	/// Initializes a new instance of the <see cref="FuxionAggregateException{TInnerExceptions}"/> class with references to the inner exceptions.
	/// </summary>
	/// <param name="innerExceptions">The exceptions of type <typeparamref name="TInnerExceptions"/> that are the cause of the current exception.</param>
	public FuxionAggregateException(IEnumerable<TInnerExceptions> innerExceptions) : base(innerExceptions) { }
	
	/// <summary>
	/// Initializes a new instance of the <see cref="FuxionAggregateException{TInnerExceptions}"/> class with a specified error message 
	/// and a reference to the inner exception.
	/// </summary>
	/// <param name="message">The error message that explains the reason for the exception.</param>
	/// <param name="innerException">The exception of type <typeparamref name="TInnerExceptions"/> that is the cause of the current exception.</param>
	public FuxionAggregateException(string message, TInnerExceptions innerException) : base(message, innerException) { }
	
	/// <summary>
	/// Initializes a new instance of the <see cref="FuxionAggregateException{TInnerExceptions}"/> class with a specified error message 
	/// and references to the inner exceptions.
	/// </summary>
	/// <param name="message">The error message that explains the reason for the exception.</param>
	/// <param name="innerExceptions">The exceptions of type <typeparamref name="TInnerExceptions"/> that are the cause of the current exception.</param>
	public FuxionAggregateException(string message, params TInnerExceptions[] innerExceptions) : base(message, innerExceptions) { }
	
	/// <summary>
	/// Initializes a new instance of the <see cref="FuxionAggregateException{TInnerExceptions}"/> class with a specified error message 
	/// and references to the inner exceptions.
	/// </summary>
	/// <param name="message">The error message that explains the reason for the exception.</param>
	/// <param name="innerExceptions">The exceptions of type <typeparamref name="TInnerExceptions"/> that are the cause of the current exception.</param>
	public FuxionAggregateException(string message, IEnumerable<TInnerExceptions> innerExceptions) : base(message, innerExceptions) { }
}