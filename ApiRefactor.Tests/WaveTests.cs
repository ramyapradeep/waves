using ApiRefactor.Models;
using Microsoft.Data.Sqlite;
using Shouldly;

namespace ApiRefactor.Tests;

// Use collection to ensure tests don't run in parallel (avoids file locking)
[Collection("Database collection")]
public class WavesTests : IDisposable
{
    private readonly string _testDbPath = "App_Data/waves_test.db";

    public WavesTests()
    {
        // Ensure App_Data directory exists
        Directory.CreateDirectory("App_Data");

        // Delete any existing test database
        if (File.Exists(_testDbPath))
        {
            try
            {
                File.Delete(_testDbPath);
            }
            catch
            {
                // If file is locked, wait a bit and try again
                Thread.Sleep(100);
                File.Delete(_testDbPath);
            }
        }

        // Create a fresh test database
        using (var connection = new SqliteConnection($"Data Source={_testDbPath}"))
        {
            connection.Open();
            var createTableCommand = connection.CreateCommand();
            createTableCommand.CommandText = @"
                CREATE TABLE IF NOT EXISTS waves (
                    id TEXT PRIMARY KEY,
                    name TEXT,
                    wavedate TEXT
                )";
            createTableCommand.ExecuteNonQuery();
        }

        // Temporarily change the static connection string to use test database
        var sqlConnectionType = typeof(SqlConnection);
        var connectionStringField = sqlConnectionType.GetField("ConnectionString",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        connectionStringField?.SetValue(null, $"Data Source={_testDbPath}");
    }

    public void Dispose()
    {
        // Wait a bit to ensure all connections are closed
        Thread.Sleep(100);

        // Restore original connection string
        var sqlConnectionType = typeof(SqlConnection);
        var connectionStringField = sqlConnectionType.GetField("ConnectionString",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        connectionStringField?.SetValue(null, "Data Source=App_Data/waves.db");

        // Clean up test database
        try
        {
            if (File.Exists(_testDbPath))
            {
                File.Delete(_testDbPath);
            }
        }
        catch
        {
            // If cleanup fails, it's not critical for tests
        }
    }

    [Fact]
    public void Waves_Constructor_WithEmptyDatabase_ShouldReturnEmptyList()
    {
        // Act
        var waves = new Waves();

        // Assert
        waves.Items.ShouldNotBeNull();
        waves.Items.ShouldBeEmpty();
    }

    [Fact]
    public void Waves_Constructor_WithMultipleRecords_ShouldReturnAllWaves()
    {
        // Arrange
        var wave1Id = Guid.NewGuid();
        var wave2Id = Guid.NewGuid();
        var wave3Id = Guid.NewGuid();

        using (var connection = new SqliteConnection($"Data Source={_testDbPath}"))
        {
            connection.Open();
            var insertCommand = connection.CreateCommand();
            insertCommand.CommandText = $@"
                INSERT INTO waves (id, name, wavedate) VALUES 
                ('{wave1Id}', 'Wave 1', '{DateTime.Now:yyyy-MM-dd HH:mm:ss}'),
                ('{wave2Id}', 'Wave 2', '{DateTime.Now.AddDays(-1):yyyy-MM-dd HH:mm:ss}'),
                ('{wave3Id}', 'Wave 3', '{DateTime.Now.AddDays(-2):yyyy-MM-dd HH:mm:ss}')";
            insertCommand.ExecuteNonQuery();
        }

        // Act
        var waves = new Waves();

        // Assert
        waves.Items.ShouldNotBeNull();
        waves.Items.Count.ShouldBe(3);
        waves.Items.ShouldContain(w => w.Id == wave1Id);
        waves.Items.ShouldContain(w => w.Id == wave2Id);
        waves.Items.ShouldContain(w => w.Id == wave3Id);
    }

    [Fact]
    public void Waves_Constructor_ShouldLoadWaveDataCorrectly()
    {
        // Arrange
        var waveId = Guid.NewGuid();
        var waveName = "Test Wave";
        var waveDate = DateTime.Now.AddDays(-3);

        using (var connection = new SqliteConnection($"Data Source={_testDbPath}"))
        {
            connection.Open();
            var insertCommand = connection.CreateCommand();
            insertCommand.CommandText = $@"
                INSERT INTO waves (id, name, wavedate) 
                VALUES ('{waveId}', '{waveName}', '{waveDate:yyyy-MM-dd HH:mm:ss}')";
            insertCommand.ExecuteNonQuery();
        }

        // Act
        var waves = new Waves();

        // Assert
        waves.Items.ShouldHaveSingleItem();
        var wave = waves.Items[0];
        wave.Id.ShouldBe(waveId);
        wave.Name.ShouldBe(waveName);
    }

    [Fact]
    public void Waves_Items_ShouldBeInternalSet()
    {
        // Arrange
        var waves = new Waves();

        // Act & Assert
        var propertyInfo = typeof(Waves).GetProperty("Items");
        var setMethod = propertyInfo?.GetSetMethod(true);

        setMethod.ShouldNotBeNull();
        (setMethod.IsAssembly || setMethod.IsFamilyOrAssembly).ShouldBeTrue();
    }
}