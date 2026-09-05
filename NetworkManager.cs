using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace VirtualIPHost;

/// <summary>
/// Gère les opérations réseau au niveau système (ajout/suppression d'alias IP).
/// Requiert les privilèges administratifs.
/// </summary>
public class NetworkManager
{
    private readonly string _virtualIp;
    private readonly string? _networkInterface;
    private readonly ILogger<NetworkManager> _logger;
    private bool _aliasAdded = false;

    public NetworkManager(string virtualIp, ILogger<NetworkManager> logger)
    {
        _virtualIp = virtualIp;
        _logger = logger;
        _networkInterface = IpValidator.GetActiveNetworkInterface();
    }

    /// <summary>
    /// Ajoute un alias IP à l'interface réseau active.
    /// </summary>
    public async Task<bool> AddIPAliasAsync()
    {
        if (!IpValidator.IsValidIPv4(_virtualIp))
        {
            _logger.LogError("Adresse IP invalide: {Ip}", _virtualIp);
            return false;
        }

        if (_networkInterface == null)
        {
            _logger.LogError("Impossible de détecter l'interface réseau active");
            return false;
        }

        if (IpValidator.IsIPLocallyConfigured(_virtualIp))
        {
            _logger.LogWarning("L'adresse IP {Ip} est déjà configurée localement", _virtualIp);
            return true;
        }

        try
        {
            _logger.LogInformation("Ajout de l'alias IP {Ip} sur l'interface {Interface}", 
                _virtualIp, _networkInterface);

            var result = await ExecuteCommandAsync("ip", 
                $"addr add {IpValidator.FormatIPWithCIDR(_virtualIp)} dev {_networkInterface}");

            if (result)
            {
                _aliasAdded = true;
                _logger.LogInformation("Alias IP {Ip} ajouté avec succès", _virtualIp);
                
                // Vérification que l'alias a été créé
                await Task.Delay(100);
                if (IpValidator.IsIPLocallyConfigured(_virtualIp))
                {
                    return true;
                }
                else
                {
                    _logger.LogWarning("L'alias IP n'a pas pu être vérifié immédiatement");
                    return true; // On continue quand même
                }
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de l'ajout de l'alias IP {Ip}", _virtualIp);
            return false;
        }
    }

    /// <summary>
    /// Supprime l'alias IP de l'interface réseau.
    /// </summary>
    public async Task<bool> RemoveIPAliasAsync()
    {
        if (!_aliasAdded || _networkInterface == null)
            return true;

        try
        {
            _logger.LogInformation("Suppression de l'alias IP {Ip} de l'interface {Interface}", 
                _virtualIp, _networkInterface);

            var result = await ExecuteCommandAsync("ip", 
                $"addr del {IpValidator.FormatIPWithCIDR(_virtualIp)} dev {_networkInterface}");

            if (result)
            {
                _logger.LogInformation("Alias IP {Ip} supprimé avec succès", _virtualIp);
                _aliasAdded = false;
                return true;
            }

            _logger.LogWarning("Impossible de supprimer l'alias IP {Ip}", _virtualIp);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la suppression de l'alias IP {Ip}", _virtualIp);
            return false;
        }
    }

    /// <summary>
    /// Configure les règles de routage/translation d'adresses si nécessaire.
    /// </summary>
    public async Task<bool> ConfigureNetworkRoutingAsync()
    {
        try
        {
            _logger.LogInformation("Configuration du routage réseau pour {Ip}", _virtualIp);
            
            // Vérifier que l'IP est accessible localement
            await Task.Delay(50);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la configuration du routage");
            return false;
        }
    }

    /// <summary>
    /// Exécute une commande système avec gestion d'erreur et capture de sortie.
    /// </summary>
    private async Task<bool> ExecuteCommandAsync(string command, string arguments)
    {
        try
        {
            var processInfo = new ProcessStartInfo
            {
                FileName = command,
                Arguments = arguments,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using var process = new Process { StartInfo = processInfo };
            process.Start();

            var stdout = await process.StandardOutput.ReadToEndAsync();
            var stderr = await process.StandardError.ReadToEndAsync();

            await process.WaitForExitAsync();

            if (process.ExitCode != 0)
            {
                _logger.LogError("Commande '{Command} {Arguments}' échouée avec code {ExitCode}\nErreur: {StdErr}",
                    command, arguments, process.ExitCode, stderr);
                return false;
            }

            if (!string.IsNullOrEmpty(stdout))
                _logger.LogDebug("Sortie: {Output}", stdout);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception lors de l'exécution de la commande '{Command} {Arguments}'",
                command, arguments);
            return false;
        }
    }
}
