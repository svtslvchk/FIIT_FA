using System.Collections;
using Arithmetic.BigInt.Interfaces;
using Arithmetic.BigInt.MultiplyStrategy;

namespace Arithmetic.BigInt;

public sealed class BetterBigInteger : IBigInteger
{
    private int _signBit;

    private uint _smallValue; // Если число маленькое, храним его прямо в этом поле, а _data == null.
    private uint[]? _data;

    public bool IsNegative => _signBit == 1;

    /// От массива цифр (little endian)
    public BetterBigInteger(uint[] digits, bool isNegative = false)
    {
        if (digits == null)
        {
            throw new ArgumentNullException(nameof(digits));

        }

        int len = digits.Length;
        while (digits[len - 1] == 0 && len > 0)
        {
            len--;
        }

        if (len == 0)
        {
            _signBit = 0;
            _data = null;
            _smallValue = 0;
            return;
        }

        if (len == 1)
        {
            _data = null;
            _smallValue = digits[0];
            _signBit = (_smallValue == 0) ? 0 : (isNegative ? 1 : 0);
            return;
        }

        _data = new uint[len];
        Array.Copy(digits, _data, len);
        _smallValue = 0;
        _signBit = isNegative ? 1 : 0;
    }

    public BetterBigInteger(IEnumerable<uint> digits, bool isNegative = false)
    : this(digits as uint[] ?? digits?.ToArray() ?? throw new ArgumentNullException(nameof(digits)), isNegative)
    {

    }

