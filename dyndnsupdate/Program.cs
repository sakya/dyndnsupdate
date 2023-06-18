using System.Net.Http.Headers;

namespace dyndnsupdate;

public static class Program
{
    public static int Main(string[] args)
    {
        var parser = new CmdLineArgsParser.Parser();
        if (args.Length == 0) {
            parser.ShowInfo(true);
            parser.ShowUsage<Options>();
            return -1;
        }

        parser.ShowInfo(false);
        var options = parser.Parse<Options>(args, out var errors);
        if (errors.Count > 0) {
            Console.WriteLine("Errors:");
            foreach (var error in errors) {
                Console.WriteLine(error.Message);
            }
            return -1;
        }

        using var httpClient = new HttpClient();

        var ipAddress = GetIpAddress(httpClient);
        if (string.IsNullOrEmpty(ipAddress)) {
            Console.WriteLine("Cannot get public IP address");
            return -1;
        }

        Console.WriteLine($"Public IP address: {ipAddress}");
        var basicAuth = Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes($"{options.Username}:{options.Password}"));
        var request = new HttpRequestMessage(HttpMethod.Get, $"https://update.dyndns.it/update.dyndns.it/?hostname={options.Hostname}&myip={ipAddress}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", basicAuth);

        var response = httpClient.Send(request);
        var res = response.Content.ReadAsStringAsync().Result;
        if (!response.IsSuccessStatusCode) {
            Console.WriteLine($"Failed to update IP address: {res}");
            return -1;
        }

        Console.WriteLine($"IP address updated: {res}");
        return 0;
    }

    private static string GetIpAddress(HttpClient client)
    {
        string[] urls =
        {
            "https://ipinfo.io/ip",
            "https://checkip.amazonaws.com/",
            "https://icanhazip.com",
            "https://wtfismyip.com/text"
        };

        foreach (var url in urls) {
            try {
                var ipAddress = client.GetStringAsync(url).Result;
                if (!string.IsNullOrEmpty(ipAddress)) {
                    return ipAddress
                        .Replace("\r", string.Empty)
                        .Replace("\n", string.Empty)
                        .Trim();
                }
            } catch {
                // ignored
            }
        }
        return string.Empty;
    }
}