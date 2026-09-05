# VirtualIPHost - Exécuteur de Processus avec Alias IP

Une application C# .NET permettant d'exécuter des processus tiers (Python, Node.js, etc.) en les associant à une adresse IP virtuelle, évitant les conflits de ports sur le réseau local.

## 🎯 Cas d'Usage

Vous avez une machine physique avec l'IP `192.168.1.1` et souhaitez :
- Exécuter un serveur Flask sur le port 80
- L'exposer via une IP virtuelle dédiée `192.168.1.99`
- Permettre aux autres machines du réseau d'y accéder directement
- Sans interférer avec les services locaux de l'hôte

**Solution** : VirtualIPHost ajoute automatiquement l'alias IP, lance votre processus, puis nettoie tout proprement à l'arrêt.

## ✨ Fonctionnalités

### Interface CLI Intuitive
```bash
./app --ip 192.168.1.99 --command python --args "app.py"
# ou syntaxe courte :
./app -i 192.168.1.99 -c python -a "app.py"
```

### Configuration Réseau Automatique
- ✅ Validation de l'adresse IP
- ✅ Détection de l'interface réseau active
- ✅ Création d'alias IP via `ip addr add`
- ✅ Configuration du routage réseau
- ✅ Nettoyage complet à l'arrêt

### Gestion Robuste des Processus
- ✅ Lancement en tant que processus enfant
- ✅ Capture des flux stdout/stderr
- ✅ Arrêt gracieux (SIGTERM)
- ✅ Terminaison forcée (SIGKILL) si nécessaire
- ✅ Gestion des codes de sortie

### Capture des Signaux d'Arrêt
- ✅ SIGINT (Ctrl+C)
- ✅ SIGTERM (arrêt système)
- ✅ Nettoyage automatique (alias IP supprimé, processus tué)

## 🔧 Prérequis

### Système d'Exploitation
- **Linux** (Ubuntu 20.04+, Debian 11+, Fedora 34+, etc.)
- Noyau 4.0+ avec support des alias IP

### Dépendances Logicielles
```bash
# .NET 8 Runtime
sudo apt-get install -y dotnet-runtime-8.0

# Utilitaires réseau (généralement pré-installés)
sudo apt-get install -y iproute2 iputils-ping
```

### Permissions
L'application **DOIT être exécutée avec les privilèges administratifs** :
```bash
# Option 1 : Utiliser sudo
sudo ./VirtualIPHost --ip 192.168.1.99 --command python --args "app.py"

# Option 2 : Attribuer CAP_NET_ADMIN (plus sûr)
sudo setcap cap_net_admin=ep ./VirtualIPHost
./VirtualIPHost --ip 192.168.1.99 --command python --args "app.py"
```

## 🚀 Installation

### 1. Cloner ou télécharger le projet
```bash
cd ~/projects
git clone <repo-url> VirtualIPHost
cd VirtualIPHost
```

### 2. Compiler l'application
```bash
# Compilation en mode Release
dotnet publish -c Release -r linux-x64 --self-contained=true

# Binaire généré à : bin/Release/net8.0/linux-x64/publish/VirtualIPHost
```

### 3. Rendre exécutable
```bash
chmod +x ./bin/Release/net8.0/linux-x64/publish/VirtualIPHost

# (Optionnel) Créer un lien symbolique
sudo ln -s $(pwd)/bin/Release/net8.0/linux-x64/publish/VirtualIPHost /usr/local/bin/
```

## 📖 Utilisation

### Syntaxe de Base
```bash
sudo ./VirtualIPHost [OPTIONS]
```

### Options Disponibles
| Option | Alias | Description | Exemple |
|--------|-------|-------------|---------|
| `--ip` | `-i` | IP virtuelle à configurer | `192.168.1.99` |
| `--command` | `-c` | Commande à exécuter | `python` |
| `--args` | `-a` | Arguments de la commande | `app.py` |

### Exemples Pratiques

#### 1. Serveur Flask Python
```bash
sudo ./VirtualIPHost \
  --ip 192.168.1.99 \
  --command python \
  --args "app.py"
```

