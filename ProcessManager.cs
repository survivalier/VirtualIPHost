using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace VirtualIPHost;

/// <summary>
/// Gère le lancement, la surveillance et l'arrêt du processus enfant.
/// Implémente la capture des signaux d'arrêt pour nettoyage propre.
/// </summary>
public class ProcessManager : IAsyncDisposable
{
    private readonly string _command;
    private readonly string _arguments;
    private readonly string _virtualIp;
    private readonly ILogger<ProcessManager> _logger;
    private readonly CancellationTokenSource _shutdownCts;
    
    private Process? _childProcess;
    private TaskCompletionSource<int>? _exitTcs;

    public ProcessManager(string command, string arguments, string virtualIp, 
        ILogger<ProcessManager> logger)
    {
        _command = command;
        _arguments = arguments;
        _virtualIp = virtualIp;
        _logger = logger;
        _shutdownCts = new CancellationTokenSource();
    }

    /// <summary>
    /// Lance le processus enfant en le liant à l'adresse IP virtuelle.
    /// </summary>
    public async Task<bool> StartProcessAsync()
    {
        try
        {
            // Configuration du processus enfant
            var processInfo = new ProcessStartInfo
            {
                FileName = _command,
                Arguments = _arguments,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = false, // Afficher la console du processus
            };

            // Variables d'environnement pour forcer la liaison à l'IP virtuelle
            // Le processus enfant doit utiliser cette variable si compatible
            processInfo.Environment["VIRTUAL_IP"] = _virtualIp;

            _logger.LogInformation("Lancement du processus: {Command} {Arguments}", 
                _command, _arguments);
            _logger.LogInformation("Adresse IP virtuelle configurée: {Ip}", _virtualIp);

            _childProcess = new Process { StartInfo = processInfo };
            _exitTcs = new TaskCompletionSource<int>();

            // Abonnement aux événements de sortie
            _childProcess.EnableRaisingEvents = true;
            _childProcess.Exited += OnProcessExited;

            if (!_childProcess.Start())
            {
                _logger.LogError("Impossible de démarrer le processus {Command}", _command);
                return false;
            }

            _logger.LogInformation("Processus enfant démarré avec PID {Pid}", _childProcess.Id);

            // Lire les flux de sortie en arrière-plan
            _ = ReadStreamAsync(_childProcess.StandardOutput, "STDOUT");
            _ = ReadStreamAsync(_childProcess.StandardError, "STDERR");

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors du démarrage du processus {Command}", _command);
            return false;
        }
    }

    /// <summary>
    /// Attend l'arrêt du processus enfant.
    /// </summary>
    public async Task<int> WaitForExitAsync()
    {
        if (_exitTcs == null)
            return -1;

        return await _exitTcs.Task;
    }

    /// <summary>
    /// Arrête proprement le processus enfant en envoyant SIGTERM d'abord, puis SIGKILL.
    /// </summary>
    public async Task<bool> StopProcessAsync(int timeoutMs = 5000)
    {
        if (_childProcess == null || _childProcess.HasExited)
        {
            _logger.LogInformation("Le processus est déjà arrêté");
            return true;
        }

        try
        {
            _logger.LogInformation("Arrêt du processus enfant PID {Pid} (SIGTERM)", 
                _childProcess.Id);

            // Envoyer SIGTERM (arrêt gracieux)
            await ExecuteCommandAsync("kill", $"-TERM {_childProcess.Id}");

            // Attendre que le processus se termine
            var exitTask = _childProcess.WaitForExitAsync(_shutdownCts.Token);
            var delayTask = Task.Delay(timeoutMs);

            var completedTask = await Task.WhenAny(exitTask, delayTask);

            if (completedTask == exitTask)
            {
                _logger.LogInformation("Processus arrêté proprement");
                return true;
            }

            // Si le timeout est écoulé, envoyer SIGKILL
            _logger.LogWarning("Timeout lors de l'arrêt gracieux, envoi de SIGKILL");
            await ExecuteCommandAsync("kill", $"-KILL {_childProcess.Id}");

            await Task.Delay(500);
            
            if (_childProcess.HasExited)
            {
                _logger.LogInformation("Processus tué avec SIGKILL");
                return true;
            }

            _logger.LogError("Impossible de tuer le processus");
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de l'arrêt du processus");
            return false;
        }
    }

    /// <summary>
    /// Lit les flux de sortie du processus enfant.
    /// </summary>
    private async Task ReadStreamAsync(StreamReader reader, string streamName)
    {
        try
        {
            string? line;
            while ((line = await reader.ReadLineAsync()) != null)
            {
                if (!string.IsNullOrWhiteSpace(line))
                {
                    _logger.LogInformation("[{StreamName}] {Line}", streamName, line);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la lecture du flux {StreamName}", streamName);
        }
    }

    /// <summary>
    /// Callback appelé quand le processus enfant se termine.
    /// </summary>
    private void OnProcessExited(object? sender, EventArgs e)
    {
        if (_childProcess != null)
        {
            _logger.LogInformation("Processus enfant terminé avec code de sortie {ExitCode}",
                _childProcess.ExitCode);

            _exitTcs?.TrySetResult(_childProcess.ExitCode);
        }
    }

    /// <summary>
    /// Exécute une commande système.
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

            await process.WaitForExitAsync();
            return process.ExitCode == 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de l'exécution de '{Command}'", command);
            return false;
        }
    }

    public async ValueTask DisposeAsync()
    {
        await StopProcessAsync();
        _childProcess?.Dispose();
        _shutdownCts?.Dispose();
    }
}
