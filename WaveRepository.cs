using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;

namespace ApiRefactor.Repositories;

public interface IWaveRepository
{
    Task<List<Guid>> GetAllWaveIdsAsync();
    Task<Wave?> GetByIdAsync(Guid id);
    Task<Wave> CreateAsync(Wave wave);
    Task<Wave> UpdateAsync(Wave wave);
    Task<bool> ExistsAsync(Guid id);
}

public class WaveRepository: IWaveRepository
{
    private readonly string _connectionString;

    public WaveRepository(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task<List<Guid>> GetAllWaveIdsAsync()
    {
        var waveIds = new List<Guid>();

        using (var connection = new SqliteConnection(_connectionString))
        {
            await connection.OpenAsync();

            var command = connection.CreateCommand();
            command.CommandText = "select id from waves";

            using (var reader = await command.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    var id = Guid.Parse(reader["id"].ToString());
                    waveIds.Add(id);
                }
            }
        }

        return waveIds;
    }

    public async Task<Wave?> GetByIdAsync(Guid id)
    {
        using (var connection = new SqliteConnection(_connectionString))
        {
            await connection.OpenAsync();

            var command = connection.CreateCommand();
            command.CommandText = "select * from waves where id = @id";
            command.Parameters.AddWithValue("@id", id.ToString());

            using (var reader = await command.ExecuteReaderAsync())
            {
                if (await reader.ReadAsync())
                {
                    return new Wave
                    {
                        Id = Guid.Parse(reader["id"].ToString()),
                        Name = reader["name"].ToString(),
                        WaveDate = DateTime.Parse(reader["wavedate"].ToString())
                    };
                }
            }
        }

        return null; // Not found
    }

    public async Task<Wave> CreateAsync(Wave wave)
    {
        using (var connection = new SqliteConnection(_connectionString))
        {
            await connection.OpenAsync();

            var command = connection.CreateCommand();
            command.CommandText = "insert into waves (id, name, wavedate) values (@id, @name, @wavedate)";
            command.Parameters.AddWithValue("@id", wave.Id.ToString());
            command.Parameters.AddWithValue("@name", wave.Name);
            command.Parameters.AddWithValue("@wavedate", wave.WaveDate.ToString("yyyy-MM-dd HH:mm:ss"));

            await command.ExecuteNonQueryAsync();
        }

        return wave;
    }

    public async Task<Wave> UpdateAsync(Wave wave)
    {
        using (var connection = new SqliteConnection(_connectionString))
        {
            await connection.OpenAsync();

            var command = connection.CreateCommand();
            command.CommandText = "update waves set name = @name, wavedate = @wavedate where id = @id";
            command.Parameters.AddWithValue("@name", wave.Name);
            command.Parameters.AddWithValue("@wavedate", wave.WaveDate.ToString("yyyy-MM-dd HH:mm:ss"));
            command.Parameters.AddWithValue("@id", wave.Id.ToString());

            var rowsAffected = await command.ExecuteNonQueryAsync();

            if (rowsAffected == 0)
            {
                throw new KeyNotFoundException($"Wave with ID {wave.Id} not found");
            }
        }

        return wave;
    }

    public async Task<bool> ExistsAsync(Guid id)
    {
        using (var connection = new SqliteConnection(_connectionString))
        {
            await connection.OpenAsync();

            var command = connection.CreateCommand();
            command.CommandText = "select count(*) from waves where id = @id";
            command.Parameters.AddWithValue("@id", id.ToString());

            var count = Convert.ToInt32(await command.ExecuteScalarAsync());
            return count > 0;
        }
    }
}
