using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace BloodyMaryLauncher.Services;

public static class DirectConnectService
{
    private static readonly Uri DirectConnectUri = new("https://bloodymary.io/ip.txt");
    private static readonly HttpClient HttpClient = CreateHttpClient();

    public static async Task<string> GetDirectConnectAddressAsync(CancellationToken cancellationToken = default)
    {
        using var response = await HttpClient.GetAsync(DirectConnectUri, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"Die Server-IP konnte nicht geladen werden ({(int)response.StatusCode}).");
        }

        var address = (await response.Content.ReadAsStringAsync(cancellationToken)).Trim();
        if (string.IsNullOrWhiteSpace(address))
        {
            throw new InvalidOperationException("Die Server-IP-Antwort ist leer.");
        }

        if (!IPAddress.TryParse(address, out _))
        {
            throw new InvalidOperationException("Die Server-IP aus ip.txt ist ungültig.");
        }

        return address;
    }

    private static HttpClient CreateHttpClient()
    {
        var client = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(10)
        };

        client.DefaultRequestHeaders.UserAgent.ParseAdd("BloodyMaryLauncher-DirectConnect");
        return client;
    }
}