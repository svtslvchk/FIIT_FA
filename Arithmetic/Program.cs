using Arithmetic.BigInt;

Console.WriteLine("=== Проверка NOT ===\n");

var tests = new[]
{
    new { Value = "0", Expected = "-1" },
    new { Value = "1", Expected = "-2" },
    new { Value = "795468561902", Expected = "-795468561903" },
    new { Value = "-1", Expected = "0" },
    new { Value = "-795468561903", Expected = "795468561902" },
};

foreach (var test in tests)
{
    var a = new BetterBigInteger(test.Value, 10);
    var result = ~a;
    Console.WriteLine($"~{test.Value} = {result}");
    Console.WriteLine($"Expected: {test.Expected}");
    Console.WriteLine($"Result: {(result.ToString() == test.Expected ? "✅" : "❌")}\n");
}

/*
/// [x] Test_Addition_Random()
/// [x] Test_Comparison_Logic()
/// [] Test_Division_Random() - зависает 
/// [x] Test_DivideByZero_Throws()
/// [x] Test_UnaryMinus_And_Modulo() - что-то наколдовал 
/// [x] Test_Constructors_And_SSO_Threshold()
/// [x] Test_Radix_Conversion()
/// [x] Test_Bitwise_Logic() - работает нечестно ❌
/// [x] Test_Shifts() - работает нечестно ❌ (а как будто и получилось)
/// [x] Test_EdgeCases()
/// [x] Test_Multiplication_Karatsuba()
/// [] Test_Multiplication_FFT() - не реализовано
/// [x] Test_Multiplication_Simple()
/// [] GenerateLargeRandomString(Random rnd, int length) - с предупреждением 
*/