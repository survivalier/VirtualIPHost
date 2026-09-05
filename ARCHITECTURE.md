# VirtualIPHost - Architecture Détaillée

## 📐 Vue d'Ensemble

```
┌─────────────────────────────────────────────────────────────────┐
│                    VirtualIPHost Application                     │
├─────────────────────────────────────────────────────────────────┤
│                                                                   │
│  ┌──────────────────────────────────────────────────────────┐   │
│  │                 Program.cs (Entry Point)                 │   │
│  │  • Analyse les arguments CLI                             │   │
│  │  • Enregistre les handlers de signaux                    │   │
│  │  • Lance l'orchestrateur                                 │   │
│  └──────────────────────────────────────────────────────────┘   │
│                             ▼                                     │
│  ┌──────────────────────────────────────────────────────────┐   │
│  │           ApplicationOrchestrator                         │   │
│  │  • Coordonne le flux d'exécution                         │   │
│  │  • Valide les conditions préalables                      │   │
│  │  • Gère le cycle de vie complet                          │   │
│  └──────────────────────────────────────────────────────────┘   │
│           ▼                                       ▼               │
│  ┌─────────────────────┐      ┌──────────────────────────────┐  │
│  │ NetworkManager      │      │  ProcessManager              │  │
│  │ • Alias IP          │      │  • Lancement du processus    │  │
│  │ • Routage           │      │  • Capture I/O               │  │
│  │ • Nettoyage         │      │  • Arrêt gracieux            │  │
│  └─────────────────────┘      └──────────────────────────────┘  │
│        ▼                              ▼                          │
│  ┌─────────────────────┐      ┌──────────────────────────────┐  │
│  │ IpValidator         │      │  Processus Enfant            │  │
│  │ • Validation IPv4   │      │  • Python/Node/PHP/etc       │  │
│  │ • Interface active  │      │  • Lié à l'IP virtuelle      │  │
│  │ • Vérifications     │      │  • Flux de sortie redirigé   │  │
│  └─────────────────────┘      └──────────────────────────────┘  │
└─────────────────────────────────────────────────────────────────┘
```

## 🔄 Flux d'Exécution Détaillé

### Phase 1 : Initialisation et Validation

```
[Démarrage]
    ▼
[Parsing CLI] ← System.CommandLine
    ├─ Extraction du --ip
    ├─ Extraction du --command
    └─ Extraction du --args
    ▼
[Validation des conditions préalables]
    ├─ IsRunningAsRoot()
    │  └─ Vérification UID == 0 ou User == "root"
    │
    ├─ IsValidIPv4(virtualIp)
    │  └─ IPAddress.TryParse() + AddressFamily check
    │
    ├─ GetActiveNetworkInterface()
    │  ├─ NetworkInterface.GetAllNetworkInterfaces()
    │  ├─ Filtre : OperationalStatus == Up
    │  ├─ Exclusion : lo*, docker*, veth*
    │  └─ Retour : eth0, enp0s... (priorité Ethernet)
    │
    ├─ CommandExists(command)
    │  └─ Exécution : which <command>
    │
    └─ IsIPLocallyConfigured(virtualIp)
       └─ Comparaison avec les addresses unicast existantes
    ▼
[Décision : Continuer ou Arrêter]
```

### Phase 2 : Configuration Réseau

```
[NetworkManager.AddIPAliasAsync()]
    ▼
[Validation de l'IP]
    ├─ Format valide ? ✓
    └─ Interface détectée ? ✓
    ▼
[Vérification de conflit]
    ├─ IsIPLocallyConfigured(virtualIp) ?
    └─ Si oui → Log Warning, continuer quand même
    ▼
[Exécution de la commande système]
    │
    └─ ip addr add 192.168.1.99/32 dev eth0
       ├─ ProcessStartInfo
       │  ├─ FileName = "ip"
       │  ├─ Arguments = "addr add ..."
       │  ├─ UseShellExecute = false
       │  └─ RedirectStandardError = true
       │
       ├─ Process.Start()
       ├─ Await WaitForExitAsync()
       │
       ├─ ExitCode == 0 ? ✓
       │  └─ _aliasAdded = true
       │
       └─ Verification locale
          └─ IsIPLocallyConfigured(virtualIp) ? ✓
    ▼
[ConfigureNetworkRoutingAsync()]
    └─ Attente + Vérification d'accessibilité
    ▼
[Fin de configuration réseau]
```

### Phase 3 : Lancement du Processus

