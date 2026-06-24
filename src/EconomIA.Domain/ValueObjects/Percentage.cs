namespace EconomIA.Domain.ValueObjects;

public record Percentage
{
    public decimal Value { get; }

    public Percentage(decimal value)
    {
        Value = value;
    }

    public static Percentage FromDecimal(decimal value) => new(value);
    public static Percentage Zero => new(0m);

    public override string ToString() => $"{Value:N2}%";
}
