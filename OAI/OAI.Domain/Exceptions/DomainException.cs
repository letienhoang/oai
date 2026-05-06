using System;
using System.Collections.Generic;
using System.Text;

namespace OAI.Domain.Exceptions;

public class DomainException : Exception
{
    public string? Code { get; }
    public IReadOnlyDictionary<string, string>? Parameters { get; }

    public DomainException(string message) : base(message)
    {
    }

    public DomainException(
        string message,
        string code,
        IReadOnlyDictionary<string, string>? parameters = null)
        : base(message)
    {
        Code = code;
        Parameters = parameters;
    }
}