#### 2. Serveur Node.js Express
```bash
sudo ./VirtualIPHost \
  --ip 192.168.1.100 \
  --command node \
  --args "server.js"
```

#### 3. Serveur HTTP simple Python
```bash
sudo ./VirtualIPHost \
  --ip 192.168.1.101 \
  --command python \
  --args "-m http.server 8000"
```

#### 4. Script PHP (avec PHP CLI)
```bash
sudo ./VirtualIPHost \
  --ip 192.168.1.102 \
  --command php \
  --args "-S 0.0.0.0:80"
```

## 🔍 Surveillance et Débogage

### Affichage Détaillé
L'application enregistre toutes les opérations :
```
╔════════════════════════════════════════╗
║   VirtualIPHost - v1.0.0              ║
║   Exécuteur de processus avec alias IP ║
╚════════════════════════════════════════╝

=== Validation des conditions préalables ===
✓ Système d'exploitation: Linux détecté
✓ Privilèges administratifs détectés
✓ Adresse IP valide: 192.168.1.99
✓ Interface réseau active détectée: eth0
✓ Commande disponible: python

=== Configuration réseau ===
Ajout de l'alias IP 192.168.1.99 sur l'interface eth0
Alias IP 192.168.1.99 ajouté avec succès

=== Démarrage du processus enfant ===
Lancement du processus: python app.py
Adresse IP configurée: 192.168.1.99
Processus enfant démarré avec PID 12345
```

### Vérifier l'Alias IP
```bash
# Pendant l'exécution
ip addr show eth0

# Exemple de sortie :
# 2: eth0: <BROADCAST,MULTICAST,UP,LOWER_UP> mtu 1500
#     inet 192.168.1.1/24 brd 192.168.1.255 scope global eth0
#     inet 192.168.1.99/32 scope global eth0

# Tester la connectivité
ping -c 1 192.168.1.99

# Vérifier l'écoute sur les ports
ss -tulnp | grep 80
```

### Logs en Temps Réel
Tous les logs sont envoyés à la console :
- Les informations en bleu/blanc
- Les avertissements en jaune
- Les erreurs en rouge

## 🛑 Arrêt de l'Application

### Arrêt Gracieux (Recommandé)
```bash
# Appuyer sur Ctrl+C
# L'application :
# 1. Envoie SIGTERM au processus enfant
# 2. Attend 5 secondes max
# 3. Envoie SIGKILL si nécessaire
# 4. Supprime l'alias IP
# 5. Quitte
```

### Arrêt Forcé
```bash
# Si Ctrl+C ne fonctionne pas
kill -TERM <pid>

# Ou tuer immédiatement
kill -KILL <pid>
```

### Nettoyage Manuel (en cas de problème)
```bash
# Lister les alias IP
ip addr show

# Supprimer un alias
sudo ip addr del 192.168.1.99/32 dev eth0

# Tuer les processus restants
sudo pkill -f "python app.py"
```

## 🏗️ Architecture

### Composants Principaux

```
Program.cs
    └─> CLI Parser (System.CommandLine)
            └─> ApplicationOrchestrator
                    ├─> NetworkManager (gestion des alias IP)
                    │   ├─> IpValidator
                    │   └─> Exécution de commandes système
                    │
                    └─> ProcessManager (gestion du processus enfant)
                        ├─> Lancement du processus
                        ├─> Capture des flux I/O
                        └─> Gestion de l'arrêt gracieux
```

### Flux d'Exécution

```
1. Validation des arguments CLI
   ↓
2. Validation des conditions préalables
   • Système = Linux
   • Privilèges = root/CAP_NET_ADMIN
   • IP valide
   • Interface détectée
   ↓
3. Configuration réseau
   • Ajout de l'alias IP
   • Configuration du routage
   ↓
4. Lancement du processus
   • Création du ProcessStartInfo
   • Start() du processus
   • Redirection des flux I/O
   ↓
5. Attente + Capture des signaux
   • SIGINT/SIGTERM → arrêt gracieux
   • Processus se termine → nettoyage
   ↓
6. Nettoyage
   • Arrêt du processus (SIGTERM → SIGKILL)
   • Suppression de l'alias IP
   • Fermeture des ressources
```

