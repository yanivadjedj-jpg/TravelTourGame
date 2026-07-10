# Travel Tour — Jeu C# avec MonoGame

## Prérequis
- .NET 8 SDK : https://dotnet.microsoft.com/download
- MonoGame : installé automatiquement via NuGet

## Installation en 3 étapes

### 1. Installer .NET 8
```bash
# Ubuntu/Debian
sudo apt install dotnet-sdk-8.0

# Ou télécharger sur https://dotnet.microsoft.com/download
```

### 2. Installer les outils MonoGame
```bash
dotnet tool install --global dotnet-mgcb
dotnet tool install --global dotnet-mgcb-editor
```

### 3. Compiler et lancer
```bash
cd /home/rebootconseil/TravelTourGame
dotnet restore
dotnet run
```

## Commandes de jeu

### Menu principal
- **Clic** sur un des 6 événements pour y accéder
- **ÉCHAP** pour quitter

### Combat (Donjons)
| Touche | Action |
|--------|--------|
| A / D  | Se déplacer |
| W / Espace | Sauter (double saut) |
| Shift | Dash (invincible) |
| Z | Attaque légère (combo) |
| X | Attaque lourde |
| Q | Capacité spéciale 1 |
| E | Capacité spéciale 2 |
| ÉCHAP | Retour menu |

### Crosspark (Moto)
| Touche | Action |
|--------|--------|
| A / D | Accélérer |
| W / Espace | Sauter sur une rampe |
| S | Backflip |
| Q | Coffin Flip |
| E | No Hands |
| ÉCHAP | Retour menu |

### Histoire
| Touche | Action |
|--------|--------|
| ← → | Changer de chapitre |
| Espace | Accélérer le texte |

## Structure du projet
```
TravelTourGame/
├── TravelTourGame.cs      ← Classe principale du jeu
├── Program.cs             ← Point d'entrée
├── Core/
│   └── GameData.cs        ← Données, enums, catalog, PlayerSave
├── States/
│   ├── IGameState.cs      ← Interface de base
│   ├── MainMenuState.cs   ← Menu avec étoiles animées
│   ├── CombatState.cs     ← Donjon avec vagues d'ennemis
│   ├── CrossparkState.cs  ← Jeu de moto avec tricks
│   ├── TeamState.cs       ← Sélection d'équipe
│   ├── BoutiqueState.cs   ← Shop + améliorations
│   ├── TrainingState.cs   ← Liste des donjons
│   ├── StoryState.cs      ← Histoire avec effet machine à écrire
│   └── BackgroundState.cs ← Sélecteur de fond
├── Entities/
│   ├── Player.cs          ← Physique, combat, capacités
│   └── Enemy.cs           ← IA patrouille/poursuite/attaque
└── UI/
    ├── UIHelper.cs        ← Fonctions de dessin réutilisables
    └── Button.cs          ← Bouton interactif
```

## Fonctionnalités implémentées
- ✅ **Menu principal** animé (étoiles, nébuleuses, pulsation du titre)
- ✅ **Crosspark** — physique moto, 5 tricks, score combo, rampes
- ✅ **My Team** — sélection 3 personnages parmi 8 (Jimmy, Kaito, Ryo, Sakura...)
- ✅ **Boutique** — achat et amélioration armes/skins/véhicules/capacités
- ✅ **Entraînement** — 6 donjons, vagues d'ennemis, boss, matériaux
- ✅ **Histoire** — 4 actes (Solo Leveling + One Piece + Naruto + OPM) + effet machine à écrire
- ✅ **Arrière Plan** — 8 fonds thématiques avec aperçu en temps réel
- ✅ **Combat 2D** — double saut, dash, combos, 5 capacités spéciales
- ✅ **IA ennemis** — 3 états (patrouille/poursuite/attaque)
- ✅ **Système de progression** — or, matériaux, niveaux d'équipement
