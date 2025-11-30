using ApiRefactor.Models;
using ApiRefactor.Repository;

namespace ApiRefactor.Service;

public class WaveService : IWaveService
{
    private readonly IWaveRepository _repository;
    private readonly ILogger<WaveService> _logger;
    
    public WaveService(IWaveRepository repository, ILogger<WaveService> logger) => 
        (_repository, _logger) = (repository, logger);

    public async Task<List<Wave>> GetAllWavesAsync()
    {
        var ids = await _repository.GetAllWavesAsync();

        var tasks = ids.Select(id => _repository.GetByIdAsync(id));
        var waves = await Task.WhenAll(tasks);

        return waves.Where(w => w is not null).ToList()!;
    }

    public Task<Wave?> GetWaveByIdAsync(Guid id)
        => _repository.GetByIdAsync(id);

    public async Task<Wave> CreateWaveAsync(Wave wave)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(wave.Name, nameof(wave.Name));

        if (await _repository.ExistsAsync(wave.Id))
        {
            _logger.LogError($"Create wave of {wave.Id} unsuccessful");
            throw new InvalidOperationException($"Wave with ID {wave.Id} already exists");
        }
        _logger.LogInformation($"Create wave of {wave.Id} successful");
        return await _repository.CreateAsync(wave);
    }

    public async Task<Wave> UpdateWaveAsync(Guid id, Wave wave)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(wave.Name, nameof(wave.Name));

        if (id != wave.Id)
        {
            _logger.LogError($"Update wave of {wave.Id} unsuccessful");
            throw new ArgumentException("ID in route must match ID in body");
        }

        if (!await _repository.ExistsAsync(id))
        {
            _logger.LogError($"Update wave of {wave.Id} unsuccessful");
            throw new KeyNotFoundException($"Wave with ID {id} not found");
        }

        _logger.LogInformation($"Update wave of {wave.Id} successful");
        return await _repository.UpdateAsync(wave);
    }

    public Task<bool> WaveExistsAsync(Guid id)
        => _repository.ExistsAsync(id);
}