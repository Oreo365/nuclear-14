namespace Content.Shared._Misfits.Random;

public struct Xoroshiro64S
{
    private ulong _state0;
    private ulong _state1;

    public Xoroshiro64S(ulong seed)
    {
        _state0 = SplitMix64.Next(ref seed);
        _state1 = SplitMix64.Next(ref seed);
    }

    public float NextFloat(float minValue, float maxValue)
    {
        var value = NextUInt32() / (float) uint.MaxValue;
        return minValue + value * (maxValue - minValue);
    }

    private uint NextUInt32()
    {
        var result = _state0 + _state1;
        _state1 ^= _state0;
        _state0 = RotateLeft(_state0, 55) ^ _state1 ^ (_state1 << 14);
        _state1 = RotateLeft(_state1, 36);
        return (uint) result;
    }

    private static ulong RotateLeft(ulong value, int count)
    {
        return (value << count) | (value >> (64 - count));
    }
}
