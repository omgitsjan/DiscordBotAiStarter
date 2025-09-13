using System.Threading.Tasks;
using DiscordBot.Models;

namespace DiscordBot.Interfaces
{
    public interface IOpenWeatherMapService
    {
        Task<(bool Success, string Message, WeatherData? WeatherData)> GetWeatherAsync(string city);
    }
}
