using ChatServer.SignalR.Hubs;
using ChatServer.Middleware;

namespace ChatServer.Extensions
{
    public static class WebApplicationExtensions
    {
        public static WebApplication ConfigurePipeline(this WebApplication app)
        {
            if (app.Environment.IsDevelopment())
            {
                app.MapOpenApi();
            }

            app.UseMiddleware<GlobalExceptionHandlerMiddleware>();
            app.UseHttpsRedirection();
            app.UseCors("AllowNextApp");
            app.UseAuthentication();
            app.UseAuthorization();

            app.ConfigureStaticFiles();
            app.ConfigureRouting();

            return app;
        }

        private static WebApplication ConfigureStaticFiles(this WebApplication app)
        {
            app.UseStaticFiles(new StaticFileOptions
            {
                FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(
                    Path.Combine(app.Environment.ContentRootPath, "uploads")),
                RequestPath = "/uploads"
            });

            return app;
        }

        private static WebApplication ConfigureRouting(this WebApplication app)
        {
            app.MapControllers();
            app.MapHub<ChatHub>("/chathub").RequireCors("AllowNextApp");
            app.MapWeatherForecast();

            return app;
        }

        private static WebApplication MapWeatherForecast(this WebApplication app)
        {
            var summaries = new[]
            {
                "Freezing","Bracing","Chilly","Cool","Mild",
                "Warm","Balmy","Hot","Sweltering","Scorching"
            };

            app.MapGet("/weatherforecast", () =>
            {
                var forecast = Enumerable.Range(1, 5).Select(index =>
                    new WeatherForecast(
                        DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                        Random.Shared.Next(-20, 55),
                        summaries[Random.Shared.Next(summaries.Length)]
                    )).ToArray();
                return forecast;
            })
            .WithName("GetWeatherForecast");

            return app;
        }
    }

    record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
    {
        public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
    }
}