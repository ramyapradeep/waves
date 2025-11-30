using ApiRefactor.Models;
using ApiRefactor.Repository;
using ApiRefactor.Service;
using Microsoft.Extensions.Logging;
using Moq;
using Shouldly;

namespace ApiRefactor.Tests;

public class WaveServiceTests
{
    private readonly Mock<IWaveRepository> _mockRepository;
    private readonly Mock<ILogger<WaveService>> _mocklogger;
    private readonly WaveService _service;

    public WaveServiceTests()
    {
        _mockRepository = new Mock<IWaveRepository>();
        _mocklogger = new Mock<ILogger<WaveService>>();

        _service = new WaveService(_mockRepository.Object, _mocklogger.Object);
    }

    #region GetAllWavesAsync Tests

    [Fact]
    public async Task GetAllWavesAsync_WithEmptyDatabase_ShouldReturnEmptyList()
    {
        // Arrange
        _mockRepository.Setup(r => r.GetAllWavesAsync())
            .ReturnsAsync([]);

        // Act
        var result = await _service.GetAllWavesAsync();

        // Assert
        result.ShouldNotBeNull();
        result.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetAllWavesAsync_WithMultipleWaves_ShouldReturnAllWaves()
    {
        // Arrange
        var wave1 = new Wave { Id = Guid.NewGuid(), Name = "Wave 1", WaveDate = DateTime.Now };
        var wave2 = new Wave { Id = Guid.NewGuid(), Name = "Wave 2", WaveDate = DateTime.Now };
        var wave3 = new Wave { Id = Guid.NewGuid(), Name = "Wave 3", WaveDate = DateTime.Now };

        _mockRepository.Setup(r => r.GetAllWavesAsync())
            .ReturnsAsync([wave1.Id, wave2.Id, wave3.Id]);

        _mockRepository.Setup(r => r.GetByIdAsync(wave1.Id)).ReturnsAsync(wave1);
        _mockRepository.Setup(r => r.GetByIdAsync(wave2.Id)).ReturnsAsync(wave2);
        _mockRepository.Setup(r => r.GetByIdAsync(wave3.Id)).ReturnsAsync(wave3);

        // Act
        var result = await _service.GetAllWavesAsync();

        // Assert
        result.Count.ShouldBe(3);
        result.ShouldContain(w => w.Id == wave1.Id);
        result.ShouldContain(w => w.Id == wave2.Id);
        result.ShouldContain(w => w.Id == wave3.Id);
    }

    [Fact]
    public async Task GetAllWavesAsync_WithNullWave_ShouldSkipNullWaves()
    {
        // Arrange
        var wave1 = new Wave { Id = Guid.NewGuid(), Name = "Wave 1", WaveDate = DateTime.Now };
        var wave2Id = Guid.NewGuid();
        var wave3 = new Wave { Id = Guid.NewGuid(), Name = "Wave 3", WaveDate = DateTime.Now };

        _mockRepository.Setup(r => r.GetAllWavesAsync())
            .ReturnsAsync([wave1.Id, wave2Id, wave3.Id]);

        _mockRepository.Setup(r => r.GetByIdAsync(wave1.Id)).ReturnsAsync(wave1);
        _mockRepository.Setup(r => r.GetByIdAsync(wave2Id)).ReturnsAsync((Wave?)null);
        _mockRepository.Setup(r => r.GetByIdAsync(wave3.Id)).ReturnsAsync(wave3);

        // Act
        var result = await _service.GetAllWavesAsync();

        // Assert
        result.Count.ShouldBe(2);
        result.ShouldContain(w => w.Id == wave1.Id);
        result.ShouldContain(w => w.Id == wave3.Id);
    }

    #endregion

    #region GetWaveByIdAsync Tests

    [Fact]
    public async Task GetWaveByIdAsync_WithExistingId_ShouldReturnWave()
    {
        // Arrange
        var wave = new Wave { Id = Guid.NewGuid(), Name = "Test Wave", WaveDate = DateTime.Now };
        _mockRepository.Setup(r => r.GetByIdAsync(wave.Id)).ReturnsAsync(wave);

        // Act
        var result = await _service.GetWaveByIdAsync(wave.Id);

        // Assert
        result.ShouldNotBeNull();
        result.Id.ShouldBe(wave.Id);
        result.Name.ShouldBe(wave.Name);
    }

    [Fact]
    public async Task GetWaveByIdAsync_WithNonExistentId_ShouldReturnNull()
    {
        // Arrange
        var id = Guid.NewGuid();
        _mockRepository.Setup(r => r.GetByIdAsync(id)).ReturnsAsync((Wave?)null);

        // Act
        var result = await _service.GetWaveByIdAsync(id);

        // Assert
        result.ShouldBeNull();
    }

    #endregion

    #region CreateWaveAsync Tests

    [Fact]
    public async Task CreateWaveAsync_WithValidWave_ShouldCreateWave()
    {
        // Arrange
        var wave = new Wave { Id = Guid.NewGuid(), Name = "New Wave", WaveDate = DateTime.Now };
        _mockRepository.Setup(r => r.ExistsAsync(wave.Id)).ReturnsAsync(false);
        _mockRepository.Setup(r => r.CreateAsync(wave)).ReturnsAsync(wave);

        // Act
        var result = await _service.CreateWaveAsync(wave);

        // Assert
        result.ShouldNotBeNull();
        result.Id.ShouldBe(wave.Id);
        _mockRepository.Verify(r => r.CreateAsync(wave), Times.Once);
    }

    [Fact]
    public async Task CreateWaveAsync_WithEmptyName_ShouldThrowArgumentException()
    {
        // Arrange
        var wave = new Wave { Id = Guid.NewGuid(), Name = "", WaveDate = DateTime.Now };

        // Act & Assert
        var exception = await Should.ThrowAsync<ArgumentException>(async () =>
        {
            await _service.CreateWaveAsync(wave);
        });

        exception.Message.ShouldContain("The value cannot be an empty string or composed entirely of whitespace. (Parameter 'Name')");
    }

    [Fact]
    public async Task CreateWaveAsync_WithNullName_ShouldThrowArgumentException()
    {
        // Arrange
        var wave = new Wave { Id = Guid.NewGuid(), Name = null, WaveDate = DateTime.Now };

        // Act & Assert
        var exception = await Should.ThrowAsync<ArgumentException>(async () =>
        {
            await _service.CreateWaveAsync(wave);
        });
    }

    [Fact]
    public async Task CreateWaveAsync_WithDuplicateId_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var wave = new Wave { Id = Guid.NewGuid(), Name = "Test Wave", WaveDate = DateTime.Now };
        _mockRepository.Setup(r => r.ExistsAsync(wave.Id)).ReturnsAsync(true);

        // Act & Assert
        var exception = await Should.ThrowAsync<InvalidOperationException>(async () =>
        {
            await _service.CreateWaveAsync(wave);
        });

        exception.Message.ShouldContain("already exists");
        _mockRepository.Verify(r => r.CreateAsync(It.IsAny<Wave>()), Times.Never);
    }

