# Makefile pour VirtualIPHost
# Wrapper autour des commandes dotnet décrites dans README.md / QUICKSTART.md

PROJECT      := VirtualIPHost.csproj
CONFIG       := Release
RUNTIME      := linux-x64
PUBLISH_DIR  := bin/$(CONFIG)/net10.0/$(RUNTIME)/publish
BIN_NAME     := VirtualIPHost

# Paramètres par défaut pour `make run` (surchargeables : make run IP=... CMD=... ARGS=...)
IP  ?= 192.168.1.99
CMD ?= python3
ARGS ?=

.PHONY: all restore build publish run test clean list-packages install uninstall

## Cible par défaut : restaure puis compile en Release
all: build

## Restaure les dépendances NuGet
restore:
	dotnet restore $(PROJECT)

## Compile le projet en configuration Release
build: restore
	dotnet build $(PROJECT) -c $(CONFIG)

## Publie un exécutable autonome (self-contained) pour linux-x64
publish: restore
	dotnet publish $(PROJECT) -c $(CONFIG) -r $(RUNTIME) --self-contained=true

## Lance l'application (nécessite sudo pour la gestion des IP/réseau)
run: build
	sudo dotnet run --project $(PROJECT) -c $(CONFIG) -- --ip $(IP) --command $(CMD) --args "$(ARGS)"

## Exécute les tests (si un projet de tests est présent)
test:
	dotnet test

## Liste les paquets NuGet référencés
list-packages:
	dotnet list $(PROJECT) package

## Nettoie les artefacts de build (bin/, obj/)
clean:
	dotnet clean $(PROJECT)
	rm -rf bin obj

## Installe l'application dans /usr/local/lib/VirtualIPHost et crée un lien dans /usr/local/bin
install: publish
	sudo mkdir -p /usr/local/lib/$(BIN_NAME)
	sudo cp -r $(PUBLISH_DIR)/. /usr/local/lib/$(BIN_NAME)/
	sudo chmod 755 /usr/local/lib/$(BIN_NAME)/$(BIN_NAME)
	sudo ln -sf /usr/local/lib/$(BIN_NAME)/$(BIN_NAME) /usr/local/bin/$(BIN_NAME)

## Désinstalle l'application
uninstall:
	sudo rm -f /usr/local/bin/$(BIN_NAME)
	sudo rm -rf /usr/local/lib/$(BIN_NAME)
