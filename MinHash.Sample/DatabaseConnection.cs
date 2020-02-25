using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;

namespace MinHash.Sample
{
    public class DatabaseConnection
    {
        private readonly SqlConnection _connection;

        public DatabaseConnection(SqlConnection connection)
        {
            _connection = connection;
            _connection.Open();
            _connection.Close();
        }
        
        private const string CreateTableStatement = @"
IF OBJECT_ID('dbo.Pairs') IS NOT NULL
BEGIN
	DROP TABLE dbo.Pairs
END

IF OBJECT_ID('dbo.Person') IS NOT NULL
BEGIN
	DROP TABLE dbo.Person
END

CREATE TABLE Person
(
	Id BIGINT PRIMARY KEY,
	Gender CHAR(1) NULL,
	FirstName NVARCHAR(200) NOT NULL,
	LastName NVARCHAR(200) NOT NULL,
	BirthDate DATE NULL,
	Phone VARCHAR(20) NULL,
	Email NVARCHAR(500) NOT NULL,
	Company NVARCHAR(100) NULL,
	Street NVARCHAR(600) NULL,
	City NVARCHAR(200) NULL,
	Country NVARCHAR(200) NULL,
    MinHash VARBINARY(4032) NULL,
    OrderNumber BIGINT NULL
);

CREATE NONCLUSTERED INDEX [IX_Person_OrderNumber] ON Person (OrderNumber ASC);

CREATE TABLE Pairs
(
	LeftId BIGINT NOT NULL REFERENCES Person(Id),
	RightId BIGINT NOT NULL REFERENCES Person(Id),
	CONSTRAINT PK_Pairs PRIMARY KEY (LeftId, RightId)
);
";

        public int CreateTableMain()
        {
            _connection.Open();
            var command = new SqlCommand(CreateTableStatement, _connection);
            command.ExecuteNonQuery();
            _connection.Close();

            return 0;
        }


        [Serializable]
        public class DatabaseMinhash
        {
            public long Id { get; set; }

            public byte[] Minhash { get; set; }
        }

        public IList<DatabaseMinhash> ReadDatabaseMinhashes()
        {
            const string query = @"SELECT Id, MinHash FROM Person ORDER BY OrderNumber ASC";

            _connection.Open();
            var command = new SqlCommand(query, _connection);

            var list = new List<DatabaseMinhash>(100);

            using (var reader = command.ExecuteReader(CommandBehavior.SequentialAccess))
            {
                var idOrdinal = reader.GetOrdinal("Id");
                var minHashOrdinal = reader.GetOrdinal("MinHash");

                while (reader.Read())
                {
                    var id = reader.GetFieldValue<long>(idOrdinal);
                    var minhash = reader.GetFieldValue<byte[]>(minHashOrdinal);

                    var databaseMinhash = new DatabaseMinhash()
                    {
                        Id = id,
                        Minhash = minhash
                    };

                    list.Add(databaseMinhash);
                }
            }

            _connection.Close();

            return list;
        }

        public ICollection<object[]> ReadPersons()
        {
            _connection.Open();
            var command = new SqlCommand("SELECT * FROM Person WHERE MinHash IS NULL", _connection);

            var rowList = new List<object[]>();

            using (var reader = command.ExecuteReader(CommandBehavior.SequentialAccess))
            {
                var fieldCount = reader.FieldCount;

                while (reader.Read())
                {
                    var itemArray = new object[fieldCount];
                    reader.GetValues(itemArray);
                    rowList.Add(itemArray);
                }
            }

            _connection.Close();

            return rowList;
        }

        public void InsertPair(long leftId, long rightId)
        {
            _connection.Open();
            var updateCommand = new SqlCommand("INSERT INTO Pairs (LeftId, RightId) VALUES (@leftId, @rightId)", _connection);
            updateCommand.Parameters.AddWithValue("leftId", leftId);
            updateCommand.Parameters.AddWithValue("rightId", rightId);
            updateCommand.ExecuteNonQuery();
            _connection.Close();
        }

        public void SetMinHash(long id, byte[] hash)
        {
            _connection.Open();
            var updateCommand = new SqlCommand("UPDATE Person SET MinHash = @hash, OrderNumber = NULL WHERE Id = @id", _connection);
            updateCommand.Parameters.AddWithValue("hash", hash);
            updateCommand.Parameters.AddWithValue("id", id);
            updateCommand.ExecuteNonQuery();
            _connection.Close();
        }

        public void NormalizeDate()
        {
            const string query = @"
UPDATE Person
SET
	FirstName = REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(RTRIM(LTRIM(FirstName)), '   ', '-'), '  ', '-'), ' ', '-'), '.', ''), '/', ''), '\', ''),
	LastName = REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(RTRIM(LTRIM(LastName)), '   ', '-'), '  ', '-'), ' ', '-'), '.', ''), '/', ''), '\', ''),
	Phone = REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(RTRIM(LTRIM(Phone)), '  ', ''), ' ', ''), '+', ''), '-', ''), '.', ''),
	Email = RTRIM(LTRIM(Email)),
	Company = REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(RTRIM(LTRIM(Company)), '   ', '-'), '  ', '-'), ' ', '-'), '.', ''), '/', ''), '\', ''),
	Street = REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(RTRIM(LTRIM(Street)), '   ', '-'), '  ', '-'), ' ', '-'), '.', ''), '/', ''), '\', ''),
	City = REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(RTRIM(LTRIM(City)), '   ', '-'), '  ', '-'), ' ', '-'), '.', ''), '/', ''), '\', ''),
	Country = REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(RTRIM(LTRIM(Country)), '   ', '-'), '  ', '-'), ' ', '-'), '.', ''), '/', ''), '\', '')
";

            _connection.Open();
            var updateCommand = new SqlCommand(query, _connection);
            updateCommand.ExecuteNonQuery();
            _connection.Close();
        }

        public void SetOrdernumber()
        {
            const string query = @"
UPDATE p
SET p.OrderNumber = x.Number
FROM Person p
INNER JOIN (
	SELECT Id, ROW_NUMBER() OVER(ORDER BY MinHash) AS Number
	FROM Person
) x
	ON x.Id = p.Id
";

            _connection.Open();
            var updateCommand = new SqlCommand(query, _connection);
            updateCommand.ExecuteNonQuery();
            _connection.Close();
        }

        public void DeleteAllPairs()
        {
            _connection.Open();
            var updateCommand = new SqlCommand("TRUNCATE TABLE Pairs;", _connection);
            updateCommand.ExecuteNonQuery();
            _connection.Close();
        }

        public void computePairs()
        {
            const string query = @"
INSERT INTO Pairs
SELECT
	leftPerson.Id,
	rightPerson.Id
FROM Person leftPerson
INNER JOIN Person rightPerson
	ON leftPerson.Id < rightPerson.Id
		AND leftPerson.Gender = rightPerson.Gender
		AND leftPerson.FirstName = rightPerson.FirstName
		AND leftPerson.LastName = rightPerson.LastName
		AND leftPerson.BirthDate = rightPerson.BirthDate
		AND leftPerson.Phone = rightPerson.Phone
		AND leftPerson.Email = rightPerson.Email
		AND leftPerson.Company = rightPerson.Company
		AND leftPerson.Street = rightPerson.Street
		AND leftPerson.City = rightPerson.City
		AND leftPerson.Country = rightPerson.Country
";

            _connection.Open();
            var updateCommand = new SqlCommand(query, _connection);
            updateCommand.ExecuteNonQuery();
            _connection.Close();

        }
    }
}
