using System.Net.Http.Json;

namespace AssetFlow.Infrastructure.Services
{
    public class GeoIpService
    {
        private readonly IHttpClientFactory _httpFactory;

        public GeoIpService(IHttpClientFactory httpFactory)
        {
            _httpFactory = httpFactory;
        }

        public async Task<string?> GetLocationAsync(string ip)
        {
            // IPs locales → pas de géoloc
            if (string.IsNullOrEmpty(ip)
                || ip == "::1"
                || ip == "127.0.0.1"
                || ip.StartsWith("192.168.")
                || ip.StartsWith("10.")
                || ip == "unknown")
                return "Réseau local";

            try
            {
                var client = _httpFactory.CreateClient();
                var response = await client.GetFromJsonAsync<GeoIpResponse>(
                    $"http://ip-api.com/json/{ip}?fields=status,city,countryCode");

                return response?.Status == "success"
                    ? $"{response.City}, {response.CountryCode}"
                    : null;
            }
            catch
            {
                return null;
            }
        }

        private record GeoIpResponse(
            string? Status,
            string? City,
            string? CountryCode);
    }
}