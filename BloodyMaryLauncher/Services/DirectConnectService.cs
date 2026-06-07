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

    public static async Task<string> GetDirectConnectTargetAsync(CancellationToken cancellationToken = default)
    {
        using var response = await HttpClient.GetAsync(DirectConnectUri, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"Das Direct-Connect-Ziel konnte nicht geladen werden ({(int)response.StatusCode}).");
        }

        var connectTarget = (await response.Content.ReadAsStringAsync(cancellationToken)).Trim();
        if (string.IsNullOrWhiteSpace(connectTarget))
        {
            throw new InvalidOperationException("Die Direct-Connect-Antwort ist leer.");
        }

        if (!IsValidConnectTarget(connectTarget))
        {
            throw new InvalidOperationException("Das Direct-Connect-Ziel aus ip.txt ist ungültig.");
        }

        return connectTarget;
    }

    private static bool IsValidConnectTarget(string connectTarget)
    {
        if (IPAddress.TryParse(connectTarget, out _))
        {
            return true;
        }

        return TryCreateConnectUri(connectTarget, out _);
    }

    private static bool TryCreateConnectUri(string connectTarget, out Uri? connectUri)
    {
        connectUri = null;

        if (Uri.TryCreate(connectTarget, UriKind.Absolute, out var absoluteUri) && IsSupportedConnectUri(absoluteUri))
        {
            connectUri = absoluteUri;
            return true;
        }

        if (connectTarget.Contains("://", StringComparison.Ordinal))
        {
            return false;
        }

        if (Uri.TryCreate($"https://{connectTarget}", UriKind.Absolute, out var inferredUri) && IsSupportedConnectUri(inferredUri))
        {
            connectUri = inferredUri;
            return true;
        }

        return false;
    }

    private static bool IsSupportedConnectUri(Uri connectUri)
    {
        if (string.IsNullOrWhiteSpace(connectUri.Host))
        {
            return false;
        }

        if (!string.Equals(connectUri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase)
            && !string.Equals(connectUri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return !string.IsNullOrWhiteSpace(connectUri.AbsolutePath)
               && !string.Equals(connectUri.AbsolutePath, "/", StringComparison.Ordinal);
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