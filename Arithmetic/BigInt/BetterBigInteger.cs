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
        while (len > 0 && digits[len - 1] == 0)
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
    : this(digits?.ToArray() ?? throw new ArgumentNullException(nameof(digits)), isNegative)
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
                throw new FormatException($"Digit '{value[i]}' is not valid for base {radix}");
            }

            var bigDigit = new BetterBigInteger(new uint[] { (uint)digit });
            result = result * bigRadix + bigDigit;
        }

        var resultDigits = result.GetDigits();
        if (resultDigits.Length == 1 && resultDigits[0] == 0)
        {
            _signBit = 0;
            _data = null;
            _smallValue = 0;
        }
        else if (resultDigits.Length == 1)
        {
            _data = null;
            _smallValue = resultDigits[0];
            _signBit = isNegative ? 1 : 0;
        }
        else
        {
            _data = new uint[resultDigits.Length];
            resultDigits.CopyTo(_data);
            _smallValue = 0;
            _signBit = isNegative ? 1 : 0;
        }
    }

    private static int CharToDigit(char c)
    {
        if (c >= '0' && c <= '9')
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
        if (da.Length == 0 || db.Length == 0)
        {
            return da.Length.CompareTo(db.Length);
        }

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
            int result = IsNegative ? -1 : 1;
            return result;
        }

        int cmp = CompareAbs(this, b);
        int finalResult = IsNegative ? -cmp : cmp;
        return finalResult;
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

    public static BetterBigInteger AddAbs(BetterBigInteger a, BetterBigInteger b)
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


    public static BetterBigInteger SubAbs(BetterBigInteger a, BetterBigInteger b)
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
                diff += (1L << 32);
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

    public bool IsZero()
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
            return new BetterBigInteger(new uint[] { 0 });
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

    public int GetBitLength()
    {
        var digits = GetDigits();
        if (digits.Length == 0)
        {
            return 0;
        }

        int count = 32 * (digits.Length - 1);
        count += 32 - System.Numerics.BitOperations.LeadingZeroCount(digits[digits.Length - 1]);
        return count;
    }

    public bool GetBit(int index)
    {
        int wordIdx = index / 32;
        int bitIdx = index % 32;
        var digits = GetDigits();
        if (wordIdx >= digits.Length)
        {
            return false;
        }

        return (digits[wordIdx] & (1u << bitIdx)) != 0;
    }

    private static (BetterBigInteger q, BetterBigInteger r) DivRem(BetterBigInteger a, BetterBigInteger b)
    {
        if (b.IsZero())
        {
            throw new DivideByZeroException();
        }

        BetterBigInteger divident = new BetterBigInteger(a.GetDigits().ToArray());
        BetterBigInteger divisor = new BetterBigInteger(b.GetDigits().ToArray());
        if (divident < divisor)
        {
            return (new BetterBigInteger(new uint[] { 0 }), divident);
        }

        BetterBigInteger q = new BetterBigInteger(new uint[] { 0 });
        BetterBigInteger r = new BetterBigInteger(new uint[] { 0 });
        for (int i = divident.GetBitLength() - 1; i >= 0; i--)
        {
            r <<= 1;
            if (divident.GetBit(i))
            {
                r += new BetterBigInteger(new uint[] { 1 });
            }

            if (r >= divisor)
            {
                r -= divisor;
                q |= (new BetterBigInteger(new uint[] { 1 }) << i);
            }
        }

        return (q, r);
    }

    public static BetterBigInteger operator /(BetterBigInteger a, BetterBigInteger b)
    {
        var q = DivRem(a, b).Item1;
        bool isNegative = a.IsNegative ^ b.IsNegative;
        return new BetterBigInteger(q.GetDigits().ToArray(), isNegative);
    }

    public static BetterBigInteger operator %(BetterBigInteger a, BetterBigInteger b)
    {
        var r = DivRem(a, b).r;
        return new BetterBigInteger(r.GetDigits().ToArray(), a.IsNegative);
    }

    public static BetterBigInteger operator *(BetterBigInteger a, BetterBigInteger b)
    {
        if (a.IsZero() || b.IsZero())
        {
            return new BetterBigInteger(new uint[] { 0 });
        }

        IMultiplier strategy;
        int size = Math.Max(a.GetDigits().Length, b.GetDigits().Length);
        if (size < 64)
        {
            strategy = new SimpleMultiplier();
        }
        else if (size < 512)
        {
            strategy = new KaratsubaMultiplier();
        }
        else
        {
            strategy = new FftMultiplier();
        }

        return strategy.Multiply(a, b);
    }
    private static uint[] ToTwosComplement(uint[] digits, bool isNegative)
    {
        if (!isNegative)
        {
            return digits;
        }

        uint[] result = new uint[digits.Length];
        Array.Copy(digits, result, digits.Length);

        for (int i = 0; i < result.Length; i++)
        {
            result[i] = ~result[i];
        }

        ulong carry = 1;
        for (int i = 0; i < result.Length && carry > 0; i++)
        {
            ulong sum = (ulong)result[i] + carry;
            result[i] = (uint)sum;
            carry = sum >> 32;
        }

        return result;
    }
    private static (uint[] digits, bool isNegative) FromTwosComplement(uint[] complement)
    {
        if (complement.Length == 0)
        {
            return (new uint[] { 0 }, false);
        }

        bool isNegative = (complement[complement.Length - 1] & (1u << 31)) != 0;

        if (!isNegative)
        {
            return (complement, false);
        }

        uint[] result = new uint[complement.Length];
        Array.Copy(complement, result, complement.Length);

        long borrow = 1;
        for (int i = 0; i < result.Length; i++)
        {
            long diff = (long)result[i] - borrow;
            if (diff < 0)
            {
                diff += (1L << 32);
                borrow = 1;
            }
            else
            {
                borrow = 0;
            }

            result[i] = (uint)diff;
        }

        for (int i = 0; i < result.Length; i++)
        {
            result[i] = ~result[i];
        }

        return (result, true);
    }

    private static uint[] ExtendAndSign(uint[] digits, int targetLen, bool isNegative)
    {
        uint[] res = new uint[targetLen];
        Array.Copy(digits, res, digits.Length);
        if (isNegative)
        {
            for (int i = digits.Length; i < targetLen; i++)
            {
                res[i] = ~0u;
            }
        }

        return res;
    }

    private static BetterBigInteger PerformBitwise(
        BetterBigInteger a, BetterBigInteger b, Func<uint, uint, uint> operation)
    {
        var ca = ToTwosComplement(a.GetDigits().ToArray(), a.IsNegative);
        var cb = ToTwosComplement(b.GetDigits().ToArray(), b.IsNegative);

        int len = Math.Max(ca.Length, cb.Length) + 1;
        uint[] extA = ExtendAndSign(ca, len, a.IsNegative);
        uint[] extB = ExtendAndSign(cb, len, b.IsNegative);

        uint[] res = new uint[len];
        for (int i = 0; i < len; i++)
        {
            res[i] = operation(extA[i], extB[i]);
        }

        var (digits, isNegative) = FromTwosComplement(res);
        return new BetterBigInteger(digits, isNegative);
    }

    public static BetterBigInteger operator &(BetterBigInteger a, BetterBigInteger b) => PerformBitwise(a, b, (x, y) => x & y);
    public static BetterBigInteger operator |(BetterBigInteger a, BetterBigInteger b) => PerformBitwise(a, b, (x, y) => x | y);
    public static BetterBigInteger operator ^(BetterBigInteger a, BetterBigInteger b) => PerformBitwise(a, b, (x, y) => x ^ y);

    public static BetterBigInteger operator ~(BetterBigInteger a)
    {
        // ~n = -(n+1)
        if (a.IsZero())
        {
            // ~0 = -1
            return new BetterBigInteger(new uint[] { 1 }, true);
        }

        var one = new BetterBigInteger(new uint[] { 1 });

        if (!a.IsNegative)
        {
            // ~positive = -(positive + 1)
            var plusOne = AddAbs(a, one);
            return new BetterBigInteger(plusOne.GetDigits().ToArray(), true);
        }
        else
        {
            // ~negative = |negative| - 1
            var absValue = new BetterBigInteger(a.GetDigits().ToArray(), false);
            var minusOne = SubAbs(absValue, one);

            if (minusOne.IsZero())
            {
                return new BetterBigInteger(new uint[] { 0 });
            }

            return new BetterBigInteger(minusOne.GetDigits().ToArray(), false);
        }
    }

    public static BetterBigInteger operator <<(BetterBigInteger a, int shift)
    {
        if (shift < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(shift));
        }

        if (shift == 0 || a.IsZero())
        {
            return new BetterBigInteger(a.GetDigits().ToArray(), a.IsNegative);
        }

        var digits = a.GetDigits().ToArray();
        int wordShift = shift / 32;
        int bitShift = shift % 32;

        int newLen = digits.Length + wordShift + (bitShift > 0 ? 1 : 0);
        uint[] result = new uint[newLen];
        ulong carry = 0;

        for (int i = 0; i < digits.Length; i++)
        {
            ulong val = ((ulong)digits[i] << bitShift) | carry;
            result[i + wordShift] = (uint)val;
            carry = val >> 32;
        }

        if (carry != 0)
        {
            result[digits.Length + wordShift] = (uint)carry;
        }

        return new BetterBigInteger(result, a.IsNegative);
    }

    public static BetterBigInteger operator >>(BetterBigInteger a, int shift)
    {
        if (shift < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(shift));
        }

        if (shift == 0 || a.IsZero())
        {
            return new BetterBigInteger(a.GetDigits().ToArray(), a.IsNegative);
        }

        int wordShift = shift / 32;
        int bitShift = shift % 32;
        var digits = a.GetDigits();

        if (wordShift >= digits.Length)
        {
            return a.IsNegative ? new BetterBigInteger(new uint[] { 1 }, true) : new BetterBigInteger(new uint[] { 0 });
        }

        int newLen = digits.Length - wordShift;
        uint[] res = new uint[newLen];

        for (int i = 0; i < newLen; i++)
        {
            uint cur = digits[i + wordShift];
            uint next = (i + wordShift + 1 < digits.Length) ? digits[i + wordShift + 1] : 0;

            if (bitShift == 0)
            {
                res[i] = cur;
            }
            else
            {
                res[i] = (cur >> bitShift) | (next << (32 - bitShift)); // добавляем кусок от более старршего
            }
        }

        var result = new BetterBigInteger(res, a.IsNegative);

        if (a.IsNegative)
        {
            bool discardedBitsAreNonZero = false;
            for (int i = 0; i < wordShift; i++)
            {
                if (digits[i] != 0)
                {
                    discardedBitsAreNonZero = true;
                    break;
                }
            }

            if (!discardedBitsAreNonZero && bitShift > 0)
            {
                uint discardedMask = (1u << bitShift) - 1;
                if ((digits[wordShift] & discardedMask) != 0)
                {
                    discardedBitsAreNonZero = true;
                }
            }

            if (discardedBitsAreNonZero)
            {
                result -= new BetterBigInteger(new uint[] { 1 }); // Округляем вниз
            }
        }

        return result;
    }

    public static bool operator ==(BetterBigInteger a, BetterBigInteger b) => Equals(a, b);
    public static bool operator !=(BetterBigInteger a, BetterBigInteger b) => !Equals(a, b);
    public static bool operator <(BetterBigInteger a, BetterBigInteger b) => a.CompareTo(b) < 0;
    public static bool operator >(BetterBigInteger a, BetterBigInteger b) => a.CompareTo(b) > 0;
    public static bool operator <=(BetterBigInteger a, BetterBigInteger b) => a.CompareTo(b) <= 0;
    public static bool operator >=(BetterBigInteger a, BetterBigInteger b) => a.CompareTo(b) >= 0;

    public override string ToString() => ToString(10);
    public string ToString(int radix)
    {
        if (radix < 2 || radix > 36)
        {
            throw new ArgumentOutOfRangeException(nameof(radix));
        }

        if (IsZero())
        {
            return "0";
        }

        var cur = new BetterBigInteger(this.GetDigits().ToArray());
        var bigRadix = new BetterBigInteger(new uint[] { (uint)radix });
        var chars = new List<char>();
        while (!cur.IsZero())
        {
            uint digit = (cur % bigRadix).GetDigits()[0];
            chars.Add((digit < 10) ? (char)('0' + digit) : (char)('A' + digit - 10));
            cur /= bigRadix;
        }

        if (IsNegative)
        {
            chars.Add('-');
        }

        chars.Reverse();
        return new string(chars.ToArray());
    }

}