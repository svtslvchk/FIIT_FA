using Arithmetic.BigInt.Interfaces;

namespace Arithmetic.BigInt.MultiplyStrategy;

internal class SimpleMultiplier : IMultiplier
{
    public BetterBigInteger Multiply(BetterBigInteger a, BetterBigInteger b)
    {
        if (a.IsZero() || b.IsZero())
        {
            return new BetterBigInteger(new uint[] { 0 });
        }

        var da = a.GetDigits();
        var db = b.GetDigits();
        uint[] res = new uint[da.Length + db.Length];
        for (int i = 0; i < da.Length; i++)
        {
            ulong carry = 0;
            for (int j = 0; j < db.Length; j++)
            {
                ulong val = (ulong)da[i] * db[j] + res[i + j] + carry;
                res[i + j] = (uint)val;
                carry = val >> 32;
            }

            if (carry != 0)
            {
                res[i + db.Length] = (uint)carry;
            }
        }

        bool isNegative = a.IsNegative ^ b.IsNegative;
        return new BetterBigInteger(res, isNegative);
    }
}