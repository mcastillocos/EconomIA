using System.Text.RegularExpressions;

namespace EconomIA.Domain.ValueObjects;

public partial record ISIN
{
    public string Value { get; }

    public ISIN(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new Exceptions.DomainException("ISIN cannot be empty.");

        value = value.ToUpperInvariant().Trim();

        if (!IsValidFormat(value))
            throw new Exceptions.DomainException($"Invalid ISIN format: {value}. Must be 2 letters + 9 alphanumeric + 1 check digit.");

        Value = value;
    }

    private static bool IsValidFormat(string isin)
    {
        return IsinRegex().IsMatch(isin);
    }

    [GeneratedRegex(@"^[A-Z]{2}[A-Z0-9]{9}[0-9]$")]
    private static partial Regex IsinRegex();

    public override string ToString() => Value;
}
