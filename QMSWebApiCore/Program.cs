using QMSWebApiCore.Data;
using QMSWebApiCore.Models;
using QMSWebApiCore.Services;
using System.Text.Json;
using System.Text.Json.Serialization;
using Npgsql;
using static QMSWebApiCore.Services.PreLoadRepository;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

// Configure PostgreSQL
//builder.Services.AddDbContext<ApplicationDbContext>(options =>
//    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Configure DBOptions
builder.Services.Configure<DBOptions>(
    builder.Configuration.GetSection("Database"));

// Register GateService
builder.Services.AddScoped<IGateInRepository, GateInRepository>();
builder.Services.AddScoped<IGateOutRepository,GateOutRepository>();
builder.Services.AddScoped<IPreCoolRepository, PreCoolRepository>();
builder.Services.AddScoped<IRSUInRepository, RSUInRepository>();
builder.Services.AddScoped<ITruckOnDockRepository, TruckOnDockRepository>();
builder.Services.AddScoped<IPreLoadRepository, PreloadRepository>();
builder.Services.AddScoped<ILoadOnTruckRepository, LoadOnTruckRepository>();
builder.Services.AddScoped<IEDPRepository, EDPRepository>();
builder.Services.AddScoped<ITMSPlanRepository, TMSPlanRepository>();
builder.Services.AddScoped<IReportQMSRepository, ReportQMSRepository>();
builder.Services.AddScoped<IDashboardQMSRepository, DashboardRepository>();

// Configure CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngular", policy =>
    {
        policy.SetIsOriginAllowed(origin => true) // Allow any origin for development
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
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