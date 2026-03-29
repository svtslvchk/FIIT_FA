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

    private int GetLength()
    {
        return GetDigits().Length;
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
    public bool Equals(IBigInteger? other) => throw new NotImplementedException();
    public override bool Equals(object? obj) => obj is IBigInteger other && Equals(other);
    public override int GetHashCode() => throw new NotImplementedException();


    public static BetterBigInteger operator +(BetterBigInteger a, BetterBigInteger b) => throw new NotImplementedException();
    public static BetterBigInteger operator -(BetterBigInteger a, BetterBigInteger b) => throw new NotImplementedException();
    public static BetterBigInteger operator -(BetterBigInteger a) => throw new NotImplementedException();
    public static BetterBigInteger operator /(BetterBigInteger a, BetterBigInteger b) => throw new NotImplementedException();
    public static BetterBigInteger operator %(BetterBigInteger a, BetterBigInteger b) => throw new NotImplementedException();


    public static BetterBigInteger operator *(BetterBigInteger a, BetterBigInteger b)
       => throw new NotImplementedException("Умножение делегируется стратегии, выбирать необходимо в зависимости от размеров чисел");

    public static BetterBigInteger operator ~(BetterBigInteger a) => throw new NotImplementedException();
    public static BetterBigInteger operator &(BetterBigInteger a, BetterBigInteger b) => throw new NotImplementedException();
    public static BetterBigInteger operator |(BetterBigInteger a, BetterBigInteger b) => throw new NotImplementedException();
    public static BetterBigInteger operator ^(BetterBigInteger a, BetterBigInteger b) => throw new NotImplementedException();
    public static BetterBigInteger operator <<(BetterBigInteger a, int shift) => throw new NotImplementedException();
    public static BetterBigInteger operator >>(BetterBigInteger a, int shift) => throw new NotImplementedException();

    public static bool operator ==(BetterBigInteger a, BetterBigInteger b) => Equals(a, b);
    public static bool operator !=(BetterBigInteger a, BetterBigInteger b) => !Equals(a, b);
    public static bool operator <(BetterBigInteger a, BetterBigInteger b) => a.CompareTo(b) < 0;
    public static bool operator >(BetterBigInteger a, BetterBigInteger b) => a.CompareTo(b) > 0;
    public static bool operator <=(BetterBigInteger a, BetterBigInteger b) => a.CompareTo(b) <= 0;
    public static bool operator >=(BetterBigInteger a, BetterBigInteger b) => a.CompareTo(b) >= 0;

    public override string ToString() => ToString(10);
    public string ToString(int radix) => throw new NotImplementedException();

}