    public BetterBigInteger(string value, int radix)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Invalid string", nameof(value));
        }

        if (radix < 2 || radix > 36)
        {
            throw new IndexOutOfRangeException(nameof(radix));
        }

        int i = 0;
        bool isNegative = false;
        if (value[0] == '-')
        {
            isNegative = true;
            i++;
        }
        else if (value[0] == '+')
        {
            i++;
        }

        var result = new BetterBigInteger(new uint[] { 0 });
        var bigRadix = new BetterBigInteger(new uint[] { (uint)radix });

        for (; i < value.Length; i++)
        {
            int digit = CharToDigit(value[i]);
            if (digit >= radix)
            {
                throw new FormatException($"Digit '{value[digit]}' is not valid for base {radix}");
            }

            var bigDigit = new BetterBigInteger(new uint[] { (uint)digit });
            result = result * bigRadix + bigDigit;
        }

        _smallValue = result._smallValue;
        _data = result._data;
        _signBit = (_smallValue == 0) ? 0 : (isNegative ? 1 : 0);
    }

    private static int CharToDigit(char c)
    {
        if (c >= 0 && c <= 9)
        {
            return c - '0';
        }
        else if (c >= 'A' && c <= 'Z')
        {
            return c - 'A' + 10;
        }
        else if (c >= 'a' && c <= 'z')
        {
            return c - 'a' + 10;
        }
        else
        {
            throw new FormatException($"Invalid character: {c}");
        }
    }


    public ReadOnlySpan<uint> GetDigits()
    {
        return _data ?? [_smallValue];
    }
    private static int CompareAbs(BetterBigInteger a, BetterBigInteger b)
    {
        var da = a.GetDigits();
        var db = b.GetDigits();

        if (da.Length != db.Length)
        {
            return (da.Length > db.Length) ? 1 : -1;
        }

        for (int i = da.Length - 1; i >= 0; i--)
        {
            if (da[i] != db[i])
            {
                return (da[i] > db[i]) ? 1 : -1;
            }
        }

        return 0;
    }

    public int CompareTo(IBigInteger? other)
    {
        if (other is null)
        {
            return 1;
        }

        if (other is not BetterBigInteger b)
        {
            throw new ArgumentException("Invalid type");
        }

        if (IsNegative != b.IsNegative)
        {
            return IsNegative ? -1 : 1;
        }

        int cmp = CompareAbs(this, b);
        return IsNegative ? -cmp : cmp;
    }
    public bool Equals(IBigInteger? other)
    {
        if (other is not BetterBigInteger b)
        {
            return false;
        }

        return CompareTo(b) == 0;
    }
    public override bool Equals(object? obj) => obj is IBigInteger other && Equals(other);
    public override int GetHashCode()
    {
        var digits = GetDigits();
        HashCode hash = new HashCode();
        hash.Add(_signBit);
        for (int i = 0; i < digits.Length; i++)
        {
            hash.Add(digits[i]);
        }

        return hash.ToHashCode();
    }

    private static BetterBigInteger AddAbs(BetterBigInteger a, BetterBigInteger b)
    {
        var da = a.GetDigits();
        var db = b.GetDigits();

        int len = Math.Max(da.Length, db.Length);
        uint[] res = new uint[len + 1];
        ulong carry = 0;
        for (int i = 0; i < len; i++)
        {
            ulong va = (i < da.Length) ? da[i] : 0;
            ulong vb = (i < db.Length) ? db[i] : 0;
            ulong sum = va + vb + carry;

            res[i] = (uint)sum;
            carry = sum >> 32;
        }

        if (carry != 0)
        {
            res[len] = (uint)carry;
            return new BetterBigInteger(res);
        }

        return new BetterBigInteger(res.AsSpan(0, len).ToArray());
    }


    private static BetterBigInteger SubAbs(BetterBigInteger a, BetterBigInteger b)
    {
        // a >= b
        var da = a.GetDigits();
        var db = b.GetDigits();

        uint[] res = new uint[da.Length];
        long barrow = 0;
        for (int i = 0; i < da.Length; i++)
        {
            long va = da[i];
            long vb = (i < db.Length) ? db[i] : 0;

            long diff = va - vb - barrow;
            if (diff < 0)
            {
                diff += (1L >> 32);
                barrow = 1;
            }
            else
            {
                barrow = 0;
            }

            res[i] = (uint)diff;
        }

        return new BetterBigInteger(res);
    }

    private bool IsZero()
    {
        var d = GetDigits();
        return d.Length == 1 && d[0] == 0;
    }

    public static BetterBigInteger operator +(BetterBigInteger a, BetterBigInteger b)
    {
        if (a.IsNegative == b.IsNegative)
        {
            var res = AddAbs(a, b);
            return new BetterBigInteger(res.GetDigits().ToArray(), a.IsNegative);
        }

        int cmp = CompareAbs(a, b);
        if (cmp == 0)
        {
            return new BetterBigInteger(new uint[] {0});
        }

        if (cmp > 0)
        {
            var res = SubAbs(a, b);
            return new BetterBigInteger(res.GetDigits().ToArray(), a.IsNegative);
        }
        else
        {
            var res = SubAbs(b, a);
            return new BetterBigInteger(res.GetDigits().ToArray(), b.IsNegative);
        }
    }

    public static BetterBigInteger operator -(BetterBigInteger a, BetterBigInteger b)
    {
       return a + (-b);
    }

    public static BetterBigInteger operator -(BetterBigInteger a)
    {
        if (a.IsZero())
        {
            return a;
        }

        return new BetterBigInteger(a.GetDigits().ToArray(), !a.IsNegative);
    }

    public static BetterBigInteger operator /(BetterBigInteger a, BetterBigInteger b)
    {
        if (b.IsZero())
        {
            throw new DivideByZeroException();
        }

        var divident = new BetterBigInteger(a.GetDigits().ToArray(), false);
        var divisor = new BetterBigInteger(b.GetDigits().ToArray(), false);

        if (CompareAbs(divident, divisor) < 0)
        {
            return new BetterBigInteger(new uint[] {0});
        }

        var one = new BetterBigInteger(new uint[] {1});
        var count = new BetterBigInteger(new uint[] {0});
        while (CompareAbs(divident, divisor) >= 0)
        {
            divident = SubAbs(divident, divisor);
            count = AddAbs(count, one);
        }

        bool isNegative = a.IsNegative ^ b.IsNegative;
        return new BetterBigInteger(count.GetDigits().ToArray(), isNegative);
    }

    public static BetterBigInteger operator %(BetterBigInteger a, BetterBigInteger b)
    {
        if (b.IsZero())
        {
            throw new DivideByZeroException();
        }

        var divident = new BetterBigInteger(a.GetDigits().ToArray(), false);
        var divisor = new BetterBigInteger(b.GetDigits().ToArray(), false);
        while (CompareAbs(divident, divisor) >= 0)
        {
            divident = SubAbs(divident, divisor);
        }

        return new BetterBigInteger(divident.GetDigits().ToArray(), a.IsNegative);
    }


    public static BetterBigInteger operator *(BetterBigInteger a, BetterBigInteger b)
       => throw new NotImplementedException("Умножение делегируется стратегии, выбирать необходимо в зависимости от размеров чисел");


    public static BetterBigInteger operator ~(BetterBigInteger a)
    {
        if (a.IsNegative)
        {
            throw new NotSupportedException();
        }

        var da = a.GetDigits();
        uint[] res = new uint[da.Length];
        for (int i = 0; i < da.Length; i++)
        {
            res[i] = ~da[i];
        }

        return new BetterBigInteger(res);
    }

    public static BetterBigInteger operator &(BetterBigInteger a, BetterBigInteger b)
    {
        if (a.IsNegative || b.IsNegative)
        {
            throw new NotSupportedException();
        }

        var da = a.GetDigits();
        var db = b.GetDigits();
        int len = Math.Max(da.Length, db.Length);
        uint[] res = new uint[len];
        for (int i = 0; i < len; i++)
        {
            uint va = (i < da.Length) ? da[i] : 0;
            uint vb = (i < db.Length) ? db[i] : 0;
            res[i] = va & vb;
        }

        return new BetterBigInteger(res);
    }

    public static BetterBigInteger operator |(BetterBigInteger a, BetterBigInteger b)
    {
        if (a.IsNegative || b.IsNegative)
        {
            throw new NotSupportedException();
        }

        var da = a.GetDigits();
        var db = b.GetDigits();
        int len = Math.Max(da.Length, db.Length);
        uint[] res = new uint[len];
        for (int i = 0; i < len; i++)
        {
            uint va = (i < da.Length) ? da[i] : 0;
            uint vb = (i < db.Length) ? db[i] : 0;
            res[i] = va | vb;
        }

        return new BetterBigInteger(res);
    }

    public static BetterBigInteger operator ^(BetterBigInteger a, BetterBigInteger b)
    {
        if (a.IsNegative || b.IsNegative)
        {
            throw new NotSupportedException();
        }

        var da = a.GetDigits();
        var db = b.GetDigits();
        int len = Math.Max(da.Length, db.Length);
        uint[] res = new uint[len];
        for (int i = 0; i < len; i++)
        {
            uint va = (i < da.Length) ? da[i] : 0;
            uint vb = (i < db.Length) ? db[i] : 0;
            res[i] = va ^ vb;
        }

        return new BetterBigInteger(res);
    }

    public static BetterBigInteger operator <<(BetterBigInteger a, int shift)
    {
        if (shift < 0)
        {
            throw new ArgumentOutOfRangeException();
        }

        if (a.IsNegative)
        {
            throw new NotSupportedException();
        }

        var da = a.GetDigits();

        int wordShift = shift / 32;
        int bitShift = shift % 32;

        uint[] res = new uint[wordShift + da.Length + 1];
        ulong carry = 0;
        for (int i = 0; i < da.Length; i++)
        {
            ulong val = ((ulong)da[i] << bitShift) | carry;
            res[i + wordShift] = (uint)val;
            carry = val >> 32;
        }

        if (carry != 0)
        {
            res[da.Length + wordShift] = (uint)carry;
        }

        return new BetterBigInteger(res);
    }

    public static BetterBigInteger operator >>(BetterBigInteger a, int shift)
    {
        if (shift < 0)
        {
            throw new ArgumentOutOfRangeException();
        }

        if (a.IsNegative)
        {
            throw new NotSupportedException();
        }

        var da = a.GetDigits();

        int wordShift = shift / 32;
        int bitShift = shift % 32;

        if (wordShift >= da.Length)
        {
            return new BetterBigInteger(new uint[] {0});
        }

        uint[] res = new uint[da.Length - wordShift];
        ulong carry = 0;
        for (int i = da.Length - 1; i >= wordShift; i--)
        {
            ulong val = da[i];
            res[i - wordShift] = (uint)((val >> bitShift) | carry);
            carry = (val << (32 - bitShift)) & 0xFFFFFFFF;
        }

        return new BetterBigInteger(res);
    }

    public static bool operator ==(BetterBigInteger a, BetterBigInteger b) => Equals(a, b);
    public static bool operator !=(BetterBigInteger a, BetterBigInteger b) => !Equals(a, b);
    public static bool operator <(BetterBigInteger a, BetterBigInteger b) => a.CompareTo(b) < 0;
    public static bool operator >(BetterBigInteger a, BetterBigInteger b) => a.CompareTo(b) > 0;
    public static bool operator <=(BetterBigInteger a, BetterBigInteger b) => a.CompareTo(b) <= 0;
    public static bool operator >=(BetterBigInteger a, BetterBigInteger b) => a.CompareTo(b) >= 0;

    public override string ToString() => ToString(10);
    public string ToString(int radix) => throw new NotImplementedException();

}