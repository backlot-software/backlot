using System;

namespace Backlot.Core.Exceptions;

public class NotFoundException(string message) : ApplicationException(message);