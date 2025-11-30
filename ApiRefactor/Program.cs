using ApiRefactor.Models;
using ApiRefactor.Repository;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var connectionString = builder.Configuration.GetConnectionString("WavesDb")
    ?? "Data Source=App_Data/waves.db";
builder.Services.AddScoped<IWaveRepository>(sp => new WaveRepository(connectionString));

var app = builder.Build();
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapGet("/api/wave", async (IWaveRepository repository) =>
{
    var waveIds = await repository.GetAllWavesAsync();
    var waves = new List<Wave>();

    foreach (var id in waveIds)
    {
        var wave = await repository.GetByIdAsync(id);
        if (wave != null)
        {
            waves.Add(wave);
        }
    }

    return Results.Ok(new { Items = waves });
})
.WithName("GetWaves")
.WithOpenApi();

app.MapGet("/api/wave/{id}", async (Guid id, IWaveRepository repository) =>
{
    var wave = await repository.GetByIdAsync(id);

    return wave == null
        ? Results.NotFound(new { Message = $"Wave with ID {id} not found" })
        : Results.Ok(wave);
})
.WithName("GetWaveById")
.WithOpenApi();

app.MapPost("/api/wave", async (Wave wave, IWaveRepository repository) =>
{
    var created = await repository.CreateAsync(wave);
    return Results.Created($"/api/wave/{created.Id}", created);
})
.WithName("CreateWave")
.WithOpenApi();

app.MapPut("/api/wave/{id}", async (Guid id, Wave wave, IWaveRepository repository) =>
{
    if (id != wave.Id)
    {
        return Results.BadRequest(new { Message = "ID in route must match ID in body" });
    }

    var updated = await repository.UpdateAsync(wave);
    return Results.Ok(updated);
})
.WithName("UpdateWave")
.WithOpenApi();

app.Run();