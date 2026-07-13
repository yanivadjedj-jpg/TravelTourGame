using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace TravelTour.Core
{
    public enum IslandTheme { Hunter, Pirate, Ninja }

    public class MobSpawn
    {
        public string  EnemyName = "";
        public string  SpriteKey = "";
        public Vector2 Position;
        public bool    IsBoss;
    }

    public class IslandData
    {
        public string      Name = "", Icon = "";
        public IslandTheme Theme;
        public string       VisualDesc = "";
        public string       BgSpriteKey = "";
        public string[]     MobSpriteKeys = System.Array.Empty<string>();
        public string       EliteMobSpriteKey = "";
        public int          RequiredRank  = 0;
        public int          RequiredLevel = 0;
        public List<MobSpawn> MobSpawns = new();
        public List<string>   QuestIds   = new();     // référence QuestData.Name
        public bool            HasFishingSpot;
        public Vector2          FishingSpotPosition;
        public Vector2          DockPoint;              // position mer -> point d'arrivée à pied
        public Vector2          SeaPosition;             // position du dock sur la carte WorldSea
        public string?          NativeFruitName;         // fruit natif, drop sur l'élite de l'île
        public string           LinkedDungeonName = "";   // DungeonData jumelle pour le calcul de combat
    }

    public static class WorldEventCatalog
    {
        public static List<IslandData> Islands = new()
        {
            // ── Chasseur (Solo Leveling) ─────────────────────────
            new IslandData {
                Name = "Île de la Porte Écarlate", Icon = "🔴", Theme = IslandTheme.Hunter,
                VisualDesc = "Faille dimensionnelle rouge fissurée au-dessus de plaines de cendres.",
                BgSpriteKey = "island_hunter1_bg",
                MobSpriteKeys = new[] { "island_hunter1_mob_golem", "island_hunter1_mob_chasseur" },
                RequiredRank = 0, RequiredLevel = 1,
                MobSpawns = new() {
                    new MobSpawn{ EnemyName="Golem des Failles",         SpriteKey="island_hunter1_mob_golem",    Position=new(400,500) },
                    new MobSpawn{ EnemyName="Chasseur Rang E Corrompu",  SpriteKey="island_hunter1_mob_chasseur", Position=new(700,500) },
                },
                QuestIds = new() { "Nettoyeur de Faille", "Éclaireur de la Porte" },
                SeaPosition = new(150, 150), DockPoint = new(200, 500),
                LinkedDungeonName = "Faille du Chasseur E",
            },
            new IslandData {
                Name = "Sanctuaire du Chasseur Gris", Icon = "🗝️", Theme = IslandTheme.Hunter,
                VisualDesc = "Sanctuaire de guilde en ruine, brume grise, statues brisées.",
                BgSpriteKey = "island_hunter2_bg",
                MobSpriteKeys = new[] { "island_hunter2_mob_spectre", "island_hunter2_mob_ombre" },
                RequiredRank = 2, RequiredLevel = 20,
                MobSpawns = new() {
                    new MobSpawn{ EnemyName="Spectre du Contrat", SpriteKey="island_hunter2_mob_spectre", Position=new(400,500) },
                    new MobSpawn{ EnemyName="Ombre Récoltée",     SpriteKey="island_hunter2_mob_ombre",   Position=new(700,500) },
                },
                QuestIds = new() { "Purge du Sanctuaire", "Le Pacte Brisé" },
                HasFishingSpot = true, FishingSpotPosition = new(900, 520),
                SeaPosition = new(250, 220), DockPoint = new(200, 500),
                LinkedDungeonName = "Sanctuaire Gris",
            },
            new IslandData {
                Name = "Abîme du Monarque Silencieux", Icon = "👁️", Theme = IslandTheme.Hunter,
                VisualDesc = "Trône obsidienne au bord d'un abîme violet fissuré.",
                BgSpriteKey = "island_hunter3_bg",
                MobSpriteKeys = new[] { "island_hunter3_mob_ombre" },
                EliteMobSpriteKey = "island_hunter3_boss",
                RequiredRank = 4, RequiredLevel = 45,
                MobSpawns = new() {
                    new MobSpawn{ EnemyName="Gardien du Trône Muet", SpriteKey="island_hunter3_boss", Position=new(600,500), IsBoss=true },
                },
                QuestIds = new() { "Le Gardien Muet", "Éclat du Monarque" },
                NativeFruitName = "Fruit de l'Ombre Monarque",
                SeaPosition = new(320, 300), DockPoint = new(200, 500),
                LinkedDungeonName = "Abîme du Monarque",
            },

            // ── Pirate (One Piece) ────────────────────────────────
            new IslandData {
                Name = "Île du Grand Récif", Icon = "🏴‍☠️", Theme = IslandTheme.Pirate,
                VisualDesc = "Crique tropicale, galions échoués sur le récif corallien.",
                BgSpriteKey = "island_pirate1_bg",
                MobSpriteKeys = new[] { "island_pirate1_mob_requin", "island_pirate1_mob_canonnier" },
                RequiredRank = 0, RequiredLevel = 1,
                MobSpawns = new() {
                    new MobSpawn{ EnemyName="Moussaillon Requin",         SpriteKey="island_pirate1_mob_requin",    Position=new(400,500) },
                    new MobSpawn{ EnemyName="Canonnier Barbe-de-Corail",  SpriteKey="island_pirate1_mob_canonnier", Position=new(700,500) },
                },
                QuestIds = new() { "Pilleur de Récif", "Rencontre au Port" },
                HasFishingSpot = true, FishingSpotPosition = new(950, 500),
                SeaPosition = new(600, 150), DockPoint = new(200, 500),
                LinkedDungeonName = "Récif Pirate",
            },
            new IslandData {
                Name = "Archipel des Tempêtes", Icon = "🌊", Theme = IslandTheme.Pirate,
                VisualDesc = "Falaises fouettées par l'orage, éclairs sur mer noire.",
                BgSpriteKey = "island_pirate2_bg",
                MobSpriteKeys = new[] { "island_pirate2_mob_capitaine", "island_pirate2_mob_harponneur" },
                RequiredRank = 2, RequiredLevel = 20,
                MobSpawns = new() {
                    new MobSpawn{ EnemyName="Capitaine Vague Noire", SpriteKey="island_pirate2_mob_capitaine", Position=new(400,500) },
                    new MobSpawn{ EnemyName="Harponneur des Abysses",SpriteKey="island_pirate2_mob_harponneur",Position=new(700,500) },
                },
                QuestIds = new() { "Chasse à la Tempête", "Le Journal du Naufragé" },
                HasFishingSpot = true, FishingSpotPosition = new(950, 500),
                SeaPosition = new(700, 220), DockPoint = new(200, 500),
                LinkedDungeonName = "Archipel Tempête",
            },
            new IslandData {
                Name = "Baie du Roi Naufragé", Icon = "⚓", Theme = IslandTheme.Pirate,
                VisualDesc = "Cimetière de galions royaux engloutis, brouillard doré.",
                BgSpriteKey = "island_pirate3_bg",
                MobSpriteKeys = new[] { "island_pirate3_mob_pirate" },
                EliteMobSpriteKey = "island_pirate3_boss",
                RequiredRank = 4, RequiredLevel = 45,
                MobSpawns = new() {
                    new MobSpawn{ EnemyName="Amiral Fantôme du Naufrage", SpriteKey="island_pirate3_boss", Position=new(600,500), IsBoss=true },
                },
                QuestIds = new() { "L'Amiral Fantôme", "Trésor du Roi Englouti" },
                NativeFruitName = "Fruit du Roi des Mers",
                HasFishingSpot = true, FishingSpotPosition = new(950, 500),
                SeaPosition = new(780, 300), DockPoint = new(200, 500),
                LinkedDungeonName = "Baie du Roi Naufragé",
            },

            // ── Ninja (Naruto) ────────────────────────────────────
            new IslandData {
                Name = "Village de la Feuille Grise", Icon = "🍃", Theme = IslandTheme.Ninja,
                VisualDesc = "Village forestier, toits de bois, bandeaux frontaux gravés.",
                BgSpriteKey = "island_ninja1_bg",
                MobSpriteKeys = new[] { "island_ninja1_mob_genin", "island_ninja1_mob_chien" },
                RequiredRank = 0, RequiredLevel = 1,
                MobSpawns = new() {
                    new MobSpawn{ EnemyName="Genin Renégat",           SpriteKey="island_ninja1_mob_genin", Position=new(400,500) },
                    new MobSpawn{ EnemyName="Chien-Ninja Invocateur",  SpriteKey="island_ninja1_mob_chien", Position=new(700,500) },
                },
                QuestIds = new() { "Chasse aux Renégats", "Le Message du Village" },
                SeaPosition = new(1050, 150), DockPoint = new(200, 500),
                LinkedDungeonName = "Feuille Grise",
            },
            new IslandData {
                Name = "Vallée de la Brume Rouge", Icon = "🌫️", Theme = IslandTheme.Ninja,
                VisualDesc = "Gorge embrumée aux feuilles rouges, ruines d'un village caché.",
                BgSpriteKey = "island_ninja2_bg",
                MobSpriteKeys = new[] { "island_ninja2_mob_jonin", "island_ninja2_mob_marionnette" },
                RequiredRank = 2, RequiredLevel = 20,
                MobSpawns = new() {
                    new MobSpawn{ EnemyName="Jonin Masqué du Brouillard", SpriteKey="island_ninja2_mob_jonin",       Position=new(400,500) },
                    new MobSpawn{ EnemyName="Marionnette Interdite",       SpriteKey="island_ninja2_mob_marionnette", Position=new(700,500) },
                },
                QuestIds = new() { "Brouillard Interdit", "Pêcheur du Village" },
                HasFishingSpot = true, FishingSpotPosition = new(950, 500),
                SeaPosition = new(1150, 220), DockPoint = new(200, 500),
                LinkedDungeonName = "Brume Rouge",
            },
            new IslandData {
                Name = "Temple du Sceau Ancestral", Icon = "⛩️", Theme = IslandTheme.Ninja,
                VisualDesc = "Temple de montagne, gravures de sceau luisant d'un bleu chakra.",
                BgSpriteKey = "island_ninja3_bg",
                MobSpriteKeys = new[] { "island_ninja3_mob_gardien" },
                EliteMobSpriteKey = "island_ninja3_boss",
                RequiredRank = 4, RequiredLevel = 45,
                MobSpawns = new() {
                    new MobSpawn{ EnemyName="Gardien du Sceau Interdit", SpriteKey="island_ninja3_boss", Position=new(600,500), IsBoss=true },
                },
                QuestIds = new() { "Le Sceau Interdit", "Chakra du Temple" },
                NativeFruitName = "Fruit du Sage des Six Voies",
                SeaPosition = new(1250, 300), DockPoint = new(200, 500),
                LinkedDungeonName = "Sceau Ancestral",
            },
        };

        public static IslandData? Find(string name) => Islands.Find(i => i.Name == name);
    }
}