    #endregion

    #region UpdateWaveAsync Tests

    [Fact]
    public async Task UpdateWaveAsync_WithValidWave_ShouldUpdateWave()
    {
        // Arrange
        var wave = new Wave { Id = Guid.NewGuid(), Name = "Updated Wave", WaveDate = DateTime.Now };
        _mockRepository.Setup(r => r.ExistsAsync(wave.Id)).ReturnsAsync(true);
        _mockRepository.Setup(r => r.UpdateAsync(wave)).ReturnsAsync(wave);

        // Act
        var result = await _service.UpdateWaveAsync(wave.Id, wave);

        // Assert
        result.ShouldNotBeNull();
        result.Name.ShouldBe("Updated Wave");
        _mockRepository.Verify(r => r.UpdateAsync(wave), Times.Once);
    }

    [Fact]
    public async Task UpdateWaveAsync_WithEmptyName_ShouldThrowArgumentException()
    {
        // Arrange
        var wave = new Wave { Id = Guid.NewGuid(), Name = "", WaveDate = DateTime.Now };

        // Act & Assert
        await Should.ThrowAsync<ArgumentException>(async () =>
        {
            await _service.UpdateWaveAsync(wave.Id, wave);
        });
    }

    [Fact]
    public async Task UpdateWaveAsync_WithMismatchedIds_ShouldThrowArgumentException()
    {
        // Arrange
        var id = Guid.NewGuid();
        var wave = new Wave { Id = Guid.NewGuid(), Name = "Test", WaveDate = DateTime.Now };

        // Act & Assert
        var exception = await Should.ThrowAsync<ArgumentException>(async () =>
        {
            await _service.UpdateWaveAsync(id, wave);
        });

        exception.Message.ShouldContain("ID in route must match ID in body");
    }

    [Fact]
    public async Task UpdateWaveAsync_WithNonExistentWave_ShouldThrowKeyNotFoundException()
    {
        // Arrange
        var wave = new Wave { Id = Guid.NewGuid(), Name = "Test Wave", WaveDate = DateTime.Now };
        _mockRepository.Setup(r => r.ExistsAsync(wave.Id)).ReturnsAsync(false);

        // Act & Assert
        var exception = await Should.ThrowAsync<KeyNotFoundException>(async () =>
        {
            await _service.UpdateWaveAsync(wave.Id, wave);
        });

        exception.Message.ShouldContain("not found");
        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<Wave>()), Times.Never);
    }

    #endregion

    #region WaveExistsAsync Tests

    [Fact]
    public async Task WaveExistsAsync_WithExistingWave_ShouldReturnTrue()
    {
        // Arrange
        var id = Guid.NewGuid();
        _mockRepository.Setup(r => r.ExistsAsync(id)).ReturnsAsync(true);

        // Act
        var result = await _service.WaveExistsAsync(id);

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public async Task WaveExistsAsync_WithNonExistentWave_ShouldReturnFalse()
    {
        // Arrange
        var id = Guid.NewGuid();
        _mockRepository.Setup(r => r.ExistsAsync(id)).ReturnsAsync(false);

        // Act
        var result = await _service.WaveExistsAsync(id);

        // Assert
        result.ShouldBeFalse();
    }

    #endregion
}