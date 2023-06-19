using CmdLineArgsParser;
using CmdLineArgsParser.Attributes;

namespace dyndnsupdate;

public class Options : IOptions
{
    [Option("hostname", 'h',
        Description = "The hostname to update",
        Required = true)]
    public string? Hostname { get; set; }

    [Option("username", 'u',
        Description = "The dyndns.it username",
        Required = false)]
    public string? Username { get; set; }

    [Option("password", 'p',
        Description = "The dyndns.it password",
        Required = false)]
    public string? Password { get; set; }

    [Option("token", 't',
        Description = "The dynv6.com HTTP token",
        Required = false)]
    public string? Token { get; set; }
}