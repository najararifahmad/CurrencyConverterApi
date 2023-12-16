using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Runtime.ConstrainedExecution;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddLogging();
builder.Logging.AddConsole();


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () =>
{
    var forecast = Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast")
.WithOpenApi();

app.MapGet("/convert", async Task<object> (HttpRequest request, 
                                           [FromServices] ILogger<string> logger,
                                           [FromQuery] string sourceCurrency, 
                                           [FromQuery] string targetCurrency,
                                           [FromQuery] decimal amount) =>
{
    try
    {

        var exchangeRateKey = sourceCurrency.ToUpperInvariant() + "_TO_" + targetCurrency.ToUpperInvariant();

        var exchangeRateKeyEnv = Environment.GetEnvironmentVariable(exchangeRateKey);

        string text = await File.ReadAllTextAsync("exchangeRates.json");

        Dictionary<string, decimal> exchangeRatesDictionary = JsonConvert.DeserializeObject<Dictionary<string, decimal>>(text)!;

        if(!string.IsNullOrEmpty(exchangeRateKeyEnv))
        {
            decimal exchangeRate = 0.0M;

            bool converted = Decimal.TryParse(exchangeRateKeyEnv, out exchangeRate);

            if(!converted)
            {
                logger.Log(LogLevel.Error, "Exchange Rate from environment variable is not a valid number.");
                throw new InvalidCastException("Exchange Rate must be a valid number.");
            }

            decimal convertedAmount = amount * exchangeRate;

            logger.Log(LogLevel.Information, "Currency converted successfully using environment variable.");

            return new
            {
                ExchangeRate = exchangeRate,
                ConvertedAmount = convertedAmount
            };
        }

        if (exchangeRatesDictionary.ContainsKey(exchangeRateKey))
        {
            decimal exchangeRate = exchangeRatesDictionary[exchangeRateKey];

            decimal convertedAmount = amount * exchangeRate;

            logger.Log(LogLevel.Information, "Currency converted successfully using exchangeRates.json file.");

            return new
            {
                ExchangeRate = exchangeRate,
                ConvertedAmount = convertedAmount
            };
        }

        logger.Log(LogLevel.Error, "Exchage Rate not found in exchangeRates.json file.");

        return new
        {
            ExchangeRate = 0,
            ConvertedAmount = 0,
            Message = "Invalid Exchange Rate..."
        };

    }
    catch (FileNotFoundException)
    {
        logger.Log(LogLevel.Error, "exchangeRates.json file not found.");

        return new
        {
            ExchangeRate = 0,
            ConvertedAmount = 0,
            Message = "exchangeRates.json file not found."
        };
    }
    catch(InvalidCastException cEx)
    {
        logger.Log(LogLevel.Error, "The value for the amount parameter is not a valid number.");

        return new
        {
            ExchangeRate = 0,
            ConvertedAmount = 0,
            Message = cEx.Message!
        };
    }
    catch(Exception)
    {
        logger.Log(LogLevel.Error, "Unknown error occured. Please try again...");

        return new
        {
            ExchangeRate = 0,
            ConvertedAmount = 0,
            Message = "Unknown error occured. Please try again..."
        };
    }

})
.WithName("convert")
.WithOpenApi();

app.Run();

internal record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
