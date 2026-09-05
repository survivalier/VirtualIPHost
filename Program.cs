using System.CommandLine;
using System.CommandLine.Invocation;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using VirtualIPHost;

// Créer les options CLI
var ipOption = new Option<string>(
    aliases: new[] { "--ip", "-i" },
    description: "Adresse IP virtuelle à configurer (ex: 192.168.1.99)")
{
    IsRequired = true
};

var commandOption = new Option<string>(
    aliases: new[] { "--command", "-c" },
    description: "Commande à exécuter")
{
    IsRequired = true
};

var argsOption = new Option<string>(
    aliases: new[] { "--args", "-a" },
    description: "Arguments de la commande",
    getDefaultValue: () => "");

// Créer la commande racine
var rootCommand = new RootCommand("VirtualIPHost - Exécuteur de processus avec alias IP")
{
    ipOption,
    commandOption,
    argsOption
};

rootCommand.SetHandler(async (ip, command, args) =>
{
    await Main(ip, command, args);
}, ipOption, commandOption, argsOption);

await rootCommand.InvokeAsync(args);

async Task Main(string virtualIp, string command, string arguments)
{
    // Configuration du logging
    var loggerFactory = LoggerFactory.Create(builder =>
    {
        builder.AddConsole();
        builder.SetMinimumLevel(LogLevel.Information);
    });
    var logger = loggerFactory.CreateLogger("VirtualIPHost");

    logger.LogInformation("╔════════════════════════════════════════╗");
    logger.LogInformation("║   VirtualIPHost - v1.0.0              ║");
    logger.LogInformation("║   Exécuteur de processus avec alias IP ║");
    logger.LogInformation("╚════════════════════════════════════════╝\n");

    // Créer l'orchestrateur
    var orchestrator = new ApplicationOrchestrator(virtualIp, command, arguments, 
        loggerFactory.CreateLogger<ApplicationOrchestrator>());

    // Valider les conditions préalables
    if (!orchestrator.ValidatePrerequisites())
    {
        logger.LogError("Validation échouée. Sortie.");
        Environment.Exit(1);
        return;
    }

    // Configuration de la gestion des signaux d'arrêt
    var shutdownEvent = new ManualResetEventSlim(false);
    
    void HandleSignal(int signal)
    {
        logger.LogInformation("\nSignal {Signal} reçu, arrêt de l'application...", signal);
        shutdownEvent.Set();
    }

    // Enregistrer les handlers de signaux
    Console.CancelKeyPress += (sender, args) =>
    {
        args.Cancel = true;
        HandleSignal(2); // SIGINT
    };

    // Pour SIGTERM et autres signaux, on utilise PosixSignalRegistration
    if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
    {
        try
        {
            using var sigterm = PosixSignalRegistration.Create(PosixSignal.SIGTERM, _ => HandleSignal(15));
            using var sigint = PosixSignalRegistration.Create(PosixSignal.SIGINT, _ => HandleSignal(2));

            // Exécuter l'application
            var exitCode = await RunApplicationAsync(orchestrator, shutdownEvent, logger);
            Environment.Exit(exitCode);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erreur lors de l'enregistrement des signaux");
            Environment.Exit(1);
        }
    }
    else
    {
        // Fallback pour les systèmes non-Linux
        var exitCode = await RunApplicationAsync(orchestrator, shutdownEvent, logger);
        Environment.Exit(exitCode);
    }
}

async Task<int> RunApplicationAsync(ApplicationOrchestrator orchestrator, 
    ManualResetEventSlim shutdownEvent, ILogger logger)
{
    try
    {
        // Créer une tâche pour l'exécution de l'application
        var appTask = orchestrator.ExecuteAsync();

        // Créer une tâche pour attendre le signal d'arrêt
        var shutdownTask = Task.Run(() =>
        {
            shutdownEvent.Wait();
        });

        // Attendre soit la fin du processus, soit le signal d'arrêt
        var completedTask = await Task.WhenAny(appTask, shutdownTask);

        if (completedTask == shutdownTask)
        {
            // L'utilisateur a envoyé un signal d'arrêt
            logger.LogInformation("Arrêt initié par signal utilisateur");
            await orchestrator.ShutdownAsync();
            return 0;
        }
        else
        {
            // Le processus enfant s'est terminé naturellement
            var exitCode = await appTask;
            await orchestrator.ShutdownAsync();
            return exitCode;
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Erreur lors de l'exécution de l'application");
        await orchestrator.ShutdownAsync();
        return 1;
    }
}
