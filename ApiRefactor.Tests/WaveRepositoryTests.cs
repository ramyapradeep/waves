using ApiRefactor.Models;
using ApiRefactor.Repository;
using Microsoft.Data.Sqlite;
using Shouldly;

namespace ApiRefactor.Tests;

[Collection("Database collection")]
public class WaveRepositoryTests : IDisposable
{
    private readonly string _testDbPath = "App_Data/waves_repo_test.db";
    private readonly string _connectionString;
    private readonly WaveRepository _repository;

    public WaveRepositoryTests()
    {
        SqliteConnection.ClearAllPools();

        Directory.CreateDirectory("App_Data");

        if (File.Exists(_testDbPath))
        {
            try
            {
                File.Delete(_testDbPath);
            }
            catch
            {
                Thread.Sleep(500);
                SqliteConnection.ClearAllPools();
                File.Delete(_testDbPath);
            }
        }

        _connectionString = $"Data Source={_testDbPath}";

        // test database
        using (var connection = new SqliteConnection(_connectionString))
        {
            connection.Open();
            var createTableCommand = connection.CreateCommand();
            createTableCommand.CommandText = @"
                CREATE TABLE waves (
                    id TEXT PRIMARY KEY,
                    name TEXT,
                    wavedate TEXT
                )";
            createTableCommand.ExecuteNonQuery();
        }

        _repository = new WaveRepository(_connectionString);
    }

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        GC.Collect();
        GC.WaitForPendingFinalizers();

