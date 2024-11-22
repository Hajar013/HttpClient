using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;




namespace testweather
{


    namespace apiW
    {
        // Define a DTO for weather data
        public class WeatherDTO
        {
            public string CityName { get; set; }
            public float TemperatureCelsius { get; set; }
        }
        class WeatherData
        {
            public string Name { get; set; }
            public MainData Main { get; set; }
        }

        class MainData
        {
            public float Temp { get; set; }
        }

        // Define an interface for the weather service
        public interface IWeatherService
        {
            Task<WeatherDTO> GetWeatherDataAsync(string cityName);
        }

        // Implement the weather service
        public class OpenWeatherMapService : IWeatherService
        {
            private readonly HttpClient _httpClient;
            private readonly string _apiKey;

            public OpenWeatherMapService(HttpClient httpClient, string apiKey)
            {
                _httpClient = httpClient;
                _apiKey = apiKey;
            }

            public async Task<WeatherDTO> GetWeatherDataAsync(string cityName)
            {
                string apiUrl = $"http://api.openweathermap.org/data/2.5/weather?q={cityName}&appid={_apiKey}";

                HttpResponseMessage response = await _httpClient.GetAsync(apiUrl);

                if (response.IsSuccessStatusCode)
                {
                    WeatherData weatherData = await response.Content.ReadFromJsonAsync<WeatherData>();

                    if (weatherData != null)
                    {
                        float temperatureKelvin = weatherData.Main.Temp;
                        float temperatureCelsius = temperatureKelvin - 273.15f;

                        return new WeatherDTO
                        {
                            CityName = weatherData.Name,
                            TemperatureCelsius = temperatureCelsius
                        };
                    }
                    else
                    {
                        throw new InvalidOperationException("Unable to deserialize the JSON response.");
                    }
                }
                else
                {
                    throw new HttpRequestException($"Error: {response.StatusCode}");
                }
            }
        }

        // Define a factory interface for the weather service
        public interface IWeatherServiceFactory
        {
            IWeatherService CreateWeatherService();
        }

        // Implement the weather service factory
        public class OpenWeatherMapServiceFactory : IWeatherServiceFactory
        {
            private readonly HttpClient _httpClient;
            private readonly string _apiKey;

            public OpenWeatherMapServiceFactory(HttpClient httpClient, string apiKey)
            {
                _httpClient = httpClient;
                _apiKey = apiKey;
            }

            public IWeatherService CreateWeatherService()
            {
                return new OpenWeatherMapService(_httpClient, _apiKey);
            }
        }

        internal class Program
        {
            static async Task Main()
            {
                // Replace "YOUR_API_KEY" with your actual OpenWeatherMap API key
                string apiKey = "958a0cca7d6e90effe76435c6f3c35d7";

                // Replace "London" with the desired city name
                string cityName = "London";

                // Setup DI
                var serviceProvider = new ServiceCollection()
                    .AddHttpClient()
                    .AddSingleton<IWeatherServiceFactory, OpenWeatherMapServiceFactory>(provider =>
                    {
                        var httpClient = provider.GetRequiredService<HttpClient>();
                        return new OpenWeatherMapServiceFactory(httpClient, apiKey);
                    })
                    .AddSingleton<IWeatherService>(provider =>
                    {
                        var factory = provider.GetRequiredService<IWeatherServiceFactory>();
                        return factory.CreateWeatherService();
                    })
                    .BuildServiceProvider();

                // Use the services
                var weatherService = serviceProvider.GetRequiredService<IWeatherService>();

                try
                {
                    // Use the weather service to fetch and process weather data
                    var weatherDTO = await weatherService.GetWeatherDataAsync(cityName);

                    // Display the city name and temperature on the console
                    Console.WriteLine($"City: {weatherDTO.CityName}");
                    Console.WriteLine($"Temperature: {weatherDTO.TemperatureCelsius}°C");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"An error occurred: {ex.Message}");
                }

                // Wait for user input before closing the console window
                Console.ReadLine();
            }
        }
    }


}
