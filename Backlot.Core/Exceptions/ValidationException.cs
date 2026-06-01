using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Backlot.Core.Exceptions;

/// <summary>
/// Validation exceptions are thrown when a scenario is not valid
/// its using ValidationResult from DataAnnotations to give more details 
/// </summary>
public class ValidationException : ArgumentException
{
    public IEnumerable<ValidationResult> Validations { get; }

    public ValidationException(IEnumerable<ValidationResult> validations, string message) : base(message)
    {
        Validations = validations;
    }
}