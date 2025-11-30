using ApiRefactor.Models;

namespace ApiRefactor.Service;

public interface IWaveService
{
    Task<List<Wave>> GetAllWavesAsync();
    Task<Wave?> GetWaveByIdAsync(Guid id);
    Task<Wave> CreateWaveAsync(Wave wave);
    Task<Wave> UpdateWaveAsync(Guid id, Wave wave);
    Task<bool> WaveExistsAsync(Guid id);
}