```
[ProcessManager.StartProcessAsync()]
    ▼
[Création du ProcessStartInfo]
    ├─ FileName = _command (ex: "python")
    ├─ Arguments = _arguments (ex: "app.py")
    ├─ UseShellExecute = false
    ├─ RedirectStandardOutput = true
    ├─ RedirectStandardError = true
    └─ Environment["VIRTUAL_IP"] = _virtualIp
    ▼
[Abonnement aux événements]
    ├─ EnableRaisingEvents = true
    └─ Exited += OnProcessExited
    ▼
[Lancement]
    ├─ Process.Start()
    └─ Log: "PID xxx démarré"
    ▼
[Gestion des flux I/O en arrière-plan]
    ├─ ReadStreamAsync(StandardOutput)
    │  └─ Task.Run() → lecture ligne par ligne
    │
    └─ ReadStreamAsync(StandardError)
       └─ Task.Run() → lecture ligne par ligne
    ▼
[Attente de fin]
    └─ _exitTcs.Task (créée dans StartProcessAsync)
```

### Phase 4 : Surveillance et Gestion des Signaux

```
[Configuration des handlers de signaux]
    ├─ Console.CancelKeyPress
    │  └─ Ctrl+C → HandleSignal(SIGINT)
    │
    ├─ PosixSignalRegistration.Create(SIGTERM)
    │  └─ Signal système → HandleSignal(SIGTERM)
    │
    └─ PosixSignalRegistration.Create(SIGINT)
       └─ Redondance SIGINT
    ▼
[Attente en parallèle]
    ├─ Task 1 : ProcessManager.WaitForExitAsync()
    │  └─ Attendez la fin naturelle du processus enfant
    │
    └─ Task 2 : shutdownEvent.Wait()
       └─ Attendez un signal de l'utilisateur
    ▼
[Déterminer qui a fini en premier]
    ├─ Si Task 1 complétée : Processus s'est terminé naturellement
    │  └─ Récupérer ExitCode
    │
    └─ Si Task 2 complétée : Signal d'arrêt reçu
       └─ Déclencher StopProcessAsync()
```

### Phase 5 : Arrêt et Nettoyage

```
[ProcessManager.StopProcessAsync()]
    ▼
[Vérifier l'état du processus]
    ├─ HasExited ? → Retour true
    └─ En cours ? → Continuer
    ▼
[Tentative d'arrêt gracieux (SIGTERM)]
    │
    ├─ ExecuteCommand("kill", "-TERM <pid>")
    │
    ├─ Attendre jusqu'à timeout (5000ms par défaut)
    │
    ├─ Si le processus s'arrête : ✓ Retour true
    │
    └─ Si timeout écoulé : Continuer
    ▼
[Arrêt forcé (SIGKILL)]
    │
    ├─ ExecuteCommand("kill", "-KILL <pid>")
    │
    └─ Attendre 500ms
    ▼
[NetworkManager.RemoveIPAliasAsync()]
    │
    ├─ Vérifier que l'alias a été ajouté (_aliasAdded == true)
    │
    └─ ip addr del 192.168.1.99/32 dev eth0
       ├─ ProcessStartInfo
       ├─ Process.Start()
       ├─ Await WaitForExitAsync()
       ├─ ExitCode == 0 ? ✓
       └─ _aliasAdded = false
    ▼
[Fermeture des ressources]
    ├─ _childProcess?.Dispose()
    ├─ _shutdownCts?.Dispose()
    └─ _networkManager?.Dispose()
    ▼
[Fin propre]
```

## 🏛️ Structure des Composants

### 1. Program.cs
**Responsabilité** : Point d'entrée et orchestration CLI

```csharp
Rôle              | Détail
─────────────────────────────────────────────────────────────
Arguments CLI     | Parsing avec System.CommandLine
Logging           | Configuration ILoggerFactory
Signaux Linux     | PosixSignalRegistration
Coordination      | Exécution et nettoyage
```

### 2. IpValidator.cs
**Responsabilité** : Validation et introspection réseau

```csharp
Méthode                       | Sortie
──────────────────────────────────────────────────────────────
IsValidIPv4(string ip)        | bool
FormatIPWithCIDR(ip, cidr)    | string "ip/cidr"
GetActiveNetworkInterface()   | string "eth0"
IsIPLocallyConfigured(ip)     | bool
```

### 3. NetworkManager.cs
**Responsabilité** : Gestion des alias IP au niveau système

```csharp
Méthode                         | Privilèges | Système
──────────────────────────────────────────────────────────────
AddIPAliasAsync()               | CAP_NET_ADMIN | ip addr add
RemoveIPAliasAsync()            | CAP_NET_ADMIN | ip addr del
ConfigureNetworkRoutingAsync()  | CAP_NET_ADMIN | Configuration
ExecuteCommandAsync()           | Var         | Process générique
```

### 4. ProcessManager.cs
**Responsabilité** : Gestion du cycle de vie du processus enfant

