# VirtualIPHost - Index Complet du Projet

## 📦 Vue d'Ensemble

**VirtualIPHost** est une application C# .NET complète permettant d'exécuter des processus tiers (Python Flask, Node.js, etc.) en les associant à des adresses IP virtuelles pour contourner les conflits de ports sur le réseau local.

**Version** : 1.0.0  
**Framework** : .NET 8  
**Plateforme** : Linux  
**Licence** : MIT  

---

## 📂 Fichiers du Projet

### 🔧 Code Source (C#)

| Fichier | Ligne | Rôle | Import |
|---------|-------|------|--------|
| **Program.cs** | 180 | Point d'entrée, parsing CLI, gestion des signaux | ⭐⭐⭐ |
| **ApplicationOrchestrator.cs** | 220 | Orchestration complète du cycle de vie | ⭐⭐⭐ |
| **NetworkManager.cs** | 160 | Gestion des alias IP système | ⭐⭐⭐ |
| **ProcessManager.cs** | 210 | Gestion du processus enfant | ⭐⭐⭐ |
| **IpValidator.cs** | 90 | Validation et introspection réseau | ⭐⭐ |
| **IpValidator.Tests.cs** | 130 | Tests unitaires et d'intégration | ⭐⭐ |

### 📄 Configuration

| Fichier | Rôle |
|---------|------|
| **VirtualIPHost.csproj** | Configuration du projet .NET |

### 📚 Documentation

| Fichier | Contenu | Audience |
|---------|---------|----------|
| **README.md** (11 KB) | Documentation complète, cas d'usage, troubleshooting | Tous |
| **ARCHITECTURE.md** (15 KB) | Conception technique, diagrammes, flux | Développeurs |
| **DEPLOYMENT.md** (11 KB) | Guide complet de déploiement en production | DevOps/SysAdmins |
| **QUICKSTART.md** (5.6 KB) | Guide rapide de 5 minutes | Nouveaux utilisateurs |
| **INDEX.md** | Ce fichier, structure du projet | Navigation |

### 🛠️ Outils

| Fichier | Fonction |
|---------|----------|
| **examples.sh** (6.3 KB) | Script de compilation, test, déploiement |

---

## 🚀 Guide de Navigation

### Pour Commencer Rapidement
1. Commencer par : **QUICKSTART.md** (5 min)
2. Compiler : `./examples.sh compile`
3. Tester : `./examples.sh test`
4. Lancer : `sudo ./VirtualIPHost --ip 192.168.1.99 --command python3 --args "app.py"`

### Pour Comprendre la Conception
1. Lire : **ARCHITECTURE.md**
   - Vue d'ensemble
   - Structure des composants
   - Flux d'exécution détaillé
   - Modèles de données

### Pour Déployer en Production
1. Suivre : **DEPLOYMENT.md**
   - Compilation Release
   - Installation système
   - Configuration systemd
   - Sécurité et monitoring

### Pour Utiliser Complètement
1. Consulter : **README.md**
   - Toutes les fonctionnalités
   - Tous les cas d'usage
   - Troubleshooting détaillé
   - Configuration avancée

---

## 🏗️ Composants Principaux

### 1. **Program.cs** - Point d'Entrée
```
Responsabilités :
├─ Analyse des arguments CLI (System.CommandLine)
├─ Configuration du logging
├─ Enregistrement des handlers de signaux (SIGTERM, SIGINT)
├─ Création de l'orchestrateur
└─ Gestion du cycle complet
```

### 2. **ApplicationOrchestrator.cs** - Orchestration Globale
```
Responsabilités :
├─ Validation des conditions préalables
│  ├─ Système = Linux
│  ├─ Privilèges = root/CAP_NET_ADMIN
│  ├─ IP valide
│  └─ Interface détectée
├─ Création NetworkManager
├─ Création ProcessManager
├─ Coordination de l'exécution
└─ Nettoyage propre à l'arrêt
```

### 3. **NetworkManager.cs** - Gestion Réseau
```
Responsabilités :
├─ AddIPAliasAsync()
│  └─ Exécute : ip addr add 192.168.1.99/32 dev eth0
├─ RemoveIPAliasAsync()
│  └─ Exécute : ip addr del 192.168.1.99/32 dev eth0
├─ ConfigureNetworkRoutingAsync()
│  └─ Configuration avancée du routage
└─ ExecuteCommandAsync()
   └─ Utilitaire générique d'exécution
```

### 4. **ProcessManager.cs** - Gestion du Processus
```
Responsabilités :
├─ StartProcessAsync()
│  ├─ Création ProcessStartInfo
│  ├─ Redirection I/O
│  └─ Redirection des flux
├─ WaitForExitAsync()
│  └─ TaskCompletionSource<int>
├─ StopProcessAsync()
│  ├─ SIGTERM (gracieux)
│  ├─ SIGKILL (forcé)
│  └─ Timeout management
└─ ReadStreamAsync()
   └─ Lecture stdout/stderr
```

### 5. **IpValidator.cs** - Validation & Introspection
```
Responsabilités :
├─ IsValidIPv4(string ip)
│  └─ Validation format IPv4
├─ GetActiveNetworkInterface()
│  └─ Détection interface eth0/enp0s
├─ IsIPLocallyConfigured(ip)
│  └─ Détection conflits d'IP
└─ FormatIPWithCIDR(ip, cidr)
   └─ Formatting "ip/cidr"
```

---

## 💻 Utilisation Rapide

### Compilation
```bash
# Mode Release (optimisé, autonome)
dotnet publish -c Release -r linux-x64 --self-contained=true

# Binaire : ./bin/Release/net8.0/linux-x64/publish/VirtualIPHost
```

