using System.Net.Http.Headers;

namespace dyndnsupdate;

public static class Program
{
    enum DynDnsService
    {
        None,
        DynDnsIt,
        Dynv6Com
    }

    public static int Main(string[] args)
    {
        var parser = new CmdLineArgsParser.Parser();
        if (args.Length == 0) {
            parser.ShowInfo();
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

        var service = GetDynDnsService(options.Hostname!);
        if (service == DynDnsService.None) {
            if (!string.IsNullOrEmpty(options.Token)) {
                service = DynDnsService.Dynv6Com;
            } else {
                Console.WriteLine("Cannot get service from hostname");
                return -1;
            }
        }

        var optionsOk = true;
        switch (service) {
            case DynDnsService.DynDnsIt:
                if (string.IsNullOrEmpty(options.Username)) {
                    Console.WriteLine("Missing username");
                    optionsOk = false;
                }
                if (string.IsNullOrEmpty(options.Password)) {
                    Console.WriteLine("Missing password");
                    optionsOk = false;
                }
                break;
            case DynDnsService.Dynv6Com:
                if (string.IsNullOrEmpty(options.Token)) {
                    Console.WriteLine("Missing HTTP token");
                    optionsOk = false;
                }
                break;
        }

        if (!optionsOk)
            return -1;

        using var httpClient = new HttpClient();

        var ipAddress = GetIpv4Address(httpClient);
        if (string.IsNullOrEmpty(ipAddress)) {
            Console.WriteLine("Cannot get public IP address");
            return -1;
        }

        Console.WriteLine($"Public IP address: {ipAddress}");

        return service switch
        {
            DynDnsService.DynDnsIt => UpdateDynDnsIt(httpClient, options, ipAddress),
            DynDnsService.Dynv6Com => UpdateDynv6Com(httpClient, options, ipAddress),
            _ => -1
        };
    }

    private static string GetIpv4Address(HttpClient client)
    {
        string[] urls =
        {
            "https://ipinfo.io/ip",
            "https://checkip.amazonaws.com/",
            "https://ipv4.seeip.org",
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

    private static string GetIpv6Address(HttpClient client)
    {
        string[] urls =
        {
            "https://ipv6.seeip.org",
            "https://ipv6.icanhazip.com/"
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

    private static DynDnsService GetDynDnsService(string hostname)
    {
        var host = string.Empty;
        var idx = hostname.IndexOf('.');
        if (idx > 0 && idx < hostname.Length) {
            host = hostname.Substring(idx + 1).ToLower();
        }

        if (string.IsNullOrEmpty(host))
            return DynDnsService.None;

        var hosts = new Dictionary<DynDnsService, string[]>()
        {
            { DynDnsService.Dynv6Com, new[]{ "dns.army", "dns.navy", "dynv6.net", "v6.army", "v6.navy", "v6.rocks" } },
            { DynDnsService.DynDnsIt, new[]{ "homepc.it" } },
        };

        foreach (var kvp in hosts) {
            if (kvp.Value.Contains(host))
                return kvp.Key;
        }

        return DynDnsService.None;
    }

    private static int UpdateDynDnsIt(HttpClient httpClient, Options options, string ipAddress)
    {
        var basicAuth = Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes($"{options.Username}:{options.Password}"));
        using var request = new HttpRequestMessage(HttpMethod.Get, $"https://update.dyndns.it/?hostname={options.Hostname}&myip={ipAddress}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", basicAuth);

        using var response = httpClient.Send(request);
        var res = response.Content.ReadAsStringAsync().Result;
        if (!response.IsSuccessStatusCode) {
            Console.WriteLine($"Failed to update IP address: {res}");
            return -1;
        }

        if (!res.StartsWith("good ")) {
            Console.WriteLine($"Failed to update IP address: {res}");
            return -1;
        }
        Console.WriteLine($"IP address updated: {res}");
        return 0;
    }

    private static int UpdateDynv6Com(HttpClient httpClient, Options options, string ipAddress)
    {
        var ipv6Address = GetIpv6Address(httpClient);

        var url = $"https://dynv6.com/api/update?zone={options.Hostname}&ipv4={ipAddress}&token={options.Token}";
        if (!string.IsNullOrEmpty(ipv6Address))
            url = $"{url}&ipv6={ipv6Address}";
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        using var response = httpClient.Send(request);
        var res = response.Content.ReadAsStringAsync().Result;
        if (!response.IsSuccessStatusCode) {
            Console.WriteLine($"Failed to update IP address: {res}");
            return -1;
        }

        Console.WriteLine($"IP address updated: {res}");
        return 0;
    }
}