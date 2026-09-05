using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;

namespace VirtualIPHost;

/// <summary>
/// Orchestrateur principal de l'application.
/// Gère la validation des entrées, la configuration réseau et le cycle de vie du processus.
/// </summary>
public class ApplicationOrchestrator : IAsyncDisposable
{
    private readonly string _virtualIp;
    private readonly string _command;
    private readonly string _arguments;
    private readonly ILogger<ApplicationOrchestrator> _logger;
    
    private NetworkManager? _networkManager;
    private ProcessManager? _processManager;

    public ApplicationOrchestrator(string virtualIp, string command, string arguments,
        ILogger<ApplicationOrchestrator> logger)
    {
        _virtualIp = virtualIp;
        _command = command;
        _arguments = arguments;
        _logger = logger;
    }

    /// <summary>
    /// Valide les conditions préalables à l'exécution.
    /// </summary>
    public bool ValidatePrerequisites()
    {
        _logger.LogInformation("=== Validation des conditions préalables ===");

        // Vérifier le système d'exploitation
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            _logger.LogError("Cette application ne fonctionne que sur Linux");
            return false;
        }
        _logger.LogInformation("✓ Système d'exploitation: Linux détecté");

        // Vérifier les privilèges administratifs
        if (!IsRunningAsRoot())
        {
            _logger.LogError("Cette application doit être exécutée avec les privilèges administratifs (sudo ou CAP_NET_ADMIN)");
            return false;
        }
        _logger.LogInformation("✓ Privilèges administratifs détectés");

        // Valider l'adresse IP
        if (!IpValidator.IsValidIPv4(_virtualIp))
        {
            _logger.LogError("Adresse IP invalide: {Ip}", _virtualIp);
            return false;
        }
        _logger.LogInformation("✓ Adresse IP valide: {Ip}", _virtualIp);

        // Vérifier que l'interface réseau existe
        var iface = IpValidator.GetActiveNetworkInterface();
        if (iface == null)
        {
            _logger.LogError("Impossible de détecter une interface réseau active");
            return false;
        }
        _logger.LogInformation("✓ Interface réseau active détectée: {Interface}", iface);

        // Vérifier que la commande existe
        if (!CommandExists(_command))
        {
            _logger.LogWarning("Attention: La commande '{Command}' n'a pas pu être vérifiée. Elle peut ne pas exister ou ne pas être dans le PATH", _command);
        }
        else
        {
            _logger.LogInformation("✓ Commande disponible: {Command}", _command);
        }

        _logger.LogInformation("=== Tous les contrôles préalables sont passés ===\n");
        return true;
    }

    /// <summary>
    /// Exécute le processus avec configuration réseau.
    /// </summary>
    public async Task<int> ExecuteAsync()
    {
        try
        {
            // Créer le gestionnaire réseau
            _networkManager = new NetworkManager(_virtualIp, 
                _loggerFactory.CreateLogger<NetworkManager>());

            // Ajouter l'alias IP
            _logger.LogInformation("=== Configuration réseau ===");
            if (!await _networkManager.AddIPAliasAsync())
            {
                _logger.LogError("Impossible de configurer l'alias IP");
                return 1;
            }

            // Configurer le routage si nécessaire
            if (!await _networkManager.ConfigureNetworkRoutingAsync())
            {
                _logger.LogError("Impossible de configurer le routage réseau");
                await _networkManager.RemoveIPAliasAsync();
                return 1;
            }

            _logger.LogInformation("=== Configuration réseau complétée ===\n");

            // Créer et démarrer le gestionnaire de processus
            _processManager = new ProcessManager(_command, _arguments, _virtualIp,
                _loggerFactory.CreateLogger<ProcessManager>());

            _logger.LogInformation("=== Démarrage du processus enfant ===");
            if (!await _processManager.StartProcessAsync())
            {
                _logger.LogError("Impossible de démarrer le processus enfant");
                await _networkManager.RemoveIPAliasAsync();
                return 1;
            }

            _logger.LogInformation("=== Processus en cours d'exécution ===\n");

            // Attendre la fin du processus
            var exitCode = await _processManager.WaitForExitAsync();

            return exitCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de l'exécution");
            return 1;
        }
    }

    /// <summary>
    /// Arrête proprement tous les éléments.
    /// </summary>
    public async Task ShutdownAsync()
    {
        _logger.LogInformation("\n=== Arrêt de l'application ===");

        // Arrêter le processus enfant
        if (_processManager != null)
        {
            await _processManager.StopProcessAsync();
            await _processManager.DisposeAsync();
        }

        // Supprimer l'alias IP
        if (_networkManager != null)
        {
            await _networkManager.RemoveIPAliasAsync();
        }

        _logger.LogInformation("=== Application arrêtée proprement ===");
    }

    /// <summary>
    /// Vérifie si l'application est exécutée en tant que root.
    /// </summary>
    private bool IsRunningAsRoot()
    {
        try
        {
            return Environment.UserName.Equals("root", StringComparison.OrdinalIgnoreCase) ||
                   (int.TryParse(Environment.GetEnvironmentVariable("UID"), out var uid) && uid == 0);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Vérifie si une commande existe dans le PATH.
    /// </summary>
    private bool CommandExists(string command)
    {
        try
        {
            var processInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "which",
                Arguments = command,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true
            };

            using var process = System.Diagnostics.Process.Start(processInfo);
            process?.WaitForExit();
            return process?.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }

    public async ValueTask DisposeAsync()
    {
        await ShutdownAsync();
    }

    private readonly ILoggerFactory _loggerFactory = Microsoft.Extensions.Logging.LoggerFactory.Create(builder =>
    {
        builder.AddConsole();
        builder.SetMinimumLevel(LogLevel.Information);
    });
}
