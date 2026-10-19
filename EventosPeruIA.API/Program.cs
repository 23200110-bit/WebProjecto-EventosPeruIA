using EventosPeruIA.CORE.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

var _config = builder.Configuration;
var cnx = _config.GetConnectionString("DevConnection");

builder.Services.AddDbContext<PostgresContext>(options =>
    options.UseNpgsql(cnx));

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseAuthorization();

app.MapControllers();

app.Run();
