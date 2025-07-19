using ChatServer.Applications;
using ChatServer.Configs;
using ChatServer.Repositories;
using ChatServer.Repositories.Attachment;
using ChatServer.Repositories.Group;
using ChatServer.Repositories.Messenger;
using ChatServer.Repositories.Reaction;
using ChatServer.Services;
using ChatServer.SignalR;
using ChatServer.SignalR.Hubs;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Npgsql;
using RabbitMQ.Client;
using System.Data;
using System.Text;

namespace ChatServer.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddDatabaseServices(this IServiceCollection services, string connectionString)
        {
            services.AddScoped<IDbConnection>(sp => new NpgsqlConnection(connectionString));
            return services;
        }

        public static IServiceCollection AddApplicationServices(this IServiceCollection services)
        {
            services.AddScoped<IMessageService, MessageService>();
            services.AddScoped<IChatClientNotifier, SignalRChatClientNotifier>();
            return services;
        }

        public static IServiceCollection AddRepositories(this IServiceCollection services)
        {
            services.AddScoped<IMessageRepo, MessageRepo>();
            services.AddScoped<IUserRepo, UserRepo>();
            services.AddScoped<IAttachmentRepo, AttachmentRepo>();
            services.AddScoped<IMessagePublisher, MessagePublisher>();
            services.AddScoped<IReactionRepo, ReactionRepo>();
            services.AddScoped<IGroupRepository, GroupRepository>();
            return services;
        }

        public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = configuration["Jwt:Issuer"],
                    ValidateAudience = true,
                    ValidAudience = configuration["Jwt:Audience"],
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["Jwt:Secret"]!)),
                    RequireExpirationTime = true,
                    ClockSkew = TimeSpan.Zero
                };

                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        context.Request.Cookies.TryGetValue("access_token", out var token);
                        if (!string.IsNullOrEmpty(token))
                        {
                            context.Token = token;
                        }
                        return Task.CompletedTask;
                    }
                };
            });

            return services;
        }

        public static IServiceCollection AddCorsPolicy(this IServiceCollection services)
        {
            services.AddCors(opts =>
            {
                opts.AddPolicy("AllowNextApp", policy =>
                {
                    policy.WithOrigins("http://localhost:3000")
                          .SetIsOriginAllowed(origin =>
                          {
                              var uri = new Uri(origin);
                              return uri.IsLoopback || uri.Host.EndsWith(".aistudio.com.vn", StringComparison.OrdinalIgnoreCase);
                          })
                          .AllowAnyHeader()
                          .AllowAnyMethod()
                          .AllowCredentials();
                });
            });

            return services;
        }

        public static IServiceCollection AddRabbitMQ(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddOptions<RabbitMQOptions>()
                    .Bind(configuration.GetSection("RabbitMQ"))
                    .ValidateDataAnnotations();

            services.AddSingleton<IConnection>(sp =>
            {
                var opts = sp.GetRequiredService<IOptions<RabbitMQOptions>>().Value;
                var factory = new ConnectionFactory
                {
                    HostName = opts.HostName,
                    UserName = opts.UserName,
                    Password = opts.Password
                };
                return factory.CreateConnectionAsync().GetAwaiter().GetResult();
            });

            services.AddHostedService<RabbitMQConsumerService>();
            return services;
        }

        public static IServiceCollection AddSignalRServices(this IServiceCollection services)
        {
            services.AddSignalR();
            services.AddSingleton<IUserIdProvider, NameUserIdProvider>();
            return services;
        }

        public static IServiceCollection AddValidation(this IServiceCollection services)
        {
            services.AddFluentValidationAutoValidation();
            services.AddValidatorsFromAssemblyContaining<Program>();
            return services;
        }
    }
}