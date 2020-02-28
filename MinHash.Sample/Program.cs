using System;
using Microsoft.Extensions.Configuration;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace MinHash.Sample
{
    internal static class Program
    {
        private static int Main(string[] args)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);

            var configuration = builder.Build();
            var connectionString = configuration.GetConnectionString("Storage");
            using var connection = new SqlConnection(connectionString);
            var databaseConnection = new DatabaseConnection(connection);

//#if DEBUG
            args = new[] { "4_COMPUTE_PAIRS_OLD" };
//#endif

            if (args.Length == 1)
            {
                Func<DatabaseConnection, int> methodMain = args[0] switch
                {
                    "1_CREATE_TABLE" => CreateTableMain,
                    "2_FILL_TABLE" => NormalizeDataMain,
                    "3_COMPUTE_HASH" => ComputeHashMain,
                    "4_COMPUTE_PAIRS" => ComputePairsMain,
                    "4_COMPUTE_PAIRS_OLD" => ComputePairsOldMain,
                    _ => throw new ArgumentException("Invalid argument passed.")
                };

                var sw = new Stopwatch();
                sw.Reset();
                sw.Start();
                var ret = methodMain(databaseConnection);
                sw.Stop();
                Console.WriteLine($"Elapsed time : {sw.ElapsedMilliseconds} milliseconds.");

                return ret;
            }

            Console.Error.WriteLine("Invalid number of argument. (should be one and only one)");
            return -1;

        }

        private static int CreateTableMain(DatabaseConnection connection)
        {
            connection.CreateTableMain();

            return 0;
        }

        private static int NormalizeDataMain(DatabaseConnection connection)
        {
            connection.NormalizeDate();

            return 0;
        }

        private static int ComputeHashMain(DatabaseConnection connection)
        {
            var persons = connection.ReadPersons();

            foreach (var personItems in persons)
            {
                var items = personItems.AsEnumerable().Skip(1).Where(i => !(i is System.DBNull));
                var stringItems = items.Select(i => i.ToString());
                var hashes = stringItems.Select(Farmhash.Sharp.Farmhash.Hash64);
                var minHash = new MinHash();
                minHash.Add(hashes);
                connection.SetMinHash((long)personItems[0], MinHash.ToByteArray(minHash.MinHashes));
            }

            return 0;
        }

        private static int ComputePairsMain(DatabaseConnection connection)
        {
            connection.DeleteAllPairs();
            connection.SetOrdernumber();
            connection.ComputeMinHashPairs();

            return 0;
        }
        
        private static int ComputePairsOldMain(DatabaseConnection connection)
        {
            connection.DeleteAllPairs();
            connection.ComputeClassicPairs();

            return 0;
        }
    }
}