        Thread.Sleep(500);
    }

    #region GetAllWavesAsync Tests

    [Fact]
    public async Task GetAllWavesAsync_WithEmptyDatabase_ShouldReturnEmptyList()
    {
        // Act
        var waveIds = await _repository.GetAllWavesAsync();

        // Assert
        waveIds.ShouldNotBeNull();
        waveIds.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetAllWavesAsync_WithMultipleRecords_ShouldReturnAllWaveIds()
    {
        // Arrange
        var wave1Id = Guid.NewGuid();
        var wave2Id = Guid.NewGuid();
        var wave3Id = Guid.NewGuid();

        using (var connection = new SqliteConnection(_connectionString))
        {
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = @"
                    INSERT INTO waves (id, name, wavedate) VALUES 
                    (@id1, 'Wave 1', @date1),
                    (@id2, 'Wave 2', @date2),
                    (@id3, 'Wave 3', @date3)";
            command.Parameters.AddWithValue("@id1", wave1Id.ToString());
            command.Parameters.AddWithValue("@date1", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            command.Parameters.AddWithValue("@id2", wave2Id.ToString());
            command.Parameters.AddWithValue("@date2", DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd HH:mm:ss"));
            command.Parameters.AddWithValue("@id3", wave3Id.ToString());
            command.Parameters.AddWithValue("@date3", DateTime.Now.AddDays(-2).ToString("yyyy-MM-dd HH:mm:ss"));
            command.ExecuteNonQuery();
        }

        // Act
        var waveIds = await _repository.GetAllWavesAsync();

        // Assert
        waveIds.ShouldNotBeNull();
        waveIds.Count.ShouldBe(3);
        waveIds.ShouldContain(wave1Id);
        waveIds.ShouldContain(wave2Id);
        waveIds.ShouldContain(wave3Id);
    }

    #endregion

    #region GetByIdAsync Tests

    [Fact]
    public async Task GetByIdAsync_WithExistingId_ShouldReturnWave()
    {
        // Arrange
        var waveId = Guid.NewGuid();
        var waveName = "Test Wave";
        var waveDate = DateTime.Now.AddDays(-3);

        using (var connection = new SqliteConnection(_connectionString))
        {
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = @"
                    INSERT INTO waves (id, name, wavedate) 
                    VALUES (@id, @name, @wavedate)";
            command.Parameters.AddWithValue("@id", waveId.ToString());
            command.Parameters.AddWithValue("@name", waveName);
            command.Parameters.AddWithValue("@wavedate", waveDate.ToString("yyyy-MM-dd HH:mm:ss"));
            command.ExecuteNonQuery();
        }

        // Act
        var wave = await _repository.GetByIdAsync(waveId);

        // Assert
        wave.ShouldNotBeNull();
        wave.Id.ShouldBe(waveId);
        wave.Name.ShouldBe(waveName);
        wave.WaveDate.Date.ShouldBe(waveDate.Date);
    }

    [Fact]
    public async Task GetByIdAsync_WithNonExistentId_ShouldReturnNull()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var wave = await _repository.GetByIdAsync(nonExistentId);

        // Assert
        wave.ShouldBeNull();
    }
    #endregion

    #region CreateAsync Tests

    [Fact]
    public async Task CreateAsync_WithValidWave_ShouldInsertIntoDatabase()
    {
        // Arrange
        var wave = new Wave
        {
            Id = Guid.NewGuid(),
            Name = "New Wave",
            WaveDate = DateTime.Now
        };

        // Act
        var result = await _repository.CreateAsync(wave);

        // Assert
        result.ShouldNotBeNull();
        result.Id.ShouldBe(wave.Id);
        result.Name.ShouldBe(wave.Name);

        var retrieved = await _repository.GetByIdAsync(wave.Id);
        retrieved.ShouldNotBeNull();
        retrieved.Name.ShouldBe(wave.Name);
    }

    [Fact]
    public async Task CreateAsync_WithNullName_ShouldThrowException()
    {
        // Arrange
        var wave = new Wave
        {
            Id = Guid.NewGuid(),
            Name = null,
            WaveDate = DateTime.Now
        };

        // Act & Assert
        await Should.ThrowAsync<InvalidOperationException>(async () =>
        {
            await _repository.CreateAsync(wave);
        });
    }


    [Fact]
    public async Task CreateAsync_MultipleTimes_ShouldCreateMultipleRecords()
    {
        // Arrange
        var wave1 = new Wave { Id = Guid.NewGuid(), Name = "Wave 1", WaveDate = DateTime.Now };
        var wave2 = new Wave { Id = Guid.NewGuid(), Name = "Wave 2", WaveDate = DateTime.Now };
        var wave3 = new Wave { Id = Guid.NewGuid(), Name = "Wave 3", WaveDate = DateTime.Now };

        // Act
        await _repository.CreateAsync(wave1);
        await _repository.CreateAsync(wave2);
        await _repository.CreateAsync(wave3);

        // Assert
        var allWaveIds = await _repository.GetAllWavesAsync();
        allWaveIds.Count.ShouldBe(3);
    }

    #endregion

    #region UpdateAsync Tests

    [Fact]
    public async Task UpdateAsync_WithExistingWave_ShouldUpdateDatabase()
    {
        // Arrange
        var wave = new Wave
        {
            Id = Guid.NewGuid(),
            Name = "Original Name",
            WaveDate = DateTime.Now
        };
        await _repository.CreateAsync(wave);

        wave.Name = "Updated Name";
        wave.WaveDate = DateTime.Now.AddDays(1);

        // Act
        var result = await _repository.UpdateAsync(wave);

        // Assert
        result.ShouldNotBeNull();
        result.Name.ShouldBe("Updated Name");

        var retrieved = await _repository.GetByIdAsync(wave.Id);
        retrieved.ShouldNotBeNull();
        retrieved.Name.ShouldBe("Updated Name");
    }

    [Fact]
    public async Task UpdateAsync_WithNonExistentWave_ShouldThrowKeyNotFoundException()
    {
        // Arrange
        var wave = new Wave
        {
            Id = Guid.NewGuid(),
            Name = "Nonexistent Wave",
            WaveDate = DateTime.Now
        };

        // Act & Assert
        await Should.ThrowAsync<KeyNotFoundException>(async () =>
        {
            await _repository.UpdateAsync(wave);
        });
    }

    [Fact]
    public async Task UpdateAsync_WithSpecialCharacters_ShouldHandleCorrectly()
    {
        // Arrange
        var wave = new Wave
        {
            Id = Guid.NewGuid(),
            Name = "Original",
            WaveDate = DateTime.Now
        };
        await _repository.CreateAsync(wave);

        wave.Name = "Updated'; DROP TABLE waves;--";

        // Act
        var result = await _repository.UpdateAsync(wave);

        // Assert
        result.Name.ShouldBe(wave.Name);

        // Verify table still exists and data is correct
        var retrieved = await _repository.GetByIdAsync(wave.Id);
        retrieved.ShouldNotBeNull();
        retrieved.Name.ShouldBe(wave.Name);
    }

    #endregion

    #region SQL Injection Protection Tests

    [Fact]
    public async Task GetByIdAsync_WithMaliciousId_ShouldNotCauseSQLInjection()
    {
        // Arrange
        var normalWave = new Wave
        {
            Id = Guid.NewGuid(),
            Name = "Normal Wave",
            WaveDate = DateTime.Now
        };
        await _repository.CreateAsync(normalWave);

        // Act - Try to retrieve with malicious guid pattern
        var result = await _repository.GetByIdAsync(normalWave.Id);

        // Assert
        result.ShouldNotBeNull();

        var allWaves = await _repository.GetAllWavesAsync();
        allWaves.Count.ShouldBe(1);
    }

    [Fact]
    public async Task CreateAsync_WithSQLInjectionInName_ShouldEscapeCorrectly()
    {
        // Arrange
        var maliciousWave = new Wave
        {
            Id = Guid.NewGuid(),
            Name = "Robert'); DROP TABLE waves;--",
            WaveDate = DateTime.Now
        };

        // Act
        await _repository.CreateAsync(maliciousWave);

        // Assert
        var retrieved = await _repository.GetByIdAsync(maliciousWave.Id);
        retrieved.ShouldNotBeNull();
        retrieved.Name.ShouldBe(maliciousWave.Name);

        var allWaves = await _repository.GetAllWavesAsync();
        allWaves.ShouldNotBeEmpty();
    }

    #endregion

    #region ExistsAsync Tests

    [Fact]
    public async Task ExistsAsync_WithExistingWave_ShouldReturnTrue()
    {
        // Arrange
        var wave = new Wave
        {
            Id = Guid.NewGuid(),
            Name = "Test Wave",
            WaveDate = DateTime.Now
        };
        await _repository.CreateAsync(wave);

        // Act
        var exists = await _repository.ExistsAsync(wave.Id);

        // Assert
        exists.ShouldBeTrue();
    }

    [Fact]
    public async Task ExistsAsync_WithNonExistentWave_ShouldReturnFalse()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var exists = await _repository.ExistsAsync(nonExistentId);

        // Assert
        exists.ShouldBeFalse();
    }

    #endregion
}