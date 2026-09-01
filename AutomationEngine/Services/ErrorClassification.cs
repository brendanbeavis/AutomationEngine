using System;

namespace AutomationEngine.Services
{
    /// <summary>
    /// Classifies exceptions and errors into categories for better monitoring and alerting.
    /// </summary>
    public enum ErrorClassification
    {
        /// <summary>
        /// Input validation or business rule violation
        /// </summary>
        ValidationError,

        /// <summary>
        /// Expected file system operation failure (file not found, access denied, etc.)
        /// </summary>
        FileSystemError,

        /// <summary>
        /// Database or data access error
        /// </summary>
        DatabaseError,

        /// <summary>
        /// Network communication failure
        /// </summary>
        NetworkError,

        /// <summary>
        /// Permission or authorization failure
        /// </summary>
        PermissionError,

        /// <summary>
        /// Timeout during operation
        /// </summary>
        TimeoutError,

        /// <summary>
        /// Configuration or initialization error
        /// </summary>
        ConfigurationError,

        /// <summary>
        /// Unexpected system or runtime error
        /// </summary>
        SystemError,

        /// <summary>
        /// Job execution failure
        /// </summary>
        JobExecutionError,

        /// <summary>
        /// Unclassified or unknown error
        /// </summary>
        Unknown
    }

    /// <summary>
    /// Helper class for classifying exceptions and errors.
    /// Provides methods to categorize exceptions into error classifications
    /// and extract meaningful diagnostic information.
    /// </summary>
    public static class ErrorClassifier
    {
        /// <summary>
        /// Classifies an exception into an error category.
        /// </summary>
        public static ErrorClassification Classify(Exception? exception)
        {
            if (exception is null)
                return ErrorClassification.Unknown;

            return ClassifyInternal(exception);
        }

        private static ErrorClassification ClassifyInternal(Exception exception)
        {
            // Check type by comparing full name to avoid assembly reference issues
            var exceptionType = exception.GetType();
            var fullName = exceptionType.FullName ?? "";
            var message = exception.Message ?? "";

            // Validation and argument errors
            if (exceptionType == typeof(ArgumentException) ||
                exceptionType == typeof(ArgumentNullException) ||
                exceptionType == typeof(ArgumentOutOfRangeException) ||
                exceptionType == typeof(InvalidOperationException) ||
                exceptionType == typeof(FormatException))
            {
                return ErrorClassification.ValidationError;
            }

            // File system errors
            if (exceptionType == typeof(FileNotFoundException) ||
                exceptionType == typeof(DirectoryNotFoundException) ||
                exceptionType == typeof(IOException) ||
                exceptionType == typeof(PathTooLongException))
            {
                return ErrorClassification.FileSystemError;
            }

            // Permission errors
            if (exceptionType == typeof(UnauthorizedAccessException))
            {
                return ErrorClassification.PermissionError;
            }

            // Timeout errors
            if (exceptionType == typeof(TimeoutException) ||
                exceptionType == typeof(OperationCanceledException))
            {
                return ErrorClassification.TimeoutError;
            }

            // Database errors - check by type name and message content
            if (fullName.Contains("DbException") || 
                fullName.Contains("SqlException") ||
                fullName.Contains("DataException") ||
                message.Contains("database", StringComparison.OrdinalIgnoreCase) ||
                message.Contains("sql", StringComparison.OrdinalIgnoreCase))
            {
                return ErrorClassification.DatabaseError;
            }

            // Network errors
            if (exceptionType == typeof(HttpRequestException) ||
                fullName.Contains("HttpRequestException"))
            {
                return ErrorClassification.NetworkError;
            }

            // Configuration errors
            if (exceptionType == typeof(InvalidOperationException) ||
                message.Contains("configuration", StringComparison.OrdinalIgnoreCase))
            {
                return ErrorClassification.ConfigurationError;
            }

            // Default to Unknown for unclassified exceptions
            return ErrorClassification.Unknown;
        }

        /// <summary>
        /// Gets a human-readable description of an error classification.
        /// </summary>
        public static string GetDescription(ErrorClassification classification)
        {
            return classification switch
            {
                ErrorClassification.ValidationError => "Validation Error",
                ErrorClassification.FileSystemError => "File System Error",
                ErrorClassification.DatabaseError => "Database Error",
                ErrorClassification.NetworkError => "Network Error",
                ErrorClassification.PermissionError => "Permission Error",
                ErrorClassification.TimeoutError => "Timeout Error",
                ErrorClassification.ConfigurationError => "Configuration Error",
                ErrorClassification.SystemError => "System Error",
                ErrorClassification.JobExecutionError => "Job Execution Error",
                _ => "Unknown Error"
            };
        }
    }
}
