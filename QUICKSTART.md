# VirtualIPHost - Quick Start Guide 🚀

## 📁 Structure du Projet

```
VirtualIPHost/
├── Program.cs                      # Point d'entrée principal
├── ApplicationOrchestrator.cs       # Orchestrateur global
├── NetworkManager.cs               # Gestion des alias IP
├── ProcessManager.cs               # Gestion du processus enfant
├── IpValidator.cs                  # Validation IP et introspection réseau
├── IpValidator.Tests.cs            # Tests unitaires
├── VirtualIPHost.csproj            # Configuration du projet
├── examples.sh                     # Script d'exemples et outils
├── README.md                       # Documentation complète
├── ARCHITECTURE.md                 # Architecture détaillée
├── DEPLOYMENT.md                   # Guide de déploiement
└── QUICKSTART.md                   # Ce fichier
```

## ⚡ 5 Minutes pour Démarrer

### 1. Compiler l'Application
```bash
cd ~/VirtualIPHost
dotnet publish -c Release -r linux-x64 --self-contained=true

# Binaire : ./bin/Release/net8.0/linux-x64/publish/VirtualIPHost
```

### 2. Tester les Conditions Préalables
```bash
# Vérifier le système
./examples.sh test

# Afficher les interfaces
./examples.sh interfaces
```

### 3. Lancer un Serveur Exemple
```bash
# Serveur HTTP simple Python
sudo ./bin/Release/net8.0/linux-x64/publish/VirtualIPHost \
  --ip 192.168.1.99 \
  --command python3 \
  --args "-m http.server 8000"

# Dans un autre terminal, vérifier :
curl http://192.168.1.99:8000
```

### 4. Arrêter l'Application
```bash
# Appuyer sur Ctrl+C
# L'alias IP sera automatiquement supprimé
```

## 🎯 Cas d'Usage Courants

### Flask Application
```bash
sudo ./VirtualIPHost \
  --ip 192.168.1.99 \
  --command python3 \
  --args "app.py"
```

### Node.js Express
```bash
sudo ./VirtualIPHost \
  --ip 192.168.1.100 \
  --command node \
  --args "server.js"
```

### PHP Built-in Server
```bash
sudo ./VirtualIPHost \
  --ip 192.168.1.101 \
  --command php \
  --args "-S 0.0.0.0:80"
```

### Bash Script
```bash
sudo ./VirtualIPHost \
  --ip 192.168.1.102 \
  --command bash \
  --args "script.sh"
```

## 🔍 Vérification Rapide

### Vérifier que l'alias IP existe
```bash
ip addr show eth0 | grep 192.168.1.99
```

### Tester la connectivité
```bash
ping -c 1 192.168.1.99
```

### Voir les processus en écoute
```bash
ss -tulnp | grep 192.168.1.99
```

## 🛠️ Installation Système

### Mode 1 : Avec sudo (Simple)
```bash
sudo ./bin/Release/net8.0/linux-x64/publish/VirtualIPHost ...
```

### Mode 2 : Avec CAP_NET_ADMIN (Recommandé)
```bash
sudo setcap cap_net_admin=ep ./bin/Release/net8.0/linux-x64/publish/VirtualIPHost
./bin/Release/net8.0/linux-x64/publish/VirtualIPHost ...
```

### Mode 3 : Installer Globalement
```bash
sudo cp ./bin/Release/net8.0/linux-x64/publish/VirtualIPHost /usr/local/bin/
sudo setcap cap_net_admin=ep /usr/local/bin/VirtualIPHost
VirtualIPHost --ip 192.168.1.99 --command python3 --args "app.py"
```

## 📚 Documentation

| Document | Contenu |
|----------|---------|
| **README.md** | Documentation complète, cas d'usage, troubleshooting |
| **ARCHITECTURE.md** | Architecture technique, diagrammes, flux d'exécution |
| **DEPLOYMENT.md** | Guide complet de déploiement en production |
| **QUICKSTART.md** | Ce guide rapide |

## ⚠️ Points Importants

1. **Privilèges Administratifs Requis**
   - L'application doit être exécutée avec sudo ou avoir CAP_NET_ADMIN

2. **Système d'Exploitation**
   - Linux uniquement (Ubuntu, Debian, Fedora, etc.)
   - Noyau 4.0+

3. **Dépendances**
   - .NET 8 Runtime (ou SDK pour compiler)
   - iproute2 (généralement pré-installé)
   - iputils-ping (pour tests)

4. **Port Réservé**
   - Si vous utilisez des ports < 1024 (80, 443), besoin de privilèges root

## 🚨 Troubleshooting Rapide

### "Permission denied"
→ Utiliser `sudo` ou attribuer CAP_NET_ADMIN

### "Address already in use"
→ Vérifier avec `ss -tulnp` et nettoyer les alias orphelins

### "Interface not found"
→ Vérifier avec `ip addr show` ou `ifconfig`

### Service ne démarre pas
→ Vérifier les logs avec `sudo journalctl -u virtualiphost -f`

## 📞 Ressources Rapides

```bash
# Afficher l'aide
VirtualIPHost --help

# Voir les exemples
./examples.sh

# Tester le système
./examples.sh test

# Nettoyer les alias orphelins
./examples.sh cleanup

# Compiler
./examples.sh compile
```

## 🔐 Checklist de Sécurité

- [ ] Application compilée en Release (`-c Release`)
- [ ] Binaire en lecture seule (`chmod 755`)
- [ ] Propriétaire : root (`sudo chown root:root`)
- [ ] CAP_NET_ADMIN uniquement (pas root complet)
- [ ] Logs centralisés et archivés
- [ ] Firewall configuré pour les ports utilisés
- [ ] Monitoring en place

## 📊 Commandes Utiles

```bash
# Compiler uniquement
dotnet build -c Release

# Compiler et publier (auto-contenu)
dotnet publish -c Release -r linux-x64 --self-contained=true

# Exécuter les tests
dotnet test

# Nettoyer les artefacts
dotnet clean

# Voir les dépendances
dotnet list package
```

## 🎓 Prochaines Étapes

1. **Lire README.md** pour la documentation complète
2. **Consulter ARCHITECTURE.md** pour comprendre la conception
3. **Suivre DEPLOYMENT.md** pour la mise en production
4. **Configurer systemd** pour l'exécution automatique

## 🆘 Besoin d'Aide ?

1. Vérifier les logs : `sudo journalctl -u virtualiphost -f`
2. Consulter le troubleshooting dans README.md
3. Vérifier les conditions préalables : `./examples.sh test`
4. Lancer manuellement avec logs détaillés

---

**VirtualIPHost** - Exécution de processus avec alias IP pour Linux 🐧

Pour plus d'informations : Consultez README.md
