using ChatServer.Extensions;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

// Service registrations
builder.Services.AddControllers();
builder.Services.AddOpenApi();

builder.Services.AddDatabaseServices(connectionString!);
builder.Services.AddApplicationServices();
builder.Services.AddRepositories();
builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddCorsPolicy();
builder.Services.AddSignalRServices();
builder.Services.AddRabbitMQ(builder.Configuration);

// Build and configure app
var app = builder.Build();
app.ConfigurePipeline();

app.Run();
