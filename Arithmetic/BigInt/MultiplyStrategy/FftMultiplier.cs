using System.Numerics;
using Arithmetic.BigInt.Interfaces;

namespace Arithmetic.BigInt.MultiplyStrategy;

internal class FftMultiplier : IMultiplier
{
    public BetterBigInteger Multiply(BetterBigInteger a, BetterBigInteger b)
    {
        if (a.IsZero() || b.IsZero())
        {
            return new BetterBigInteger(new uint[] { 0 });
        }

        var aChunks = Get16BitChunks(a);
        var bChunks = Get16BitChunks(b);

        int n = 1;
        while (n < aChunks.Length + bChunks.Length)
        {
            n <<= 1;
        }

        var fa = new Complex[n];
        var fb = new Complex[n];

        for (int i = 0; i < aChunks.Length; i++)
        {
            fa[i] = new Complex(aChunks[i], 0);
        }

        for (int i = 0; i < bChunks.Length; i++)
        {
            fb[i] = new Complex(bChunks[i], 0);
        }

        Fft(fa, false);
        Fft(fb, false);

        for (int i = 0; i < n; i++)
        {
            fa[i] *= fb[i];
        }

        Fft(fa, true);
        var res16 = new List<ushort>(n);
        ulong carry = 0;
        for (int i = 0; i < n; i++)
        {
            ulong val = (ulong)Math.Round(fa[i].Real) + carry;
            
            res16.Add((ushort)(val & 0xFFFF));
            
            carry = val >> 16;
        }

        while (carry > 0)
        {
            res16.Add((ushort)(carry & 0xFFFF));
            carry >>= 16;
        }

        var res32 = new List<uint>((res16.Count + 1) / 2);
        for (int i = 0; i < res16.Count; i += 2)
        {
            uint lower = res16[i];
            uint upper = (i + 1 < res16.Count) ? res16[i + 1] : 0u;
            res32.Add(lower | (upper << 16));
        }

        int len = res32.Count;
        while (len > 1 && res32[len - 1] == 0) len--;

        bool isNegative = a.IsNegative ^ b.IsNegative;
        
        return new BetterBigInteger(res32.Take(len).ToArray(), isNegative);
    }

    private static ushort[] Get16BitChunks(BetterBigInteger num)
    {
        var digits = num.GetDigits();
        var result = new List<ushort>(digits.Length * 2);
        
        foreach (var d in digits)
        {
            result.Add((ushort)(d & 0xFFFF));
            result.Add((ushort)(d >> 16));
        }

        int count = result.Count;
        while (count > 1 && result[count - 1] == 0) count--;
        
        return result.Take(count).ToArray();
    }

    private static void Fft(Complex[] a, bool invert)
    {
        int n = a.Length;

        for (int i = 1, j = 0; i < n; i++)
        {
            int bit = n >> 1;
            for (; j >= bit; bit >>= 1)
            {
                j -= bit;
            }

            j += bit;
            if (i < j)
            {
                (a[i], a[j]) = (a[j], a[i]);
            }
        }

        for (int len = 2; len <= n; len <<= 1)
        {
            double angle = 2 * Math.PI / len * (invert ? -1 : 1);
            Complex wlen = new Complex(Math.Cos(angle), Math.Sin(angle));
            
            for (int i = 0; i < n; i += len)
            {
                Complex w = 1;
                for (int j = 0; j < len / 2; j++)
                {
                    Complex u = a[i + j];
                    Complex v = a[i + j + len / 2] * w;
                    
                    a[i + j] = u + v;
                    a[i + j + len / 2] = u - v;
                    w *= wlen;
                }
            }
        }

        if (invert)
        {
            for (int i = 0; i < n; i++)
            {
                a[i] /= n;
            }
        }
    }
}