## 🔐 Sécurité

### Points d'Attention

1. **Privilèges Minimaux**
   ```bash
   # Préférer CAP_NET_ADMIN plutôt que root
   sudo setcap cap_net_admin=ep /usr/local/bin/VirtualIPHost
   sudo chown root:root /usr/local/bin/VirtualIPHost
   sudo chmod 755 /usr/local/bin/VirtualIPHost
   ```

2. **Validation des Entrées**
   - IP : vérification du format IPv4
   - Commande : vérification d'existence dans PATH
   - Arguments : transmis directement (no shell injection par défaut)

3. **Isolation Réseau**
   - L'alias IP est limité à /32 (hôte unique)
   - Aucune modification des routes par défaut
   - Le processus hérite des restrictions du parent

4. **Nettoyage Automatique**
   - Alias IP supprimé même en cas d'erreur
   - Processus tué en cas d'interruption
   - Gestion des signaux correctement implémentée

## 🐛 Troubleshooting

### Erreur : "Cette application ne fonctionne que sur Linux"
- L'app n'est compilée que pour Linux
- Si vous êtes sous WSL, utilisez une vraie distribution Linux

### Erreur : "Doit être exécutée avec les privilèges administratifs"
- Utiliser `sudo ./VirtualIPHost ...`
- Ou attribuer `CAP_NET_ADMIN` avec `setcap`

### Erreur : "Address already in use"
```bash
# Vérifier les processus écoutant le port
sudo ss -tulnp | grep :80

# Nettoyer les alias IP orphelins
sudo ip addr del 192.168.1.99/32 dev eth0
```

### L'alias IP n'est pas supprimé après arrêt
```bash
# Vérifier quels alias restent
ip addr show

# Nettoyer manuellement
sudo ip addr del 192.168.1.99/32 dev eth0
```

### Le processus enfant ne s'arrête pas
```bash
# Vérifier le PID
ps aux | grep python

# Tuer manuellement
sudo kill -KILL <pid>
```

## 📊 Cas d'Usage Avancés

### Déploiement Multi-Services
```bash
# Terminal 1
sudo ./VirtualIPHost -i 192.168.1.99 -c python -a "app1.py"

# Terminal 2
sudo ./VirtualIPHost -i 192.168.1.100 -c python -a "app2.py"

# Terminal 3
sudo ./VirtualIPHost -i 192.168.1.101 -c node -a "server.js"
```

### Intégration Systemd
```ini
# /etc/systemd/system/virtualiphost.service
[Unit]
Description=VirtualIPHost - Flask App
After=network.target

[Service]
Type=simple
User=root
WorkingDirectory=/opt/myapp
ExecStart=/usr/local/bin/VirtualIPHost \
  --ip 192.168.1.99 \
  --command python \
  --args "app.py"
Restart=on-failure
RestartSec=10

[Install]
WantedBy=multi-user.target
```

Activation :
```bash
sudo systemctl daemon-reload
sudo systemctl enable virtualiphost
sudo systemctl start virtualiphost
sudo systemctl status virtualiphost
```

### Container Docker (pour développement)
```dockerfile
FROM mcr.microsoft.com/dotnet/runtime:8.0

WORKDIR /app
COPY bin/Release/net8.0/linux-x64/publish/ .

# Donner CAP_NET_ADMIN au conteneur
# docker run --cap-add=NET_ADMIN ...

ENTRYPOINT ["./VirtualIPHost"]
```

## 🤝 Contribution

Les contributions sont bienvenues ! Domaines d'amélioration possibles :
- Support IPv6
- Interface Web de gestion
- Authentification/autorisation
- Metriques et monitoring
- Configuration par fichier YAML

## 📝 License

MIT License - Libre d'utilisation dans vos projets

## 📞 Support

Pour les problèmes ou questions :
1. Vérifier les logs en sortie standard
2. Consulter le troubleshooting ci-dessus
3. Ouvrir une issue avec logs complets

---

**VirtualIPHost** - Simplifiez la gestion des alias IP et des processus 🚀
