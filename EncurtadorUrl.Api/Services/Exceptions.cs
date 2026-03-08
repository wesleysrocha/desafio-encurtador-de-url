namespace UrlShortener.Api.Services;

// 400
public sealed class ValidationException(string message) : Exception(message);

// 401
public sealed class UnauthorizedException(string message) : Exception(message);

// 404
public sealed class NotFoundException(string message) : Exception(message);

// 409
public sealed class ConflictException(string message) : Exception(message);

// 410
public sealed class ExpiredException(string message) : Exception(message);