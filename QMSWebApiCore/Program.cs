using Microsoft.EntityFrameworkCore;
using QMSWebApiCore.Data;
using QMSWebApiCore.Services;
using QMSWebApiCore.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();

// Configure PostgreSQL
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Configure DBOptions
builder.Services.Configure<DBOptions>(
    builder.Configuration.GetSection("Database"));

// Register GateService
builder.Services.AddScoped<IGateInRepository, GateRepository>();
builder.Services.AddScoped<IGateOutRepository,GateOutRepository>();
builder.Services.AddScoped<IPreCoolRepository, PreCoolRepository>();


// Configure CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngular", policy =>
    {
        policy.WithOrigins("http://localhost:4200")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// Add Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

//app.UseHttpsRedirection();

app.UseCors("AllowAngular");

app.UseAuthorization();

app.MapControllers();

app.Run();