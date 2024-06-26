using Microsoft.OpenApi.Models;
using ScraperApi.Services;
using ScraperApi.Data;
using Microsoft.EntityFrameworkCore;
using ScraperApi.Data.Repositories;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();

// Register Swagger services
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Scraper API", Version = "v1" });
});

// Register Database Context
builder.Services.AddDbContext<ActorsDbContext>(options =>
    options.UseInMemoryDatabase("ActorsDb"));

// Register services and repositories
builder.Services.AddScoped<IScraperService, ImdbScraperService>();
builder.Services.AddScoped<IActorRepository, ActorRepository>();
builder.Services.AddScoped<IActorService, ActorService>();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Scraper API V1");
        c.RoutePrefix = string.Empty; // serve the Swagger UI at the root URL
    });
}

// Preload data at application startup 
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<ActorsDbContext>();
        var scraperService = services.GetRequiredService<IScraperService>();

        if (!context.Actors.Any())
        {
            var actors = await scraperService.GetTopActorsAsync();
            await scraperService.LoadActorsIntoDatabaseAsync(actors);
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"An error occurred while seeding the DB: {ex.Message}");
    }
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.UseRouting();

app.MapControllers();

app.Run();