```csharp
Méthode                    | Async | Description
───────────────────────────────────────────────────────────────
StartProcessAsync()        | ✓     | Lancer le processus
WaitForExitAsync()         | ✓     | Attendre l'arrêt
StopProcessAsync(timeout)  | ✓     | Arrêt SIGTERM/SIGKILL
ReadStreamAsync()          | ✓     | Redirection I/O
OnProcessExited()          | ✗     | Callback Exited
```

### 5. ApplicationOrchestrator.cs
**Responsabilité** : Coordination globale

```csharp
Méthode                     | Description
────────────────────────────────────────────────────────────────
ValidatePrerequisites()    | Vérifications pré-exécution
ExecuteAsync()             | Lancer complet
ShutdownAsync()            | Arrêt propre
DisposeAsync()             | Libération des ressources
```

## 🔐 Modèle de Privilèges

### Opérations Requérant root/CAP_NET_ADMIN

```
Operation                         | Commande       | User Required
──────────────────────────────────────────────────────────────────
Ajouter alias IP                  | ip addr add    | root
Supprimer alias IP                | ip addr del    | root
Configurer routage                | ip route       | root
Lancer processus (basique)        | Process.Start  | Non (héréité)
Envoyer signaux                   | kill -TERM     | root (même PID)
```

### Modes d'Exécution

**Mode 1 : Via sudo (Courant)**
```bash
sudo ./VirtualIPHost --ip 192.168.1.99 --command python --args "app.py"
```
- ✓ Simple
- ✗ Partage les privilèges root avec le processus enfant
- ✓ Fonctionne partout

**Mode 2 : Via setcap (Recommandé)**
```bash
sudo setcap cap_net_admin=ep /usr/local/bin/VirtualIPHost
./VirtualIPHost --ip 192.168.1.99 --command python --args "app.py"
```
- ✓ Moins de privilèges (CAP_NET_ADMIN uniquement)
- ✓ Pas besoin de sudo
- ✗ Légèrement plus complexe à mettre en place

## 📊 Modèle de Données

### Configuration Runtime

```csharp
class ExecutionContext
{
    string VirtualIp           // 192.168.1.99
    string Command             // python
    string Arguments           // app.py
    string? NetworkInterface   // eth0
    int ProcessId             // PID du processus enfant
    bool AliasAdded           // État de l'alias IP
    CancellationToken         // Token d'arrêt
}
```

### État des Signaux

```csharp
enum SignalState
{
    Running,
    SigTermReceived,
    SigKillRequired,
    ProcessExited,
    Shutdown
}
```

## 🧪 Points de Test Critiques

### Tests Unitaires Recommandés

1. **IpValidator**
   - ✓ IPv4 valides vs invalides
   - ✓ Extraction d'interface active
   - ✓ Détection de conflit d'IP

2. **ProcessManager**
   - ✓ Lancement et capture I/O
   - ✓ Arrêt gracieux avec timeout
   - ✓ Redirection des signaux

3. **NetworkManager** (Requiert root)
   - ✓ Ajout/suppression d'alias
   - ✓ Nettoyage en cas d'erreur
   - ✓ Gestion des doublons

### Tests d'Intégration

```bash
# Test 1 : Serveur HTTP simple
sudo ./VirtualIPHost \
  --ip 192.168.1.99 \
  --command python3 \
  --args "-m http.server 80"

# Test 2 : Vérifier depuis autre machine
ping 192.168.1.99
curl http://192.168.1.99

# Test 3 : Arrêt et vérification du nettoyage
# (Appuyer sur Ctrl+C, vérifier alias supprimé)
ip addr show eth0 # L'IP 192.168.1.99 ne doit plus apparaître
```

## 🔧 Configuration Système Requise

### Fichier kernel.conf pour permettre alias IP sans limite

```bash
# /etc/sysctl.conf (optionnel, normal par défaut)
net.ipv4.conf.all.promote_secondaries = 1

# Appliquer :
sudo sysctl -p
```

### Permissions de fichier

```bash
# VirtualIPHost binary
-rwxr-xr-x root:root /usr/local/bin/VirtualIPHost

# Ou avec CAP_NET_ADMIN
-rwxr-xr-x root:root /usr/local/bin/VirtualIPHost
# + cap_net_admin=ep
```

## 🚀 Chemins d'Optimisation Future

1. **IPv6 Support**
   - Ajouter IsValidIPv6()
   - Gérer CIDR /128 pour IPv6

2. **Interface Web**
   - API REST pour lancer/arrêter processes
   - Dashboard temps réel

3. **Persistance**
   - Config YAML pour présets
   - Historique des exécutions

4. **Monitoring**
   - Métriques Prometheus
   - Logs structurés JSON

5. **Sécurité Avancée**
   - Authentification pour l'API
   - Sandboxing du processus enfant
   - Quotas de ressources (CPU, RAM)

---

**VirtualIPHost Architecture** - Conception robuste et maintenable
