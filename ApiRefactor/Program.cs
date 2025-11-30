using ApiRefactor.Models;
using ApiRefactor.Repository;
using ApiRefactor.Service;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var connectionString = builder.Configuration.GetConnectionString("WavesDb")
    ?? "Data Source=App_Data/waves.db";
builder.Services.AddScoped<IWaveRepository>(sp => new WaveRepository(connectionString));
builder.Services.AddScoped<IWaveService, WaveService>();

var app = builder.Build();
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapGet("/api/wave", async (IWaveService service) =>
{
    var waves = await service.GetAllWavesAsync();
    return Results.Ok(new { Items = waves });
})
.WithName("GetWaves")
.WithOpenApi();

app.MapGet("/api/wave/{id}", async (Guid id, IWaveService service) =>
{
    var wave = await service.GetWaveByIdAsync(id);

    return wave == null
        ? Results.NotFound(new { Message = $"Wave with ID {id} not found" })
        : Results.Ok(wave);
})
.WithName("GetWaveById")
.WithOpenApi();

app.MapPost("/api/wave", async (Wave wave, IWaveService service) =>
{
    try
    {
        var created = await service.CreateWaveAsync(wave);
        return Results.Created($"/api/wave/{created.Id}", created);
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(new { Message = ex.Message });
    }
    catch (InvalidOperationException ex)
    {
        return Results.Conflict(new { Message = ex.Message });
    }
})
.WithName("CreateWave")
.WithOpenApi();

app.MapPut("/api/wave/{id}", async (Guid id, Wave wave, IWaveService service) =>
{
    try
    {
        var updated = await service.UpdateWaveAsync(id, wave);
        return Results.Ok(updated);
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(new { ex.Message });
    }
    catch (KeyNotFoundException ex)
    {
        return Results.NotFound(new { ex.Message });
    }
})
.WithName("UpdateWave")
.WithOpenApi();

app.Run();