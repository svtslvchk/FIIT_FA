using Arithmetic.BigInt.Interfaces;

namespace Arithmetic.BigInt.MultiplyStrategy;

internal class KaratsubaMultiplier : IMultiplier
{
    private readonly SimpleMultiplier _simpleMultiplier = new SimpleMultiplier();
    public BetterBigInteger Multiply(BetterBigInteger a, BetterBigInteger b)
    {
        var da = a.GetDigits().ToArray();
        var db = b.GetDigits().ToArray();
        var res = Karatsuba(da, db);
        bool isNegative = a.IsNegative ^ b.IsNegative;
        return new BetterBigInteger(res, isNegative);
    }

    private uint[] PadToLength(uint[] arr, int len)
    {
        if (arr.Length >= len)
        {
            return arr;
        }

        uint[] padded = new uint[len];
        Array.Copy(arr, padded, arr.Length);
        return padded;
    }

    private uint[] GetLow(uint[] arr, int half)
    {
        int len = Math.Min(half, arr.Length);
        uint[] res = new uint[len];
        Array.Copy(arr, 0, res, 0, len);
        return res;
    }

    private uint[] GetHigh(uint[] arr, int half)
    {
        if (half >= arr.Length)
        {
            return new uint[0];
        }

        int len = arr.Length - half;
        uint[] res = new uint[len];
        Array.Copy(arr, half, res, 0, len);
        return res;
    }

    private uint[] AddArrays(uint[] a, uint[] b)
    {
        var ta = new BetterBigInteger(a);
        var tb = new BetterBigInteger(b);
        var sum = BetterBigInteger.AddAbs(ta, tb);
        return sum.GetDigits().ToArray();
    }

    private uint[] SubArrays(uint[] a, uint[] b)
    {
        var ta = new BetterBigInteger(a);
        var tb = new BetterBigInteger(b);
        var diff = BetterBigInteger.SubAbs(ta, tb);
        return diff.GetDigits().ToArray();
    }

    private void AddToResult(uint[] target, uint[] source, int shift)
    {
        ulong carry = 0;
        for (int i = 0; i < source.Length; i++)
        {
            ulong val = (ulong)target[i + shift] + (ulong)source[i] + carry;
            target[i + shift] = (uint)val;
            carry = val >> 32;
        }

        if (carry != 0)
        {
            target[shift + source.Length] += (uint)carry;
        }
    }

    private uint[] TrimArray(uint[] arr)
    {
        int len = arr.Length;
        while (len > 1 && arr[len - 1] == 0)
        {
            len--;
        }

        if (len == arr.Length)
        {
            return arr;
        }

        uint[] trimmed = new uint[len];
        Array.Copy(arr, trimmed, len);
        return trimmed;
    }

    private uint[] Karatsuba(uint[] a, uint[] b)
    {
        int n = Math.Max(a.Length, b.Length);
        if (n <= 32)
        {
            var tempA = new BetterBigInteger(a, false);
            var tempB = new BetterBigInteger(b, false);
            var product = _simpleMultiplier.Multiply(tempA, tempB);
            return product.GetDigits().ToArray();
        }

        a = PadToLength(a, n);
        b = PadToLength(b, n);
        int half = n / 2;

        uint[] aLow = GetLow(a, half);
        uint[] aHigh = GetHigh(a, half);
        uint[] bLow = GetLow(b, half);
        uint[] bHigh = GetHigh(b, half);

        uint[] z0 = Karatsuba(aLow, bLow);           // X0 * Y0
        uint[] z2 = Karatsuba(aHigh, bHigh);        // X1 * Y1

        // (X0 + X1) и (Y0 + Y1)
        var sumA = AddArrays(aLow, aHigh);
        var sumB = AddArrays(bLow, bHigh);
        uint[] z1 = Karatsuba(sumA, sumB);          // (X0+X1)*(Y0+Y1)

        // z1 = z1 - z0 - z2
        z1 = SubArrays(z1, z0);
        z1 = SubArrays(z1, z2);

        uint[] result = new uint[a.Length + b.Length];
        AddToResult(result, z0, 0);        
        AddToResult(result, z1, half);    
        AddToResult(result, z2, half * 2);

        return TrimArray(result);
    }
}