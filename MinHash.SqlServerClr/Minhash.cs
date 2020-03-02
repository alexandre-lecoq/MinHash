using Microsoft.SqlServer.Server;
using System;

namespace MinHash.SqlServerClr
{
    public class Minhash
    {
        [SqlFunction(IsDeterministic = true)]
        public static double JaccardIndex8(byte[] leftArray, byte[] rightArray)
        {
            if (leftArray == null || rightArray == null)
            {
                return 0.0;
            }

            if (leftArray.Length == 0 && rightArray.Length == 0)
            {
                return 1.0;
            }
            
            if (leftArray.Length != rightArray.Length)
            {
                throw new ArgumentException("Both array must have the same length.", nameof(leftArray));
            }

            var identicalCount = 0;

            for (var i = 0; i < leftArray.Length; i++)
            {
                if (leftArray[i] == rightArray[i])
                {
                    identicalCount++;
                }
            }

            var jaccard = (double)identicalCount / leftArray.Length;

            return jaccard;
        }

        [SqlFunction(IsDeterministic = true)]
        public static double JaccardIndex64(byte[] leftArray, byte[] rightArray)
        {
            if (leftArray == null || rightArray == null)
            {
                return 0.0;
            }

            if (leftArray.Length == 0 && rightArray.Length == 0)
            {
                return 1.0;
            }

            if (leftArray.Length != rightArray.Length)
            {
                throw new ArgumentException("Both array must have the same length.", nameof(leftArray));
            }

            var identicalCount = 0;

            for (var i = 0; i < leftArray.Length; i += 8)
            {
                if ((leftArray[i + 0] == rightArray[i + 0])
                    && (leftArray[i + 1] == rightArray[i + 1])
                    && (leftArray[i + 2] == rightArray[i + 2])
                    && (leftArray[i + 3] == rightArray[i + 3])
                    && (leftArray[i + 4] == rightArray[i + 4])
                    && (leftArray[i + 5] == rightArray[i + 5])
                    && (leftArray[i + 6] == rightArray[i + 6])
                    && (leftArray[i + 7] == rightArray[i + 7])
                    )
                {
                    identicalCount++;
                }
            }

            var jaccard = identicalCount * 8.0 / leftArray.Length;

            return jaccard;
        }
    }
}
