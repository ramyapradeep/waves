using ApiRefactor.Models;

namespace ApiRefactor.Repository;

public interface IWaveRepository
{
    Task<List<Guid>> GetAllWavesAsync();
    Task<Wave?> GetByIdAsync(Guid id);
    Task<Wave> CreateAsync(Wave wave);
    Task<Wave> UpdateAsync(Wave wave);
    Task<bool> ExistsAsync(Guid id);
}
