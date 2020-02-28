using Dapper;
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
	Email NVARCHAR(500) NULL,
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
    Similarity REAL NOT NULL,
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
            return _connection.Query<DatabaseMinhash>("SELECT Id, MinHash FROM Person ORDER BY OrderNumber ASC").ToList();
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

        internal void ComputeMinHashPairs()
        {
            const string query = @"
INSERT INTO Pairs
SELECT x.leftId, x.rightId, x.Jaccard
FROM (
	SELECT leftPerson.Id AS leftId, rightPerson.Id AS rightId, dbo.JACCARD_INDEX_64(leftPerson.MinHash, rightPerson.MinHash) AS Jaccard
	FROM Person leftPerson
	INNER JOIN Person rightPerson
		ON leftPerson.OrderNumber + 1 = rightPerson.OrderNumber
) x
WHERE x.Jaccard > 0.75
";

            _connection.Open();
            var updateCommand = new SqlCommand(query, _connection);
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

        public void ComputeClassicPairs()
        {
            const string query = @"
INSERT INTO Pairs
SELECT
	leftPerson.Id,
	rightPerson.Id,
    1.0
FROM Person leftPerson
INNER JOIN Person rightPerson
	ON leftPerson.Id < rightPerson.Id
		AND (leftPerson.Gender = rightPerson.Gender OR (leftPerson.Gender IS NULL AND rightPerson.Gender IS NULL))
		AND (leftPerson.FirstName = rightPerson.FirstName OR (leftPerson.FirstName IS NULL AND rightPerson.FirstName IS NULL))
		AND (leftPerson.LastName = rightPerson.LastName OR (leftPerson.LastName IS NULL AND rightPerson.LastName IS NULL))
		AND (leftPerson.BirthDate = rightPerson.BirthDate OR (leftPerson.BirthDate IS NULL AND rightPerson.BirthDate IS NULL))
		AND (leftPerson.Phone = rightPerson.Phone OR (leftPerson.Phone IS NULL AND rightPerson.Phone IS NULL))
		AND (leftPerson.Email = rightPerson.Email OR (leftPerson.Email IS NULL AND rightPerson.Email IS NULL))
		AND (leftPerson.Company = rightPerson.Company OR (leftPerson.Company IS NULL AND rightPerson.Company IS NULL))
		AND (leftPerson.Street = rightPerson.Street OR (leftPerson.Street IS NULL AND rightPerson.Street IS NULL))
		AND (leftPerson.City = rightPerson.City OR (leftPerson.City IS NULL AND rightPerson.City IS NULL))
		AND (leftPerson.Country = rightPerson.Country OR (leftPerson.Country IS NULL AND rightPerson.Country IS NULL))
";

            _connection.Open();
            var updateCommand = new SqlCommand(query, _connection);
            updateCommand.ExecuteNonQuery();
            _connection.Close();

        }
    }
}
