namespace InteractiveEditor.Options;

public readonly record struct NumericRange
{
    public NumericRange(double min, double max, double step = 0)
    {
        if (min > max)
            throw new ArgumentException($"Range minimum ({min}) is greater than its maximum ({max}).");

        if (step < 0)
            throw new ArgumentOutOfRangeException(nameof(step), step, "Range step cannot be negative.");

        Min = min;
        Max = max;
        Step = step;
    }

    public double Min { get; }
    public double Max { get; }
    public double Step { get; }
}
