

using MagicVilla_VillaAPI;
using MagicVilla_VillaAPI.Data;
using MagicVilla_VillaAPI.Logging;
using MagicVilla_VillaAPI.Repository;
using MagicVilla_VillaAPI.Repository.IRepository;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

//Log.Logger = new LoggerConfiguration().MinimumLevel.Debug().WriteTo.File("log/ villalogs.txt",rollingInterval:RollingInterval.Day).CreateLogger();   --> only use when Serilog (Nuget package) is installed.

//builder.Host.UseSerilog(); -- only use when serilog (Nuget Package) is installed.

builder.Services.AddDbContext<ApplicationDbContext>(option =>
{
    option.UseSqlServer(builder.Configuration.GetConnectionString("DefaultSQLConnection"));
});

builder.Services.AddScoped<IVillaRepository,VillaRepository>();

builder.Services.AddScoped<IVillaNumberRepository,VillaNumberRepository>();

builder.Services.AddAutoMapper(typeof(MappingConfig)); //----> adding auto mapper.

builder.Services.AddControllers(option =>
{
    //option.ReturnHttpNotAcceptable=true;  ---> Accet all types if turn off!

}).AddNewtonsoftJson().AddXmlDataContractSerializerFormatters();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
//builder.Services.AddSingleton<ILogging,LoggingV2>(); --> Turn on if you don't like the default one.
var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
