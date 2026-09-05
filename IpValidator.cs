using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace VirtualIPHost;

/// <summary>
/// Utilitaires de validation et de manipulation d'adresses IPv4.
/// </summary>
public static class IpValidator
{
    /// <summary>
    /// Vérifie qu'une chaîne représente une adresse IPv4 valide.
    /// </summary>
    public static bool IsValidIPv4(string ip)
    {
        return IPAddress.TryParse(ip, out var address) &&
               address.AddressFamily == AddressFamily.InterNetwork;
    }

    /// <summary>
    /// Détecte l'interface réseau active (celle utilisée par la route par défaut),
    /// en excluant loopback et interfaces inactives.
    /// </summary>
    public static string? GetActiveNetworkInterface()
    {
        foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
        {
            if (ni.OperationalStatus != OperationalStatus.Up)
                continue;
            if (ni.NetworkInterfaceType == NetworkInterfaceType.Loopback)
                continue;

            var ipProps = ni.GetIPProperties();
            var hasIPv4 = ipProps.UnicastAddresses
                .Any(a => a.Address.AddressFamily == AddressFamily.InterNetwork);

            if (hasIPv4 && ipProps.GatewayAddresses.Any())
            {
                return ni.Name;
            }
        }

        // Fallback : première interface active non-loopback avec une IPv4
        var fallback = NetworkInterface.GetAllNetworkInterfaces()
            .FirstOrDefault(ni =>
                ni.OperationalStatus == OperationalStatus.Up &&
                ni.NetworkInterfaceType != NetworkInterfaceType.Loopback &&
                ni.GetIPProperties().UnicastAddresses
                    .Any(a => a.Address.AddressFamily == AddressFamily.InterNetwork));

        return fallback?.Name;
    }

    /// <summary>
    /// Vérifie si l'adresse IP donnée est déjà configurée sur une interface locale.
    /// </summary>
    public static bool IsIPLocallyConfigured(string ip)
    {
        if (!IPAddress.TryParse(ip, out var target))
            return false;

        return NetworkInterface.GetAllNetworkInterfaces()
            .SelectMany(ni => ni.GetIPProperties().UnicastAddresses)
            .Any(a => a.Address.Equals(target));
    }

    /// <summary>
    /// Formate l'adresse IP avec un masque CIDR /32 pour ajout d'alias.
    /// </summary>
    public static string FormatIPWithCIDR(string ip)
    {
        return $"{ip}/32";
    }
}
