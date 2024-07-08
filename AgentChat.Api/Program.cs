using AgentChat.Api.Business.Implementations;
using AgentChat.Api.Business.Interfaces;
using AgentChat.Api.Data.Context;
using AgentChat.Api.Domain.Entities;
using AgentChat.Api.ExtensionHandler;
using AgentChat.Api.Middleware;
using Hangfire;
using Hangfire.MemoryStorage;
using Hangfire.SqlServer;
using HangfireBasicAuthenticationFilter;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using System.Collections.Concurrent;
using System.Configuration;

var builder = WebApplication.CreateBuilder(args);
ConfigureServices(builder);

var app = builder.Build();
ConfigureApplicationPipeline(app);

static void ConfigureServices(WebApplicationBuilder builder)
{
    var configuration = builder.Configuration;
    var services = builder.Services;

    builder.Services.AddControllers();
    // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();

    var dbHost = Environment.GetEnvironmentVariable("DB_HOST");
    var dbName = Environment.GetEnvironmentVariable("DB_NAME");
    var dbPassword = Environment.GetEnvironmentVariable("DB_SA_PASSWORD");
    var connectionString = $"Server={dbHost};Database={dbName};User Id=sa;Password={dbPassword};Persist Security Info=True;Trusted_Connection=True;Encrypt=False;MultipleActiveResultSets=true;TrustServerCertificate=True;Integrated Security=false;";


    builder.Services.AddDbContext<ApplicationDbContext>(options =>
             options.UseSqlServer(connectionString));

    builder.Host.UseSerilog((hostContext, config) =>
      config.ReadFrom.Configuration(hostContext.Configuration));

    services.AddMediatR(configuration =>
    {
        configuration.RegisterServicesFromAssembly(typeof(Program).Assembly);
    });

    builder.Services.AddStackExchangeRedisCache(redisOptions =>
    {
        string connection = configuration.GetConnectionString("Redis");
        redisOptions.Configuration = connection;
    });

    builder.Services.AddSingleton(new ConcurrentDictionary<Guid, ChatSession>());

    // Initialize agents and register AgentAssignmentService
    var agents = ChatService.InitializeAgents();
    builder.Services.AddSingleton(provider =>
    {
        var logger = provider.GetRequiredService<ILogger<AgentAssignmentService>>();
        var cache = provider.GetRequiredService<IDistributedCache>();
        var queueService = provider.GetRequiredService<QueueService>();
        var monitorService = provider.GetRequiredService<MonitorService>();
        var activeChats = provider.GetRequiredService<ConcurrentDictionary<Guid, ChatSession>>();

        return new AgentAssignmentService(agents, activeChats, queueService, monitorService, logger, cache);
    });

    builder.Services.AddSingleton<MonitorService>();
    builder.Services.AddSingleton<QueueService>();
    builder.Services.AddSingleton<IChatService, ChatService>();

    // Add Hangfire services.
    services.AddHangfire(config => config
        .SetDataCompatibilityLevel(CompatibilityLevel.Version_170)
        .UseMaxArgumentSizeToRender(204800) // to avoid VALUE IS TOO BIG in the Dashboard 200MB
        .UseSimpleAssemblyNameTypeSerializer()
        .UseRecommendedSerializerSettings()
        .UseMemoryStorage());

    services.AddHangfireServer();

    builder.Services.AddExceptionHandler<GlobalExceptionHandlerMiddleware>();
    builder.Services.AddProblemDetails();
}


void ConfigureApplicationPipeline(WebApplication app)
{
    var configuration = builder.Configuration;
    // Configure the HTTP request pipeline.
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
        app.ApplyMigrations();
    }

    app.UseHangfireDashboard("/hangfire", new DashboardOptions
    {
        //The path for the Back To Site link. Set to null in order to hide the Back To  Site link.
        AppPath = configuration.GetSection("HangfireSettings:AppPath").Value,
        DashboardTitle = "Agent Chat",
        Authorization = new[]
                   {
                    new HangfireCustomBasicAuthenticationFilter{
                        User = configuration.GetSection("HangfireSettings:UserName").Value,
                        Pass = configuration.GetSection("HangfireSettings:Password").Value
                    }
                }
    });

    app.UseSerilogRequestLogging();

    app.UseHttpsRedirection();

    app.UseAuthorization();

    app.MapControllers();

    app.Run();
}

