using System;
using System.Linq;
using Xunit;

namespace MinHash.Tests
{
    public class Tests
    {
        [Fact]
        public void Test1()
        {
            var dataSet1 = new[]
            {
                "To", "compute", "the", "resemblance", "and/or", "the", "containment", "of", "two", "documents", "it",
                "suffices", "to", "keep", "for", "each", "document", "a", "relatively", "small", "sketch.", "The",
                "sketches", "can", "be", "computed", "fairly", "fast", "(linear", "in", "the", "size", "of", "the",
                "documents)", "and", "given", "two", "sketches", "the", "resemblance", "or", "the", "containment", "of",
                "the", "corresponding", "documents", "can", "be", "computed", "in", "linear", "time", "in", "the",
                "size", "of", "the", "sketches.", "For", "computing", "resemblance,", "it", "suffices", "to", "keep",
                "a", "fixed", "size", "sketch.", "For", "computing", "containment,", "we", "need", "a", "sketch",
                "proportional", "to", "the", "size", "of", "the", "underlying", "document;", "however", "as", "it",
                "will", "be", "explained", "this", "problem", "can", "be", "finessed", "at", "the", "cost", "of", "a",
                "loss", "of", "precision."
            };

            var dataSet2 = new[]
            {
                "In", "order", "to", "compute", "the", "resemblance", "or/and", "the", "containment", "of", "two",
                "documents", "it", "suffice", "to", "keep", "a", "relatively", "small",
                "sketch.", "The", "sketches", "can", "be", "computed", "fairly", "quick", "and", "given", "two",
                "sketches", "the", "resemblance", "or", "the", "containment", "of", "the", "corresponding", "document",
                "can", "be", "in", "linear", "time", "in", "the", "size", "of", "the", "sketches.", "For",
                "computing", "resemblance,", "it", "suffices", "to", "keep", "a", "fixed", "size", "sketch.", "For",
                "computing", "containment,", "we", "need", "a", "sketch", "proportional", "to", "the", "size", "of",
                "the", "underlying", "document", "however", "as", "it", "will", "be", "explained", "this", "problem",
                "can", "be", "finessed", "at", "the", "cost", "of", "the", "loss", "precision"
            };

            var minHash1 = new MinHash();
            var dataSet1HashSet = dataSet1.Select(Farmhash.Sharp.Farmhash.Hash64);
            minHash1.Add(dataSet1HashSet);

            var minHash2 = new MinHash();
            var dataSet2HashSet = dataSet2.Select(Farmhash.Sharp.Farmhash.Hash64);
            minHash2.Add(dataSet2HashSet);

            var estimate = minHash1.GetJaccardIndex(minHash2);

            var exact = JaccardIndex.GetJaccardIndex(dataSet1, dataSet2);
            var distance = Math.Abs(exact - estimate);

            Assert.True(distance < 0.01);
        }
    }
}