### Exécution Simple
```bash
# Serveur HTTP Python
sudo ./VirtualIPHost \
  --ip 192.168.1.99 \
  --command python3 \
  --args "-m http.server 8000"

# Dans un autre terminal :
curl http://192.168.1.99:8000
```

### Exécution Avancée
```bash
# Flask App
sudo ./VirtualIPHost \
  --ip 192.168.1.99 \
  --command python3 \
  --args "app.py"

# Node.js
sudo ./VirtualIPHost \
  --ip 192.168.1.100 \
  --command node \
  --args "server.js"

# PHP
sudo ./VirtualIPHost \
  --ip 192.168.1.101 \
  --command php \
  --args "-S 0.0.0.0:80"
```

---

## ✨ Fonctionnalités Clés

### ✅ Configuration Réseau Automatique
- Validation IPv4
- Détection interface active
- Création alias IP via `ip addr`
- Nettoyage automatique

### ✅ Exécution Robuste
- Lancement en tant que child process
- Redirection stdout/stderr
- Gestion codes de sortie
- Arrêt gracieux (SIGTERM/SIGKILL)

### ✅ Gestion Signaux
- SIGINT (Ctrl+C)
- SIGTERM (arrêt système)
- Cleanup automatique
- Timeout management

### ✅ Logging Détaillé
- Logs console structurés
- Intégration systemd
- Logs d'erreur et avertissements
- Debugging facilitée

---

## 🔒 Sécurité

### Validations
- ✓ Format IPv4 valide
- ✓ Système = Linux
- ✓ Privilèges administratifs requis
- ✓ Interface réseau existante

### Privilèges Minimaux
```bash
# Plutôt que full root :
sudo setcap cap_net_admin=ep /usr/local/bin/VirtualIPHost
```

### Nettoyage Garanti
- ✓ Alias IP supprimé même en erreur
- ✓ Processus tué en cas d'interruption
- ✓ Ressources libérées proprement

---

## 📊 Statistiques du Projet

| Métrique | Valeur |
|----------|--------|
| **Code Source** | ~1000 lignes C# |
| **Documentation** | ~50 KB (4 fichiers) |
| **Tests** | 130 lignes (unitaires) |
| **Dépendances** | 2 (System.CommandLine, logging) |
| **Temps de Build** | ~10 secondes |
| **Taille Binaire** | ~100 MB (self-contained), ~5 MB (sans) |

---

## 🧪 Tests

### Unitaires
```bash
# IpValidator
dotnet test

# Classes testées :
# - IpValidatorTests
# - CLIArgumentValidationTests
```

### Intégration (Requiert root)
```bash
# Tests réseau réels
# - NetworkManagerIntegrationTests
```

### Manuels Recommandés
```bash
# Test système
./examples.sh test

# Test simple
./examples.sh simple

# Test nettoyage
./examples.sh cleanup
```

---

## 🚀 Déploiement

### Mode Développement
```bash
# Compilation debug
dotnet build

# Exécution directe
sudo dotnet run --ip 192.168.1.99 --command python3 --args "app.py"
```

### Mode Production
```bash
# Voir DEPLOYMENT.md pour :
# - Compilation Release
# - Installation système
# - Configuration systemd
# - Service template
# - Docker deployment
# - Sécurité & monitoring
```

### Systemd Service
```ini
[Unit]
Description=VirtualIPHost
After=network.target

[Service]
Type=simple
User=root
ExecStart=/usr/local/bin/VirtualIPHost --ip 192.168.1.99 --command python3 --args "app.py"
Restart=on-failure

[Install]
WantedBy=multi-user.target
```

---

## 🆘 Support Rapide

### Problèmes Courants

| Problème | Solution |
|----------|----------|
| Permission denied | Utiliser `sudo` ou setcap CAP_NET_ADMIN |
| Address already in use | `ss -tulnp` pour identifier, `pkill` pour nettoyer |
| Interface not found | `ip addr show` ou `ifconfig` |
| Service doesn't start | `journalctl -u virtualiphost -f` |

### Commandes de Diagnostic
```bash
# Vérifier les alias IP
ip addr show

# Vérifier les processus
ps aux | grep VirtualIPHost

# Vérifier les ports
ss -tulnp | grep 192.168

# Voir les logs
sudo journalctl -u virtualiphost -f

# Nettoyer les orphelins
./examples.sh cleanup
```

---

## 📞 Ressources

### Documentation Interne
- **README.md** - Toute la documentation
- **ARCHITECTURE.md** - Technique détaillée
- **DEPLOYMENT.md** - Production deployment
- **QUICKSTART.md** - Guide 5 min

### Références Externes
- [.NET 8 Docs](https://learn.microsoft.com/dotnet/)
- [Linux IP Command](https://linux.die.net/man/8/ip)
- [C# Process Documentation](https://learn.microsoft.com/dotnet/api/system.diagnostics.process)
- [Systemd Service Files](https://www.freedesktop.org/software/systemd/man/systemd.service.html)

---

## 🎯 Roadmap Futures Améliorations

- [ ] Support IPv6
- [ ] Interface Web de gestion
- [ ] Configuration YAML
- [ ] Monitoring Prometheus
- [ ] Alertes automatiques
- [ ] Quotas ressources
- [ ] Clustering multi-hôte

---

## 📜 License

MIT License - Libre d'utilisation dans vos projets

---

## 👨‍💻 Auteur

**VirtualIPHost** - Gestionnaire d'alias IP pour processus Linux

Développé avec ❤️ en C# .NET

---

**Dernière mise à jour** : Septembre 2026  
**Version du projet** : 1.0.0  
**Status** : Production Ready ✓
