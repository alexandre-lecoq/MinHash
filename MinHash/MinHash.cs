using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace MinHash
{
    public class MinHash
    {
        /// <summary>
        /// Minimum hash values.
        /// </summary>
        public ulong[] MinHashes { get; }

        public static byte[] ToByteArray(ulong[] input)
        {
            var output = new byte[input.Length * sizeof(ulong)];
            Buffer.BlockCopy(input, 0, output, 0, output.Length);

            return output;
        }

        public static ulong[] ToUInt64Array(byte[] input)
        {
            var outputLength = input.Length / sizeof(ulong);
            var remainder = input.Length % sizeof(ulong);

            if (remainder != 0)
                throw new ArgumentException("Array length is not a multiple of 8 bytes.", nameof(input));

            var output = new ulong[outputLength];
            Buffer.BlockCopy(input, 0, output, 0, input.Length);

            return output;
        }

        /// <summary>
        /// Length of the hash vector.
        /// It's better to make it something with lots of divisors to make it friendly to LSH.
        /// Example length are given in https://en.wikipedia.org/wiki/Table_of_divisors
        /// </summary>
        private const int HashLength = 504;

        public double ExpectedAverageError => 1.0 / Math.Sqrt(HashLength);
        
        public MinHash()
        {
            MinHashes = new ulong[HashLength];

            for (var i = 0; i < MinHashes.Length; i++)
            {
                MinHashes[i] = ulong.MaxValue;
            }
        }

        public MinHash(ulong[] minHashes)
        {
            if (minHashes.Length != HashLength)
            {
                throw new ArgumentException($"Array length should be {HashLength}.", nameof(minHashes));
            }

            MinHashes = new ulong[HashLength];

            Array.Copy(minHashes, MinHashes, HashLength);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public void Add(IEnumerable<ulong> inputHashSet)
        {
            if (inputHashSet == null)
            {
                throw new ArgumentNullException(nameof(inputHashSet), "Cannot be null.");
            }

            foreach (var baseHash in inputHashSet)
            {
                for (var i = 0; i < MinHashes.Length; i++)
                {
                    var hash = UniversalHashing.Hash(baseHash, i);
                    MinHashes[i] = hash <= MinHashes[i] ? hash : MinHashes[i];
                }
            }
        }

        public MinHash Merge(MinHash other)
        {
            var minHashes = new ulong[HashLength];

            for (var i = 0; i < minHashes.Length; i++)
            {
                minHashes[i] = MinHashes[i] < other.MinHashes[i] ? MinHashes[i] : other.MinHashes[i];
            }

            var hash = new MinHash(minHashes);

            return hash;
        }

        public double GetJaccardIndex(MinHash other)
        {
            if (MinHashes.Length != other.MinHashes.Length)
            {
                throw new ArgumentException($"Hash length should be {MinHashes.Length}.", nameof(other));
            }

            var count = MinHashes.Where((t, i) => t == other.MinHashes[i]).Count();
            var estimate = (double) count / MinHashes.Length;
            
            return estimate;
        }
    }
}
