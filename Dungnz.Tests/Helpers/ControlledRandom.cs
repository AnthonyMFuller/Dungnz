namespace Dungnz.Tests.Helpers;

/// <summary>Predictable Random for testing flee outcomes.</summary>
public class ControlledRandom : Random
{
    private readonly Queue<double> _doubleValues;
    private readonly double _defaultDouble;

    public ControlledRandom(double defaultDouble = 0.95, params double[] additionalDoubles)
    {
        _defaultDouble = defaultDouble;
        _doubleValues = new Queue<double>(additionalDoubles);
    }

    public override double NextDouble() =>
        _doubleValues.Count > 0 ? _doubleValues.Dequeue() : _defaultDouble;

    public override int Next(int maxValue) => 0;
    public override int Next(int minValue, int maxValue) => minValue;
}
