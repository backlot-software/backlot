namespace Backlot.Http;

public class BadRequestException(string message) : ApplicationException(message);