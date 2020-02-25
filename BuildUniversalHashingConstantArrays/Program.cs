using System;
using System.Security.Cryptography;
using System.Text;

namespace BuildUniversalHashingConstantArrays
{
    public class Program
    {
        private static readonly RandomNumberGenerator RandomNumberGenerator = RandomNumberGenerator.Create();

        public static void Main(string[] args)
        {
            const int arrayLength = 10000;

            var a = GetRandomArray(arrayLength, true);
            var b = GetRandomArray(arrayLength, false);

            var text = GetArraysCode(a, b);

            Console.WriteLine(text);
        }

        private static ulong[] GetRandomArray(int arrayLength, bool makeValuesOdd)
        {
            var result = new ulong[arrayLength];

            for (var i = 0; i < arrayLength; i++)
            {
                var b = new byte[8];
                RandomNumberGenerator.GetBytes(b);
                var v = BitConverter.ToUInt64(b, 0);

                if (makeValuesOdd)
                {
                    if ((v & 0x1) == 0)
                    {
                        v += 1;
                    }
                }

                result[i] = v;
            }

            return result;
        }
        
        private static string GetArraysCode(ulong[] a, ulong[] b)
        {
            var sb = new StringBuilder(1000000);

            sb.AppendLine("        // Let A be a random odd positive integer with A < 2^w");
            sb.AppendLine(GenerateArrayCode(a, "A"));
            sb.AppendLine("        // Let B be a random non-negative integer with B < 2^(w-M)");
            sb.AppendLine(GenerateArrayCode(b, "B"));

            return sb.ToString();
        }

        private static string GenerateArrayCode(ulong[] values, string variableName)
        {
            var result = new StringBuilder(1000000);

            result.AppendLine($"        private static readonly ulong[] {variableName} =");

            result.AppendLine("        {");

            foreach (var v in values)
            {
                result.Append($"            0x{v:X}, ");
            }
            
            result.AppendLine();
            result.AppendLine("        };");

            return result.ToString();
        }
    }
}
