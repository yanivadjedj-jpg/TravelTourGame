using System.Collections.Generic;

namespace TravelTour.Core
{
    // ── Enums ──────────────────────────────────────────────
    public enum Rarity      { Common, Rare, Epic, Legendary, Mythical }
    public enum GameState   { MainMenu, Crosspark, Team, Boutique, Training, Story, Background, Combat, Tutorial, Wallet, Fruits, Inventory, Quest, Artifact, WorldSea, WorldIsland, Fishing }
    public enum AbilityType { DomainMonarque, FruitGolem, RasenganDimensionnel, FrappeSérieuse, HakiRois }
    public enum WeaponType  { Sword, Shield, Bow, Staff, Gauntlet, Scythe }
    public enum ArtifactEffect { HpBoost, AtkBoost, DefBoost, SpeedBoost, XpBoost, GoldBoost, CooldownReduce, FruitDmgBoost, SwordDmgBoost, MeleeDmgBoost }
    public enum ArtifactSlot   { Chapeau, Amulette, Bague, Cape }
    public enum QuestObjectiveType { KillEnemies, KillBosses, CompleteDungeons, EarnGold, ReachLevel, ReachRank, OwnWeapons, OwnFruits, DoCombo, ExploreIsland, CatchFish, TalkToNpc, DefeatAbsolu, KillMages }
    public enum DifficultyLevel { Easy, Medium, Hard, Boss, Legendary }
    public enum FruitType   { Naturel, Élémentaire, Bête }   // Paramecia / Logia / Zoan

    // ── Fruit system ───────────────────────────────────────────
    public class FruitMove
    {
        public string  Name;
        public string  Desc;
        public float   Damage;
        public float   ChakraCost;
        public float   Cooldown;
        public int     MasteryReq; // 0, 100, 200, 400
        public string  Key;        // "Z","X","C","F"
        public string  Icon;
    }

    public class FruitData
    {
        public string    Name;
        public string    Icon;
        public FruitType Type;
        public Rarity    Rarity;
        public string    Description;
        public int       Mastery;       // 0-600
        public bool      IsOwned;
        public bool      IsEquipped;
        public int       BuyPrice;
        public FruitMove[] Moves;       // 4 moves

        // M1 amélioré par le fruit
        public float  M1Multiplier = 1f;   // bonus multiplicateur sur attaque légère
        public string M1Icon       = "";   // icône affichée sur le coup M1

        // Mastery thresholds
        public static readonly int[] MasteryLevels = { 0, 50, 120, 250 };

        // ── Transformation (maîtrise 600 requise, touche V) ──
        public bool   CanTransform      = false;
        public string TransformAuraKey  = "";     // clé sprite (ex: "fx_aura_magma")
        public float  TransformAtkMult   = 1f;
        public float  TransformSpeedMult = 1f;

        public string GetRarityLabel() => Rarity switch {
            Rarity.Common    => "COMMUN",
            Rarity.Rare      => "RARE",
            Rarity.Epic      => "ÉPIQUE",
            Rarity.Legendary => "LÉGENDAIRE",
            Rarity.Mythical  => "MYTHIQUE",
            _                => "?"
        };

        public int UnlockedMoves()
        {
            int count = 0;
            foreach (var m in Moves) if (Mastery >= m.MasteryReq) count++;
            return count;
        }

        public float MasteryPct() => System.Math.Clamp(Mastery / 600f, 0f, 1f);
    }

    // ── Data classes ───────────────────────────────────────
    public class CharacterData
    {
        public string Name;
        public Rarity Rarity;
        public float  MaxHP, BaseAtk, BaseDef, BaseSpeed, MaxChakra;
        public int    Level = 1, MaxLevel = 10;
        public bool   IsOwned;
        public int    BuyPrice;
        public string Icon; // emoji or sprite key
        public UpgradeCost[]? UpgradeCosts;

        public float ScaledHP()  => MaxHP   + (Level - 1) * 30f;
        public float ScaledAtk() => BaseAtk + (Level - 1) * 5f;
        public float ScaledDef() => BaseDef + (Level - 1) * 3f;

        // ── Maîtrise du personnage (0-600, gagnée en éliminant des ennemis) ──
        public int Mastery = 0;
        public static readonly int[] MasteryLevels = { 0, 100, 250, 450 };

        public string MasteryTier() => Mastery >= 450 ? "Platine" : Mastery >= 250 ? "Or" : Mastery >= 100 ? "Argent" : "Bronze";
        public float  MasteryAtkMult() => 1f + (Mastery >= 450 ? 0.30f : Mastery >= 250 ? 0.18f : Mastery >= 100 ? 0.08f : 0f);
        public float  MasteryPct() => System.Math.Clamp(Mastery / 600f, 0f, 1f);
        public bool   MasteryUltimateUnlocked => Mastery >= 450;
        public string MasteryUltimateName => $"Frappe Ultime de {Name}";
    }

    public class WeaponData
    {
        public string     Name, Icon;
        public WeaponType Type;
        public float      BaseDamage;
        public Rarity     Rarity;
        public int        Level = 1, MaxLevel = 10;
        public bool       IsOwned;
        public bool       IsEquipped;
        public int        BuyPrice;
        public List<UpgradeCost> Costs = new();

        public float GetDamage() => BaseDamage + (Level - 1) * 8f;
    }

    public class AbilityData
    {
        public string      Name, Icon, Description;
        public AbilityType Type;
        public float       Damage, ChakraCost, Cooldown, Duration;
        public bool        IsOwned;
        public int         BuyPrice;
    }

    public class VehicleData
    {
        public string Name, Icon;
        public float  Speed, Acceleration, TrickBonus;
        public Rarity Rarity;
        public int    Level = 1, MaxLevel = 10;
        public bool   IsOwned;
        public bool   IsEquipped;
        public int    BuyPrice;

        public float GetSpeed() => Speed + (Level - 1) * 1.5f;
    }

    public class ClassData
    {
        public string   Name;
        public string   Icon;
        public string   Color;
        public string   Description;
        public string[] Abilities;
    }

    public class DungeonData
    {
        public string          Name, Icon;
        public DifficultyLevel Difficulty;
        public int             RequiredRank;
        public int             RequiredLevel  = 0;    // 0 = pas de condition de niveau
        public bool            IsClassDungeon = false;
        public bool            BossGauntlet   = false; // dernier niveau d'acte : 4 vagues de boss
        public int             StoryActIndex  = -1;  // -1 = pas histoire, 0-4 = Acte 1-5
        public int             EnemyCount;
        public int             GoldReward;
        public List<MaterialReward> Rewards = new();
    }

    public class UpgradeCost
    {
        public string Material;
        public int    Quantity;
    }

    public class MaterialReward
    {
        public string Material;
        public int Min, Max;
    }

    public class TrickData
    {
        public string Name;
        public int    BaseScore;
        public float  Difficulty;
    }

    public class QuestReward
    {
        public string RewardType = "gold";  // "gold", "material", "xp"
        public string Key        = "";
        public int    Amount;
    }

    public class QuestData
    {
        public string              Name, Description, Icon, Category;
        public QuestObjectiveType  Objective;
        public int                 Target;
        public bool                IsCompleted;
        public bool                RewardClaimed;
        public QuestReward[]       Rewards = System.Array.Empty<QuestReward>();

        public int GetProgress() => Objective switch {
            QuestObjectiveType.KillEnemies      => PlayerSave.EnemiesKilled,
            QuestObjectiveType.KillBosses       => PlayerSave.BossesDefeated,
            QuestObjectiveType.CompleteDungeons => PlayerSave.DungeonsCompleted,
            QuestObjectiveType.EarnGold         => PlayerSave.Gold,
            QuestObjectiveType.ReachLevel       => PlayerSave.PlayerLevel,
            QuestObjectiveType.ReachRank        => PlayerSave.Rank,
            QuestObjectiveType.OwnWeapons       => Catalog.Weapons.FindAll(w => w.IsOwned).Count,
            QuestObjectiveType.OwnFruits        => Catalog.Fruits.FindAll(f => f.IsOwned).Count,
            QuestObjectiveType.DoCombo          => PlayerSave.MaxComboReached,
            QuestObjectiveType.ExploreIsland    => PlayerSave.VisitedIslands.Count,
            QuestObjectiveType.CatchFish        => PlayerSave.FishCaught,
            QuestObjectiveType.TalkToNpc        => PlayerSave.NpcsMet.Count,
            QuestObjectiveType.DefeatAbsolu      => PlayerSave.AbsoluDefeated ? 1 : 0,
            QuestObjectiveType.KillMages         => PlayerSave.MagesKilled,
            _                                   => 0
        };

        public float ProgressPct() => System.Math.Clamp((float)GetProgress() / System.Math.Max(1, Target), 0f, 1f);

        public bool CheckCompleted()
        {
            if (IsCompleted) return true;
            if (GetProgress() >= Target) { IsCompleted = true; SaveSystem.Save(); return true; }
            return false;
        }
    }

    public class ArtifactData
    {
        public string         Name, Icon, Description;
        public Rarity         Rarity;
        public ArtifactSlot   Slot;
        public ArtifactEffect Effect;
        public float          Value;      // % bonus ex: 0.20 = +20%
        public bool           IsOwned;
        public bool           IsEquipped;
        public int            BuyPrice;

        public string SlotLabel() => Slot switch {
            ArtifactSlot.Chapeau  => "🎩 Chapeau",
            ArtifactSlot.Amulette => "📿 Amulette",
            ArtifactSlot.Bague    => "💍 Bague",
            ArtifactSlot.Cape     => "🧣 Cape",
            _                     => "❓"
        };

        public string EffectLabel() => Effect switch {
            ArtifactEffect.HpBoost         => $"+{Value*100:F0}% HP max",
            ArtifactEffect.AtkBoost        => $"+{Value*100:F0}% ATK",
            ArtifactEffect.DefBoost        => $"+{Value*100:F0}% DEF",
            ArtifactEffect.SpeedBoost      => $"+{Value*100:F0}% Vitesse",
            ArtifactEffect.XpBoost         => $"+{Value*100:F0}% XP",
            ArtifactEffect.GoldBoost       => $"+{Value*100:F0}% Or",
            ArtifactEffect.CooldownReduce  => $"-{Value*100:F0}% Recharge",
            ArtifactEffect.FruitDmgBoost   => $"+{Value*100:F0}% Dégâts Fruit",
            ArtifactEffect.SwordDmgBoost   => $"+{Value*100:F0}% Dégâts Épée",
            ArtifactEffect.MeleeDmgBoost   => $"+{Value*100:F0}% Mêlée",
            _                              => ""
        };

        public float Multiplier() => 1f + Value;
    }

    // ── Material metadata ──────────────────────────────────
    public static class MaterialInfo
    {
        public static readonly Dictionary<string, (string Icon, string Label, string Desc, Rarity Rarity)> Data = new()
        {
            ["CristalFeu"]    = ("🔴", "Cristal de Feu",      "Fragment cristallisé des volcans dimensionnels.",           Rarity.Common),
            ["EssenceOmbres"] = ("🌑", "Essence des Ombres",  "Extrait de l'obscurité pure des donjons profonds.",         Rarity.Rare),
            ["LarmePhoenix"]  = ("🔥", "Larme du Phénix",     "Larme brûlante d'un phénix vaincu en combat.",             Rarity.Rare),
            ["PierreCeleste"] = ("⭐", "Pierre Céleste",       "Minéral tombé des cieux lors des tempêtes dimensionnelles.",Rarity.Epic),
            ["EclatFoudre"]   = ("⚡", "Éclat de Foudre",     "Résidu de chakra électrique condensé.",                    Rarity.Common),
            ["GemmeLunaire"]  = ("🌙", "Gemme Lunaire",        "Gemme qui absorbe la lumière de la lune de chaque monde.", Rarity.Rare),
            ["CristalNoir"]   = ("💎", "Cristal Noir",         "Cristal instable né du vide entre les dimensions.",        Rarity.Epic),
            ["AmeDechue"]     = ("💀", "Âme Déchue",           "Essence d'un boss vaincu, chargée de puissance brute.",    Rarity.Legendary),
        };

        public static string GetIcon(string mat) => Data.TryGetValue(mat, out var d) ? d.Icon : "❓";
        public static string GetLabel(string mat) => Data.TryGetValue(mat, out var d) ? d.Label : mat;

        // Prix de vente par matériau
        public static int SellPrice(string mat)
        {
            if (!Data.TryGetValue(mat, out var d)) return 10;
            return d.Rarity switch {
                Rarity.Common    => 50,
                Rarity.Rare      => 150,
                Rarity.Epic      => 400,
                Rarity.Legendary => 1000,
                _                => 10,
            };
        }
    }

    // ── Singleton save/runtime store ───────────────────────
    public static class PlayerSave
    {
        public static int    Gold = 500;
        // ── Niveau (1-200) ────────────────────────────────────
        public static int    PlayerLevel = 1;
        public static int    LevelXp     = 0;
        public const  int    MaxLevel    = 200;
        public static string[] RankNames = { "E","D","C","B","A","S","SS" };

        // ── Classe du joueur ──────────────────────────────────
        public static string PlayerClassName   = "";   // vide = pas de classe
        public static string PlayerClassIcon   = "";
        public static bool   ClassDungeonDone  = false;

        // ── Stats Blox Fruits style ───────────────────────────
        public static int StatMelee   = 0;  // Corps-à-corps
        public static int StatDefense = 0;  // Défense
        public static int StatSword   = 0;  // Épée
        public static int StatFruit   = 0;  // Fruit
        public static int StatSpeed   = 0;  // Vitesse
        public static int FreeStatPoints = 0;  // Points à distribuer

        const int STAT_POINTS_PER_LEVEL = 3;
        public const int MAX_STAT = 200;   // max par catégorie

        // Bonus appliqués en combat
        public static float MeleeDmgBonus()  => 1f + StatMelee   * 0.005f;  // +0.5% par point
        public static float DefenseBonus()   => 1f + StatDefense * 0.004f;  // +0.4% HP/DEF
        public static float SwordDmgBonus()  => 1f + StatSword   * 0.006f;  // +0.6% arme
        public static float FruitDmgBonus()  => 1f + StatFruit   * 0.007f;  // +0.7% fruit
        public static float SpeedBonus()     => 1f + StatSpeed   * 0.003f;  // +0.3% vitesse

        public static bool CanAllocate(int stat) => FreeStatPoints > 0 && stat < MAX_STAT;
        public static void AllocMelee()   { if (!CanAllocate(StatMelee))   return; FreeStatPoints--; StatMelee++;   SaveSystem.Save(); }
        public static void AllocDefense() { if (!CanAllocate(StatDefense)) return; FreeStatPoints--; StatDefense++; SaveSystem.Save(); }
        public static void AllocSword()   { if (!CanAllocate(StatSword))   return; FreeStatPoints--; StatSword++;   SaveSystem.Save(); }
        public static void AllocFruit()   { if (!CanAllocate(StatFruit))   return; FreeStatPoints--; StatFruit++;   SaveSystem.Save(); }
        public static void AllocSpeed()   { if (!CanAllocate(StatSpeed))   return; FreeStatPoints--; StatSpeed++;   SaveSystem.Save(); }
        public static int  TotalStats()   => StatMelee + StatDefense + StatSword + StatFruit + StatSpeed;

        // XP requis pour passer au niveau suivant (croît avec le niveau)
        public static int XpToNextLevel() =>
            PlayerLevel >= MaxLevel ? 999999 :
            PlayerLevel < 20  ? 60  + PlayerLevel * 20 :
            PlayerLevel < 50  ? 200 + PlayerLevel * 40 :
            PlayerLevel < 100 ? 500 + PlayerLevel * 80 :
                                1200 + PlayerLevel * 120;

        public static float LevelProgressPct()
        {
            int needed = XpToNextLevel();
            return System.Math.Clamp((float)LevelXp / System.Math.Max(1, needed), 0f, 1f);
        }

        // Rang basé sur le niveau (seuils personnalisés)
        public static int Rank =>
            PlayerLevel >= 150 ? 6 :   // SS
            PlayerLevel >= 95  ? 5 :   // S
            PlayerLevel >= 65  ? 4 :   // A
            PlayerLevel >= 40  ? 3 :   // B
            PlayerLevel >= 20  ? 2 :   // C
            PlayerLevel >= 5   ? 1 :   // D
            0;                          // E

        // Bonus de stats par niveau : +8 HP, +2 ATK, +1 DEF
        public static float LevelHpBonus()  => (PlayerLevel - 1) * 8f;
        public static float LevelAtkBonus() => (PlayerLevel - 1) * 2f;
        public static float LevelDefBonus() => (PlayerLevel - 1) * 1f;

        // Ajoute de l'XP — retourne true si level up
        public static bool AddXp(int amount)
        {
            if (PlayerLevel >= MaxLevel) return false;
            LevelXp += amount;
            bool leveled = false;
            while (PlayerLevel < MaxLevel && LevelXp >= XpToNextLevel())
            {
                LevelXp -= XpToNextLevel();
                PlayerLevel++;
                FreeStatPoints += STAT_POINTS_PER_LEVEL;
                leveled = true;
                Popups.Enqueue($"⬆ NIVEAU {PlayerLevel} ! +{STAT_POINTS_PER_LEVEL} points de stats à distribuer !");
                if ((PlayerLevel - 1) % 8 == 0)
                    Popups.Enqueue($"RANG {RankNames[Rank]} ATTEINT !");
            }
            SaveSystem.Save();
            return leveled;
        }

        // Compat ancienne API
        public static int   Xp { get => LevelXp; set => LevelXp = value; }
        public static string SelectedBackground = "Cosmos";

        // ── Fruit système ─────────────────────────────────────
        public static string? EquippedFruitName = null;
        public static FruitData? GetEquippedFruit() =>
            EquippedFruitName == null ? null :
            Catalog.Fruits.Find(f => f.Name == EquippedFruitName);

        public static void EquipFruit(string name)
        {
            // Unequip all
            foreach (var f in Catalog.Fruits) f.IsEquipped = false;
            EquippedFruitName = name;
            var fr = Catalog.Fruits.Find(f => f.Name == name);
            if (fr != null) fr.IsEquipped = true;
            Popups.Enqueue($"🍎 {name} équipé !");
            SaveSystem.Save();
        }

        public static void UnequipFruit()
        {
            foreach (var f in Catalog.Fruits) f.IsEquipped = false;
            EquippedFruitName = null;
            Popups.Enqueue("Fruit retiré.");
            SaveSystem.Save();
        }

        // ── Arme système ──────────────────────────────────────
        public static string? EquippedWeaponName = null;
        public static WeaponData? GetEquippedWeapon() =>
            EquippedWeaponName == null ? null :
            Catalog.Weapons.Find(w => w.Name == EquippedWeaponName);

        public static void EquipWeapon(string name)
        {
            foreach (var w in Catalog.Weapons) w.IsEquipped = false;
            EquippedWeaponName = name;
            var wp = Catalog.Weapons.Find(w => w.Name == name);
            if (wp != null) wp.IsEquipped = true;
            Popups.Enqueue($"⚔️ {name} équipée !");
            SaveSystem.Save();
        }

        public static void UnequipWeapon()
        {
            foreach (var w in Catalog.Weapons) w.IsEquipped = false;
            EquippedWeaponName = null;
            Popups.Enqueue("Arme retirée.");
            SaveSystem.Save();
        }

        // ── Véhicule système ───────────────────────────────────
        public static string? EquippedVehicleName = null;
        public static VehicleData? GetEquippedVehicle() =>
            EquippedVehicleName == null ? null :
            Catalog.Vehicles.Find(v => v.Name == EquippedVehicleName);

        public static void EquipVehicle(string name)
        {
            foreach (var v in Catalog.Vehicles) v.IsEquipped = false;
            EquippedVehicleName = name;
            var vh = Catalog.Vehicles.Find(v => v.Name == name);
            if (vh != null) vh.IsEquipped = true;
            Popups.Enqueue($"🚗 {name} équipé !");
            SaveSystem.Save();
        }

        public static void UnequipVehicle()
        {
            foreach (var v in Catalog.Vehicles) v.IsEquipped = false;
            EquippedVehicleName = null;
            Popups.Enqueue("Véhicule retiré.");
            SaveSystem.Save();
        }

        public static void AddFruitMastery(string fruitName, int amount)
        {
            var f = Catalog.Fruits.Find(fr => fr.Name == fruitName);
            if (f == null) return;
            int old = f.Mastery;
            f.Mastery = System.Math.Min(600, f.Mastery + amount);
            // Notify on milestone
            foreach (var lvl in FruitData.MasteryLevels)
                if (old < lvl && f.Mastery >= lvl && lvl > 0)
                    Popups.Enqueue($"🍎 {f.Name} — Maîtrise {lvl} !");
            SaveSystem.Save();
        }

        public static void AddCharacterMastery(string charName, int amount)
        {
            var c = Catalog.Characters.Find(ch => ch.Name == charName);
            if (c == null) return;
            int old = c.Mastery;
            c.Mastery = System.Math.Min(600, c.Mastery + amount);
            foreach (var lvl in CharacterData.MasteryLevels)
                if (old < lvl && c.Mastery >= lvl && lvl > 0)
                    Popups.Enqueue($"🎖️ {c.Name} — Maîtrise {c.MasteryTier()} !");
            if (old < 450 && c.Mastery >= 450)
                Popups.Enqueue($"⚡ {c.Name} débloque {c.MasteryUltimateName} (touche C) !");
            SaveSystem.Save();
        }

        // Tracked separately for save/load
        public static List<string> OwnedWeapons   = new() { "Épée Six Seven", "Bouclier Trois Cieux" };
        public static List<string> OwnedChars     = new() { "Jimmy", "Kaito Shadow", "Ryo Thunder" };
        public static List<string> OwnedVehicles  = new() { "Tommy Mayo" };
        public static List<string> OwnedFruits    = new() { "Fruit du Golem" };  // fruits possédés
        public static bool[]       StoryProgress  = new bool[52];  // 52 chapitres
        public static int          LastChapterIndex = 0;  // dernier chapitre consulté/joué

        // ── Statistiques globales pour les quêtes ─────────────────
        public static int EnemiesKilled    = 0;
        public static int BossesDefeated   = 0;
        public static int DungeonsCompleted= 0;
        public static int MaxComboReached  = 0;
        public static int TotalGoldEarned  = 0;
        public static int MagesKilled      = 0;

        // ── Événement Monde : îles, PNJ, pêche ────────────────────
        public static HashSet<string> VisitedIslands = new();
        public static HashSet<string> NpcsMet         = new();
        public static int  FishCaught          = 0;
        public static int  LegendaryFishCaught = 0;
        public static bool AbsoluDefeated      = false;
        public static List<string> OwnedFishingRods = new() { "Canne en Bois" };
        public static string? EquippedFishingRod    = "Canne en Bois";
        public static Dictionary<string, int> FishInventory = new();

        public static void VisitIsland(string islandName)
        {
            if (VisitedIslands.Add(islandName)) SaveSystem.Save();
        }

        public static void TalkToNpc(string npcId)
        {
            if (NpcsMet.Add(npcId)) SaveSystem.Save();
        }

        public static void EquipFishingRod(string name)
        {
            foreach (var r in Catalog.FishingRods) r.IsEquipped = false;
            EquippedFishingRod = name;
            var rod = Catalog.FishingRods.Find(r => r.Name == name);
            if (rod != null) rod.IsEquipped = true;
            SaveSystem.Save();
        }

        public static FishingRodData? GetEquippedFishingRod() =>
            EquippedFishingRod == null ? null :
            Catalog.FishingRods.Find(r => r.Name == EquippedFishingRod);

        public static void AddFish(string fishName, int qty, bool isLegendaryCatch)
        {
            if (!FishInventory.ContainsKey(fishName)) FishInventory[fishName] = 0;
            FishInventory[fishName] += qty;
            FishCaught += qty;
            if (isLegendaryCatch) LegendaryFishCaught += qty;
            Popups.Enqueue($"🐟 +{qty} {FishInfo.GetIcon(fishName)} {FishInfo.GetLabel(fishName)}");
            SaveSystem.Save();
        }

        public static bool HasFish(string fishName, int qty) =>
            FishInventory.TryGetValue(fishName, out int v) && v >= qty;

        public static bool SellFish(string fishName, int qty)
        {
            if (!HasFish(fishName, qty)) return false;
            FishInventory[fishName] -= qty;
            SaveSystem.Save();
            return true;
        }

        // ── Kills boss pour débloquer les fruits ──────────────────
        public const  int BossKillsRequired = 3;  // 3 kills pour obtenir le fruit
        public static Dictionary<string, int> BossKillCounts = new();

        public static int GetBossKills(string fruitName) =>
            BossKillCounts.TryGetValue(fruitName, out int v) ? v : 0;

        public static void IncrementBossKill(string fruitName)
        {
            if (!BossKillCounts.ContainsKey(fruitName)) BossKillCounts[fruitName] = 0;
            BossKillCounts[fruitName]++;
            SaveSystem.Save();
        }

        // ── Artefacts équipés — 1 par slot ───────────────────────
        public static Dictionary<string, string> EquippedArtifactBySlot = new(); // "Chapeau" → nom artefact
        [System.Obsolete] public static List<string> EquippedArtifacts = new(); // legacy, plus utilisé

        public static void EquipArtifact(ArtifactData a)
        {
            string slot = a.Slot.ToString();
            // Déséquipe l'ancien du même slot
            if (EquippedArtifactBySlot.TryGetValue(slot, out var old))
            {
                var prev = Catalog.Artifacts.Find(x => x.Name == old);
                if (prev != null) prev.IsEquipped = false;
            }
            EquippedArtifactBySlot[slot] = a.Name;
            a.IsEquipped = true;
            SaveSystem.Save();
        }

        public static void UnequipArtifact(ArtifactData a)
        {
            string slot = a.Slot.ToString();
            if (EquippedArtifactBySlot.TryGetValue(slot, out var cur) && cur == a.Name)
                EquippedArtifactBySlot.Remove(slot);
            a.IsEquipped = false;
            SaveSystem.Save();
        }

        public static float GetArtifactBonus(ArtifactEffect effect)
        {
            float total = 0f;
            foreach (var name in EquippedArtifactBySlot.Values)
            {
                var a = Catalog.Artifacts.Find(x => x.Name == name);
                if (a != null && a.Effect == effect) total += a.Value;
            }
            return total;
        }
        public static float ArtifactMult(ArtifactEffect effect) => 1f + GetArtifactBonus(effect);

        public static Dictionary<string, int> Materials = new()
        {
            ["CristalFeu"]    = 12,
            ["EssenceOmbres"] = 8,
            ["LarmePhoenix"]  = 5,
            ["PierreCeleste"] = 3,
            ["EclatFoudre"]   = 6,
            ["GemmeLunaire"]  = 4,
            ["CristalNoir"]   = 2,
            ["AmeDechue"]     = 1,
        };

        public static string[] CurrentTeam = new string[3];

        // Pending popups for gold/material gains
        public static readonly System.Collections.Generic.Queue<string> Popups = new();

        public static bool SpendGold(int amount)
        {
            if (Gold < amount) return false;
            Gold -= amount;
            SaveSystem.Save();
            return true;
        }

        public static void AddGold(int amount)
        {
            Gold += amount;
            Popups.Enqueue($"+{amount:N0} 💰");
            SaveSystem.Save();
        }

        public static bool HasMaterial(string m, int q) =>
            Materials.TryGetValue(m, out int v) && v >= q;

        public static bool ConsumeMaterial(string m, int q)
        {
            if (!HasMaterial(m, q)) return false;
            Materials[m] -= q;
            SaveSystem.Save();
            return true;
        }

        public static void AddMaterial(string m, int q)
        {
            if (!Materials.ContainsKey(m)) Materials[m] = 0;
            Materials[m] += q;
            Popups.Enqueue($"+{q} {MaterialInfo.GetIcon(m)}");
            SaveSystem.Save();
        }

        public static string GetRank() => RankNames[Rank];
    }

    // ── Catalog singleton ─────────────────────────────────
    public static partial class Catalog
    {
        static Catalog()
        {
            // Prix x4 : armes, fruits et accessoires (artefacts)
            foreach (var w in Weapons)   if (w.BuyPrice > 0) w.BuyPrice *= 4;
            foreach (var f in Fruits)    if (f.BuyPrice > 0) f.BuyPrice *= 4;
            foreach (var a in Artifacts) if (a.BuyPrice > 0) a.BuyPrice *= 4;
        }

        public static List<CharacterData> Characters = new()
        {
            new(){ Name="Jimmy",         Rarity=Rarity.Legendary, MaxHP=100, BaseAtk=12, BaseDef=6, BaseSpeed=8, MaxChakra=200, IsOwned=true,  BuyPrice=0,     Icon="😎" },
            new(){ Name="Kaito Shadow",  Rarity=Rarity.Epic,      MaxHP=90, BaseAtk=14, BaseDef=5, BaseSpeed=9, MaxChakra=220, IsOwned=true,  BuyPrice=0,     Icon="🥷" },
            new(){ Name="Ryo Thunder",   Rarity=Rarity.Epic,      MaxHP=95, BaseAtk=13, BaseDef=5, BaseSpeed=9, MaxChakra=250, IsOwned=true,  BuyPrice=0,     Icon="⚡" },
            new(){ Name="Sakura Storm",  Rarity=Rarity.Legendary, MaxHP=210, BaseAtk=16, BaseDef=16, BaseSpeed=8, MaxChakra=230, IsOwned=false, BuyPrice=9000,  Icon="🌸" },
            new(){ Name="Nova Blaze",    Rarity=Rarity.Rare,      MaxHP=160, BaseAtk=25, BaseDef=10, BaseSpeed=10,MaxChakra=180, IsOwned=false, BuyPrice=5000,  Icon="🔥" },
            new(){ Name="Void Walker",   Rarity=Rarity.Legendary, MaxHP=160, BaseAtk=28, BaseDef=12, BaseSpeed=7, MaxChakra=300, IsOwned=false, BuyPrice=13000, Icon="🌑" },
            new(){ Name="Lion Céleste",  Rarity=Rarity.Legendary, MaxHP=180, BaseAtk=32, BaseDef=14, BaseSpeed=8, MaxChakra=280, IsOwned=false, BuyPrice=20000, Icon="🦁" },
            new(){ Name="Dragon Fist",   Rarity=Rarity.Epic,      MaxHP=125, BaseAtk=26, BaseDef=8, BaseSpeed=9, MaxChakra=200, IsOwned=false, BuyPrice=11000, Icon="🐉" },
            new(){ Name="Zephyr Storm",  Rarity=Rarity.Legendary, MaxHP=150, BaseAtk=22, BaseDef=10, BaseSpeed=12, MaxChakra=260, IsOwned=false, BuyPrice=17000, Icon="🌪️" },
            new(){ Name="Eclipse",       Rarity=Rarity.Epic,      MaxHP=140, BaseAtk=30, BaseDef=9,  BaseSpeed=10, MaxChakra=240, IsOwned=false, BuyPrice=14000, Icon="🌒" },
            new(){ Name="Kira Void",     Rarity=Rarity.Legendary, MaxHP=170, BaseAtk=30, BaseDef=13, BaseSpeed=11, MaxChakra=270, IsOwned=false, BuyPrice=19000, Icon="🌀" },
            new(){ Name="Tsuki Eclipse", Rarity=Rarity.Legendary, MaxHP=175, BaseAtk=33, BaseDef=13, BaseSpeed=10, MaxChakra=285, IsOwned=false, BuyPrice=21000, Icon="🌙" },
            new(){ Name="Seika Arashi",  Rarity=Rarity.Legendary, MaxHP=180, BaseAtk=32, BaseDef=13, BaseSpeed=11, MaxChakra=290, IsOwned=false, BuyPrice=23000, Icon="🌺" },
            new(){ Name="Rei Mugen",     Rarity=Rarity.Legendary, MaxHP=185, BaseAtk=34, BaseDef=13, BaseSpeed=11, MaxChakra=295, IsOwned=false, BuyPrice=18000, Icon="♾️" },
            new(){ Name="Akuma Ryu",     Rarity=Rarity.Legendary, MaxHP=190, BaseAtk=35, BaseDef=12, BaseSpeed=10, MaxChakra=300, IsOwned=false, BuyPrice=22000, Icon="🔱" },
        };

        public static List<WeaponData> Weapons = new()
        {
            // ── COMMUNS ──────────────────────────────────────────
            new(){ Name="Dague Tranchante",       Type=WeaponType.Sword,    BaseDamage=30,  Rarity=Rarity.Common,    IsOwned=false, BuyPrice=800,   Icon="🔪",  Costs=new(){ new(){Material="CristalFeu",    Quantity=1} } },
            new(){ Name="Arc de Bois",            Type=WeaponType.Bow,      BaseDamage=20,  Rarity=Rarity.Common,    IsOwned=false, BuyPrice=600,   Icon="🏹",  Costs=new(){ new(){Material="EclatFoudre",   Quantity=1} } },
            new(){ Name="Bâton Grossier",         Type=WeaponType.Staff,    BaseDamage=25,  Rarity=Rarity.Common,    IsOwned=false, BuyPrice=500,   Icon="🪄",  Costs=new(){ new(){Material="CristalFeu",    Quantity=1} } },
            new(){ Name="Poing de Fer",           Type=WeaponType.Gauntlet, BaseDamage=22,  Rarity=Rarity.Common,    IsOwned=false, BuyPrice=700,   Icon="🥊",  Costs=new(){ new(){Material="EclatFoudre",   Quantity=1} } },
            // ── RARES ────────────────────────────────────────────
            new(){ Name="Épée de Foudre",         Type=WeaponType.Sword,    BaseDamage=60,  Rarity=Rarity.Rare,      IsOwned=false, BuyPrice=5500,  Icon="⚡",  Costs=new(){ new(){Material="EclatFoudre",   Quantity=3}, new(){Material="CristalFeu",    Quantity=2} } },
            new(){ Name="Faux Maudite",           Type=WeaponType.Scythe,   BaseDamage=55,  Rarity=Rarity.Rare,      IsOwned=false, BuyPrice=4500,  Icon="🌙",  Costs=new(){ new(){Material="EssenceOmbres", Quantity=2}, new(){Material="GemmeLunaire",  Quantity=1} } },
            new(){ Name="Bouclier des Vents",     Type=WeaponType.Shield,   BaseDamage=45,  Rarity=Rarity.Rare,      IsOwned=false, BuyPrice=5000,  Icon="🌀",  Costs=new(){ new(){Material="EclatFoudre",   Quantity=2}, new(){Material="LarmePhoenix",  Quantity=1} } },
            new(){ Name="Poing du Dragon",        Type=WeaponType.Gauntlet, BaseDamage=75,  Rarity=Rarity.Rare,      IsOwned=false, BuyPrice=11000, Icon="👊",  Costs=new(){ new(){Material="CristalFeu",    Quantity=4}, new(){Material="LarmePhoenix",  Quantity=2} } },
            // ── ÉPIQUES ──────────────────────────────────────────
            new(){ Name="Arc Éternel",            Type=WeaponType.Bow,      BaseDamage=65,  Rarity=Rarity.Epic,      IsOwned=false, BuyPrice=8000,  Icon="🪃",  Costs=new(){ new(){Material="EclatFoudre",   Quantity=4}, new(){Material="GemmeLunaire",  Quantity=3} } },
            new(){ Name="Sceptre des Ombres",     Type=WeaponType.Staff,    BaseDamage=90,  Rarity=Rarity.Epic,      IsOwned=false, BuyPrice=14000, Icon="🔱",  Costs=new(){ new(){Material="CristalNoir",   Quantity=6}, new(){Material="AmeDechue",     Quantity=2} } },
            new(){ Name="Faux du Chaos",          Type=WeaponType.Scythe,   BaseDamage=85,  Rarity=Rarity.Epic,      IsOwned=false, BuyPrice=12000, Icon="⚰️",  Costs=new(){ new(){Material="EssenceOmbres", Quantity=5}, new(){Material="CristalNoir",   Quantity=2} } },
            new(){ Name="Griffes du Loup",        Type=WeaponType.Gauntlet, BaseDamage=80,  Rarity=Rarity.Epic,      IsOwned=false, BuyPrice=10000, Icon="🐾",  Costs=new(){ new(){Material="LarmePhoenix",  Quantity=4}, new(){Material="GemmeLunaire",  Quantity=2} } },
            // ── LÉGENDAIRES ───────────────────────────────────────
            new(){ Name="Épée Six Seven",         Type=WeaponType.Sword,    BaseDamage=80,  Rarity=Rarity.Legendary, IsOwned=true,  BuyPrice=0,     Icon="⚔️",  Costs=new(){ new(){Material="CristalFeu",    Quantity=3}, new(){Material="EssenceOmbres",Quantity=2} } },
            new(){ Name="Bouclier Trois Cieux",   Type=WeaponType.Shield,   BaseDamage=40,  Rarity=Rarity.Legendary, IsOwned=true,  BuyPrice=0,     Icon="🛡️",  Costs=new(){ new(){Material="LarmePhoenix",  Quantity=5}, new(){Material="PierreCeleste", Quantity=1} } },
            new(){ Name="Lame du Chaos",          Type=WeaponType.Sword,    BaseDamage=110, Rarity=Rarity.Legendary, IsOwned=false, BuyPrice=20000, Icon="🗡️",  Costs=new(){ new(){Material="EssenceOmbres", Quantity=5}, new(){Material="CristalNoir",   Quantity=3} } },
            new(){ Name="Lance du Dragon",        Type=WeaponType.Staff,    BaseDamage=100, Rarity=Rarity.Legendary, IsOwned=false, BuyPrice=18000, Icon="🐉",  Costs=new(){ new(){Material="AmeDechue",     Quantity=2}, new(){Material="PierreCeleste", Quantity=3} } },
            new(){ Name="Faux Dimensionnelle",    Type=WeaponType.Scythe,   BaseDamage=125, Rarity=Rarity.Legendary, IsOwned=false, BuyPrice=22000, Icon="☠️",  Costs=new(){ new(){Material="AmeDechue",     Quantity=3}, new(){Material="CristalNoir",   Quantity=4} } },
            new(){ Name="Arc du Crépuscule",      Type=WeaponType.Bow,      BaseDamage=62,  Rarity=Rarity.Rare,      IsOwned=false, BuyPrice=6000,  Icon="🌙",  Costs=new(){ new(){Material="EssenceOmbres", Quantity=2}, new(){Material="GemmeLunaire",  Quantity=2} } },
            new(){ Name="Gantelets Stellaires",   Type=WeaponType.Gauntlet, BaseDamage=88,  Rarity=Rarity.Epic,      IsOwned=false, BuyPrice=13000, Icon="⭐",  Costs=new(){ new(){Material="PierreCeleste", Quantity=4}, new(){Material="EclatFoudre",   Quantity=3} } },
            new(){ Name="Lame de Givre",           Type=WeaponType.Sword,    BaseDamage=63,  Rarity=Rarity.Rare,      IsOwned=false, BuyPrice=5800,  Icon="❄️",  Costs=new(){ new(){Material="EssenceOmbres", Quantity=2}, new(){Material="GemmeLunaire",  Quantity=2} } },
            new(){ Name="Lame Lunaire",             Type=WeaponType.Sword,    BaseDamage=82,  Rarity=Rarity.Epic,      IsOwned=false, BuyPrice=12500, Icon="🌙",  Costs=new(){ new(){Material="GemmeLunaire",  Quantity=4}, new(){Material="EssenceOmbres", Quantity=3} } },
            new(){ Name="Hache des Tempêtes",       Type=WeaponType.Scythe,   BaseDamage=62,  Rarity=Rarity.Rare,      IsOwned=false, BuyPrice=5100,  Icon="🌩️", Costs=new(){ new(){Material="EclatFoudre",   Quantity=2}, new(){Material="LarmePhoenix",  Quantity=2} } },
            new(){ Name="Étoile Dimensionnelle",    Type=WeaponType.Bow,      BaseDamage=118, Rarity=Rarity.Legendary, IsOwned=false, BuyPrice=24000, Icon="🌠",  Costs=new(){ new(){Material="PierreCeleste", Quantity=5}, new(){Material="AmeDechue",     Quantity=2} } },
            new(){ Name="Lame de l'Éveil",          Type=WeaponType.Sword,    BaseDamage=65,  Rarity=Rarity.Rare,      IsOwned=false, BuyPrice=5700,  Icon="✨",  Costs=new(){ new(){Material="EclatFoudre",   Quantity=3}, new(){Material="LarmePhoenix",  Quantity=1} } },
            new(){ Name="Sceptre de l'Infini",      Type=WeaponType.Staff,    BaseDamage=105, Rarity=Rarity.Legendary, IsOwned=false, BuyPrice=20000, Icon="💫",  Costs=new(){ new(){Material="AmeDechue",     Quantity=2}, new(){Material="CristalNoir",   Quantity=4} } },
            new(){ Name="Lame des Abysses",          Type=WeaponType.Sword,    BaseDamage=67,  Rarity=Rarity.Rare,      IsOwned=false, BuyPrice=6200,  Icon="🌊",  Costs=new(){ new(){Material="EssenceOmbres", Quantity=2}, new(){Material="CristalNoir",   Quantity=1} } },
            new(){ Name="Bâton du Tonnerre Céleste", Type=WeaponType.Staff,    BaseDamage=94,  Rarity=Rarity.Epic,      IsOwned=false, BuyPrice=15000, Icon="⛈️", Costs=new(){ new(){Material="EclatFoudre",   Quantity=5}, new(){Material="PierreCeleste", Quantity=2} } },
        };

        public static List<AbilityData> Abilities = new()
        {
            new(){ Name="Domaine du Monarque",      Icon="🌑", Type=AbilityType.DomainMonarque,      Damage=150, ChakraCost=80,  Cooldown=30, Duration=10, IsOwned=false, BuyPrice=12000, Description="Invoque 3 soldats des ombres pendant 10s." },
            new(){ Name="Fruit du Golem",           Icon="🪨", Type=AbilityType.FruitGolem,           Damage=0,   ChakraCost=60,  Cooldown=25, Duration=8,  IsOwned=false, BuyPrice=18500, Description="Transformation titan, défense ×4 pendant 8s." },
            new(){ Name="Rasengan Dimensionnel",    Icon="🌀", Type=AbilityType.RasenganDimensionnel, Damage=200, ChakraCost=50,  Cooldown=15, Duration=1,  IsOwned=true,  BuyPrice=9000,  Description="Lance un tourbillon de chakra pur." },
            new(){ Name="Frappe Sérieuse",          Icon="👊", Type=AbilityType.FrappeSérieuse,       Damage=999, ChakraCost=100, Cooldown=60, Duration=0.5f,IsOwned=false,BuyPrice=25000, Description="Un coup qui détruit tout dans un rayon de 15m." },
            new(){ Name="Haki des Rois",            Icon="⚡", Type=AbilityType.HakiRois,             Damage=0,   ChakraCost=70,  Cooldown=20, Duration=5,  IsOwned=false, BuyPrice=15000, Description="Paralyse tous les ennemis proches pendant 5s." },
            // ── Capacités de classes (obtenues via donjon) ──────────
            new(){ Name="Invocation des Ombres",  Icon="👥", Type=AbilityType.DomainMonarque, Damage=180, ChakraCost=60, Cooldown=12, Duration=8,  IsOwned=false, BuyPrice=0, Description="[Monarque] Invoque 2 soldats des ombres depuis les ennemis vaincus." },
            new(){ Name="Armure des Ombres",       Icon="🌑", Type=AbilityType.DomainMonarque, Damage=0,   ChakraCost=40, Cooldown=18, Duration=10, IsOwned=false, BuyPrice=0, Description="[Monarque] DEF×3 pendant 10s, aura sombre." },
            new(){ Name="Extraction des Ombres",   Icon="🕳️", Type=AbilityType.DomainMonarque, Damage=300, ChakraCost=90, Cooldown=30, Duration=1,  IsOwned=false, BuyPrice=0, Description="[Monarque] Extrait l'essence d'un ennemi — dégâts massifs." },
            new(){ Name="Chaîne Électrique",        Icon="⛓️", Type=AbilityType.HakiRois,       Damage=120, ChakraCost=45, Cooldown=8,  Duration=1,  IsOwned=false, BuyPrice=0, Description="[Foudre] Chaîne d'éclairs qui rebondit sur 3 ennemis." },
            new(){ Name="Vitesse du Tonnerre",      Icon="💨", Type=AbilityType.HakiRois,       Damage=80,  ChakraCost=35, Cooldown=6,  Duration=0.5f,IsOwned=false,BuyPrice=0, Description="[Foudre] Dash ultra-rapide + coup de poing électrique." },
            new(){ Name="Tempête de Foudre",        Icon="🌩️", Type=AbilityType.HakiRois,       Damage=250, ChakraCost=80, Cooldown=22, Duration=3,  IsOwned=false, BuyPrice=0, Description="[Foudre] Zone d'éclairs couvrant tout l'écran pendant 3s." },
            new(){ Name="Écailles du Dragon",       Icon="🐉", Type=AbilityType.FruitGolem,     Damage=0,   ChakraCost=50, Cooldown=20, Duration=12, IsOwned=false, BuyPrice=0, Description="[Dragon] Bouclier d'écailles — absorbe 60% des dégâts." },
            new(){ Name="Souffle de Feu Dragon",    Icon="🔥", Type=AbilityType.FruitGolem,     Damage=200, ChakraCost=65, Cooldown=14, Duration=2,  IsOwned=false, BuyPrice=0, Description="[Dragon] Souffle de flammes qui traverse la zone." },
            new(){ Name="Forme Draconique",         Icon="🐲", Type=AbilityType.FruitGolem,     Damage=0,   ChakraCost=100,Cooldown=35, Duration=15, IsOwned=false, BuyPrice=0, Description="[Dragon] Transformation partielle +ATK×2 +DEF×2 pendant 15s." },
            new(){ Name="Frappe Fatale",             Icon="🗡️", Type=AbilityType.FrappeSérieuse, Damage=400, ChakraCost=80, Cooldown=25, Duration=0.5f,IsOwned=false,BuyPrice=0, Description="[Assassin] Coup critique garantissant 400 dégâts depuis l'ombre." },
            new(){ Name="Voile de Brume",            Icon="🌫️", Type=AbilityType.FrappeSérieuse, Damage=0,   ChakraCost=35, Cooldown=15, Duration=5,  IsOwned=false, BuyPrice=0, Description="[Assassin] Invisibilité pendant 5s — immunité aux attaques." },
            new(){ Name="Sentence de Mort",          Icon="☠️", Type=AbilityType.FrappeSérieuse, Damage=600, ChakraCost=120,Cooldown=40, Duration=1,  IsOwned=false, BuyPrice=0, Description="[Assassin] Condamne un ennemi à mourir en 1 coup." },
            new(){ Name="Soin Divin",                Icon="💖", Type=AbilityType.HakiRois,       Damage=-1,  ChakraCost=50, Cooldown=15, Duration=0,  IsOwned=false, BuyPrice=0, Description="[Céleste] Restaure 50% des HP max instantanément." },
            new(){ Name="Bouclier Sacré",            Icon="🛡️", Type=AbilityType.DomainMonarque, Damage=0,   ChakraCost=40, Cooldown=20, Duration=8,  IsOwned=false, BuyPrice=0, Description="[Céleste] Bouclier divin absorbant 80% des dégâts pendant 8s." },
            new(){ Name="Jugement Céleste",          Icon="☀️", Type=AbilityType.RasenganDimensionnel, Damage=350, ChakraCost=90, Cooldown=28, Duration=1, IsOwned=false, BuyPrice=0, Description="[Céleste] Rayon de lumière divine frappe tous les ennemis visibles." },
        };

        public static List<VehicleData> Vehicles = new()
        {
            new(){ Name="Tommy Mayo",          Icon="🏍️", Speed=18, Acceleration=12, TrickBonus=1.0f, Rarity=Rarity.Legendary, IsOwned=true,  BuyPrice=0     },
            new(){ Name="Dragster de l'Abîme", Icon="🐉", Speed=20, Acceleration=14, TrickBonus=1.2f, Rarity=Rarity.Epic,      IsOwned=false, BuyPrice=10000 },
            new(){ Name="Phoenix Rider",       Icon="🚀", Speed=22, Acceleration=16, TrickBonus=1.5f, Rarity=Rarity.Legendary, IsOwned=false, BuyPrice=16000 },
            new(){ Name="Vaisseau Céleste",    Icon="🛸", Speed=25, Acceleration=18, TrickBonus=1.8f, Rarity=Rarity.Legendary, IsOwned=false, BuyPrice=22000 },
            new(){ Name="Loup des Steppes",    Icon="🐺", Speed=15, Acceleration=10, TrickBonus=0.9f, Rarity=Rarity.Rare,      IsOwned=false, BuyPrice=7000  },
            new(){ Name="Comète Noire",        Icon="☄️", Speed=21, Acceleration=15, TrickBonus=1.4f, Rarity=Rarity.Epic,      IsOwned=false, BuyPrice=12000 },
            new(){ Name="Dragon Volant",        Icon="🐲", Speed=20, Acceleration=14, TrickBonus=1.3f, Rarity=Rarity.Epic,      IsOwned=false, BuyPrice=14000 },
            new(){ Name="Fantôme des Cieux",    Icon="👻", Speed=19, Acceleration=13, TrickBonus=1.1f, Rarity=Rarity.Epic,      IsOwned=false, BuyPrice=9000  },
            new(){ Name="Tornade Astrale",      Icon="🌪️", Speed=21, Acceleration=15, TrickBonus=1.4f, Rarity=Rarity.Epic,      IsOwned=false, BuyPrice=11000 },
            new(){ Name="Raijin Drift",         Icon="⚡", Speed=23, Acceleration=17, TrickBonus=1.6f, Rarity=Rarity.Legendary, IsOwned=false, BuyPrice=20000 },
        };

        public static List<DungeonData> Dungeons = new()
        {
            // ── RANG E ───────────────────────────────────────────
            new(){ Name="Caverne des Ombres",    Icon="🕯️", Difficulty=DifficultyLevel.Easy,      RequiredRank=0, EnemyCount=6,  GoldReward=120,  Rewards=new(){ new(){Material="CristalFeu",    Min=2, Max=4} } },
            new(){ Name="Forêt Obscure",         Icon="🌲", Difficulty=DifficultyLevel.Easy,      RequiredRank=0, EnemyCount=5,  GoldReward=100,  Rewards=new(){ new(){Material="CristalFeu",    Min=1, Max=3} } },
            new(){ Name="Ruines Oubliées",       Icon="🏚️", Difficulty=DifficultyLevel.Easy,      RequiredRank=0, EnemyCount=7,  GoldReward=140,  Rewards=new(){ new(){Material="EclatFoudre",   Min=1, Max=3} } },
            // ── RANG D ───────────────────────────────────────────
            new(){ Name="Marécage Maudit",       Icon="🌿", Difficulty=DifficultyLevel.Easy,      RequiredRank=1, EnemyCount=8,  GoldReward=160,  Rewards=new(){ new(){Material="EssenceOmbres", Min=1, Max=3} } },
            new(){ Name="Catacombes",            Icon="💀", Difficulty=DifficultyLevel.Medium,    RequiredRank=1, EnemyCount=9,  GoldReward=180,  Rewards=new(){ new(){Material="GemmeLunaire",  Min=1, Max=2} } },
            new(){ Name="Crypte Dorée",          Icon="🪙", Difficulty=DifficultyLevel.Easy,      RequiredRank=1, EnemyCount=8,  GoldReward=170,  Rewards=new(){ new(){Material="CristalFeu",    Min=2, Max=4} } },
            // ── RANG C ───────────────────────────────────────────
            new(){ Name="Temple du Feu",         Icon="🔥", Difficulty=DifficultyLevel.Medium,    RequiredRank=2, EnemyCount=9,  GoldReward=220,  Rewards=new(){ new(){Material="LarmePhoenix",  Min=1, Max=3} } },
            new(){ Name="Volcan Intérieur",      Icon="🌋", Difficulty=DifficultyLevel.Medium,    RequiredRank=2, EnemyCount=10, GoldReward=240,  Rewards=new(){ new(){Material="LarmePhoenix",  Min=2, Max=4} } },
            new(){ Name="Château Maudit",        Icon="🏰", Difficulty=DifficultyLevel.Medium,    RequiredRank=2, EnemyCount=9,  GoldReward=230,  Rewards=new(){ new(){Material="EssenceOmbres", Min=1, Max=3} } },
            // ── RANG B ───────────────────────────────────────────
            new(){ Name="Sanctuaire Céleste",    Icon="✨", Difficulty=DifficultyLevel.Medium,    RequiredRank=3, EnemyCount=8,  GoldReward=260,  Rewards=new(){ new(){Material="PierreCeleste", Min=1, Max=2} } },
            new(){ Name="Tour des Illusions",    Icon="🌈", Difficulty=DifficultyLevel.Hard,      RequiredRank=3, EnemyCount=11, GoldReward=300,  Rewards=new(){ new(){Material="PierreCeleste", Min=1, Max=3} } },
            new(){ Name="Palais du Silence",     Icon="🌙", Difficulty=DifficultyLevel.Medium,    RequiredRank=3, EnemyCount=10, GoldReward=280,  Rewards=new(){ new(){Material="EssenceOmbres", Min=2, Max=3} } },
            // ── RANG A ───────────────────────────────────────────
            new(){ Name="Tour de la Tempête",    Icon="⚡", Difficulty=DifficultyLevel.Hard,      RequiredRank=4, EnemyCount=12, GoldReward=380,  Rewards=new(){ new(){Material="EclatFoudre",   Min=2, Max=4} } },
            new(){ Name="Forteresse des Éclairs",Icon="⛩️", Difficulty=DifficultyLevel.Hard,      RequiredRank=4, EnemyCount=13, GoldReward=420,  Rewards=new(){ new(){Material="EclatFoudre",   Min=3, Max=5} } },
            new(){ Name="Arène des Champions",   Icon="🏟️", Difficulty=DifficultyLevel.Hard,      RequiredRank=4, EnemyCount=12, GoldReward=400,  Rewards=new(){ new(){Material="CristalNoir",   Min=1, Max=2} } },
            // ── RANG S ───────────────────────────────────────────
            new(){ Name="Abîme du Néant",        Icon="👹", Difficulty=DifficultyLevel.Boss,      RequiredRank=5, EnemyCount=5,  GoldReward=600,  Rewards=new(){ new(){Material="EssenceOmbres", Min=3, Max=5} } },
            new(){ Name="Forteresse du Chaos",   Icon="🌑", Difficulty=DifficultyLevel.Boss,      RequiredRank=5, EnemyCount=7,  GoldReward=700,  Rewards=new(){ new(){Material="AmeDechue",     Min=1, Max=2} } },
            new(){ Name="Palais des Spectres",   Icon="👻", Difficulty=DifficultyLevel.Boss,      RequiredRank=5, EnemyCount=6,  GoldReward=650,  Rewards=new(){ new(){Material="CristalNoir",   Min=2, Max=3} } },
            // ── RANG SS ──────────────────────────────────────────
            new(){ Name="Domaine du Roi",        Icon="👑", Difficulty=DifficultyLevel.Legendary, RequiredRank=6, EnemyCount=15, GoldReward=1000, Rewards=new(){ new(){Material="CristalNoir",   Min=2, Max=3} } },
            new(){ Name="Sanctuaire de l'Absolu",Icon="🌌", Difficulty=DifficultyLevel.Legendary, RequiredRank=6, EnemyCount=18, GoldReward=1200, Rewards=new(){ new(){Material="AmeDechue",     Min=2, Max=3}, new(){Material="CristalNoir", Min=1, Max=2} } },
            new(){ Name="Citadelle de l'Éternité",Icon="🏯",Difficulty=DifficultyLevel.Legendary, RequiredRank=6, EnemyCount=20, GoldReward=1500, Rewards=new(){ new(){Material="AmeDechue",     Min=3, Max=5} } },
            // ── DONJON DE CLASSE (débloqué au niveau 30) ─────────
            new(){ Name="Porte de la Destinée",  Icon="🌀", Difficulty=DifficultyLevel.Boss,
                   RequiredLevel=30, IsClassDungeon=true, EnemyCount=8, GoldReward=0,
                   Rewards=new() },
            // ── NOUVEAUX DONJONS ──────────────────────────────────
            new(){ Name="Grotte du Cristal Maudit", Icon="💎", Difficulty=DifficultyLevel.Easy,   RequiredRank=0, EnemyCount=6,  GoldReward=130,  Rewards=new(){ new(){Material="CristalFeu",    Min=2, Max=4}, new(){Material="EclatFoudre",   Min=1, Max=2} } },
            new(){ Name="Forteresse de l'Aube Noire",Icon="🌑",Difficulty=DifficultyLevel.Hard,   RequiredRank=4, EnemyCount=14, GoldReward=450,  Rewards=new(){ new(){Material="EssenceOmbres", Min=2, Max=4}, new(){Material="GemmeLunaire",  Min=1, Max=3} } },
            new(){ Name="Crypte des Éclairs Dormants", Icon="⚡", Difficulty=DifficultyLevel.Medium, RequiredRank=2, EnemyCount=10, GoldReward=200, Rewards=new(){ new(){Material="EclatFoudre",   Min=1, Max=3}, new(){Material="CristalFeu",    Min=1, Max=2} } },
            new(){ Name="Caverne du Phénix Noir",      Icon="🔥", Difficulty=DifficultyLevel.Boss,   RequiredRank=4, EnemyCount=8,  GoldReward=520, Rewards=new(){ new(){Material="LarmePhoenix",  Min=2, Max=4}, new(){Material="EssenceOmbres", Min=1, Max=3} } },
            new(){ Name="Temple de l'Orage Sacré",     Icon="🌩️", Difficulty=DifficultyLevel.Medium,    RequiredRank=2, EnemyCount=9,  GoldReward=210, Rewards=new(){ new(){Material="EclatFoudre",   Min=2, Max=3}, new(){Material="LarmePhoenix",  Min=1, Max=2} } },
            new(){ Name="Citadelle du Néant Primordial",Icon="👁️", Difficulty=DifficultyLevel.Legendary, RequiredRank=6, EnemyCount=18, GoldReward=1300, Rewards=new(){ new(){Material="AmeDechue",    Min=2, Max=4}, new(){Material="CristalNoir",   Min=2, Max=3} } },
            new(){ Name="Crypte des Étoiles Tombées",  Icon="🌠", Difficulty=DifficultyLevel.Medium,    RequiredRank=3, EnemyCount=10, GoldReward=270,  Rewards=new(){ new(){Material="GemmeLunaire",  Min=1, Max=3}, new(){Material="PierreCeleste", Min=1, Max=2} } },
            new(){ Name="Sanctuaire des Âmes Perdues", Icon="💀", Difficulty=DifficultyLevel.Boss,      RequiredRank=5, EnemyCount=7,  GoldReward=680,  Rewards=new(){ new(){Material="EssenceOmbres", Min=3, Max=5}, new(){Material="CristalNoir",   Min=1, Max=2} } },
            new(){ Name="Crypte des Esprits Ancestraux", Icon="👻", Difficulty=DifficultyLevel.Medium, RequiredRank=1, EnemyCount=8,  GoldReward=185,  Rewards=new(){ new(){Material="GemmeLunaire",  Min=1, Max=2}, new(){Material="CristalFeu",    Min=1, Max=3} } },
            new(){ Name="Colisée des Légendes",          Icon="🏛️", Difficulty=DifficultyLevel.Legendary, RequiredRank=5, EnemyCount=16, GoldReward=850,  Rewards=new(){ new(){Material="AmeDechue",     Min=2, Max=4}, new(){Material="CristalNoir",   Min=1, Max=3} } },
            // ── ÉVÉNEMENT MONDE : combats d'îles (mêmes récompenses/pool que les donjons classiques) ──
            new(){ Name="Faille du Chasseur E",   Icon="🔴", Difficulty=DifficultyLevel.Easy,   RequiredRank=0, EnemyCount=2,  GoldReward=140,  Rewards=new(){ new(){Material="EssenceOmbres", Min=1, Max=3} } },
            new(){ Name="Sanctuaire Gris",        Icon="🗝️", Difficulty=DifficultyLevel.Medium, RequiredRank=2, EnemyCount=2,  GoldReward=220,  Rewards=new(){ new(){Material="GemmeLunaire",  Min=1, Max=2} } },
            new(){ Name="Abîme du Monarque",      Icon="👁️", Difficulty=DifficultyLevel.Boss,   RequiredRank=4, EnemyCount=1,  GoldReward=700,  Rewards=new(){ new(){Material="AmeDechue", Min=1, Max=2} } },
            new(){ Name="Récif Pirate",           Icon="🏴‍☠️", Difficulty=DifficultyLevel.Easy,  RequiredRank=0, EnemyCount=2,  GoldReward=145,  Rewards=new(){ new(){Material="CristalFeu",    Min=1, Max=3} } },
            new(){ Name="Archipel Tempête",       Icon="🌊", Difficulty=DifficultyLevel.Medium, RequiredRank=2, EnemyCount=2,  GoldReward=225,  Rewards=new(){ new(){Material="EclatFoudre",   Min=1, Max=3} } },
            new(){ Name="Baie du Roi Naufragé",   Icon="⚓", Difficulty=DifficultyLevel.Boss,   RequiredRank=4, EnemyCount=1,  GoldReward=710,  Rewards=new(){ new(){Material="AmeDechue", Min=1, Max=2} } },
            new(){ Name="Feuille Grise",          Icon="🍃", Difficulty=DifficultyLevel.Easy,   RequiredRank=0, EnemyCount=2,  GoldReward=150,  Rewards=new(){ new(){Material="EclatFoudre",   Min=1, Max=3} } },
            new(){ Name="Brume Rouge",            Icon="🌫️", Difficulty=DifficultyLevel.Medium, RequiredRank=2, EnemyCount=2,  GoldReward=230,  Rewards=new(){ new(){Material="GemmeLunaire",  Min=1, Max=2} } },
            new(){ Name="Sceau Ancestral",        Icon="⛩️", Difficulty=DifficultyLevel.Boss,   RequiredRank=4, EnemyCount=1,  GoldReward=720,  Rewards=new(){ new(){Material="AmeDechue", Min=1, Max=2} } },
        };

        // ── CLASSES DU JOUEUR ──────────────────────────────────────
        public static readonly ClassData[] PlayerClasses = {
            new(){ Name="Monarque des Ombres", Icon="👤", Color="purple",
                Description="Invoque des soldats des ombres depuis les morts ennemis. Maître des ténèbres.",
                Abilities=new[]{ "Invocation des Ombres","Armure des Ombres","Extraction des Ombres" } },
            new(){ Name="Seigneur de la Foudre", Icon="⚡", Color="yellow",
                Description="Vitesse absolue et chaînes d'éclairs. Frappe avant que l'ennemi réagisse.",
                Abilities=new[]{ "Chaîne Électrique","Vitesse du Tonnerre","Tempête de Foudre" } },
            new(){ Name="Roi Dragon", Icon="🐉", Color="red",
                Description="Transformation partielle en dragon. Écailles impénétrables et souffle dévastateur.",
                Abilities=new[]{ "Écailles du Dragon","Souffle de Feu Dragon","Forme Draconique" } },
            new(){ Name="Assassin de l'Aube", Icon="🗡️", Color="blue",
                Description="Frappe critique depuis l'ombre. Un seul coup peut mettre fin au combat.",
                Abilities=new[]{ "Frappe Fatale","Voile de Brume","Sentence de Mort" } },
            new(){ Name="Guérisseur Céleste", Icon="✨", Color="gold",
                Description="Aura de régénération divine. Soigne les alliés et affaiblit les ennemis.",
                Abilities=new[]{ "Soin Divin","Bouclier Sacré","Jugement Céleste" } },
        };

        public static List<TrickData> Tricks = new()
        {
            new(){ Name="Wheelie",       BaseScore=120, Difficulty=1 },
            new(){ Name="No Hands",      BaseScore=200, Difficulty=2 },
            new(){ Name="Backflip",      BaseScore=350, Difficulty=3 },
            new(){ Name="Superman",      BaseScore=500, Difficulty=4 },
            new(){ Name="Cordova",       BaseScore=650, Difficulty=4 },
            new(){ Name="Coffin Flip",   BaseScore=800, Difficulty=5 },
            new(){ Name="One Footer",    BaseScore=300, Difficulty=3 },
            new(){ Name="Death Spiral",  BaseScore=1000,Difficulty=5 },
        };

        // ── FRUITS DU DÉMON ────────────────────────────────────────
        static FruitMove M(string name, string desc, float dmg, float chakra, float cd, int mastery, string key, string icon) =>
            new FruitMove { Name=name, Desc=desc, Damage=dmg, ChakraCost=chakra, Cooldown=cd, MasteryReq=mastery, Key=key, Icon=icon };

        public static List<FruitData> Fruits = new()
        {
            // ── COMMUNS ──────────────────────────────────────────
            new(){ Name="Fruit du Golem",     Icon="🪨", Type=FruitType.Naturel,
                Rarity=Rarity.Common, IsOwned=true, BuyPrice=0, Mastery=0,
                Description="Transforme ton corps en pierre. Dégâts lourds, défense massive.",
                Moves=new[]{
                    M("Poing de Pierre","Coup de poing renforcé de roc.",30,10,0.5f,0,"Z","👊"),
                    M("Mur de Roc","Crée un mur qui blesse les ennemis proches.",50,25,2.0f,100,"X","🧱"),
                    M("Avalanche","Bombardement de rochers sur la zone.",90,50,4.0f,200,"C","🌋"),
                    M("Corps de Titan","Corps en pierre géant +DEF×3 pendant 6s.",0,80,10.0f,400,"F","🗿"),
                }},

            new(){ Name="Fruit de la Fleur",  Icon="🌸", Type=FruitType.Naturel,
                Rarity=Rarity.Common, IsOwned=false, BuyPrice=0, Mastery=0,
                Description="Contrôle les plantes et fleurs. Soins et pièges.",
                Moves=new[]{
                    M("Épines","Lance des épines acérées.",20,8,0.4f,0,"Z","🌿"),
                    M("Soin Floral","Se soigne de 30% des HP max.",0,35,4.0f,100,"X","💐"),
                    M("Forêt Piège","Lianes qui immobilisent les ennemis.",0,50,5.0f,200,"C","🌲"),
                    M("Jardin Eden","Aura de régénération de 5 secondes.",0,90,12.5f,400,"F","🌺"),
                }},

            // ── RARES ────────────────────────────────────────────
            new(){ Name="Fruit du Phénix",    Icon="🔥", Type=FruitType.Bête,
                Rarity=Rarity.Rare, IsOwned=false, BuyPrice=0, Mastery=0,
                Description="Transformation en Phénix de feu. Régénération et flammes.",
                Moves=new[]{
                    M("Plume de Feu","Lance des plumes enflammées.",35,12,0.5f,0,"Z","🪶"),
                    M("Aile Brûlante","Frappe avec une aile de flammes.",65,28,2.0f,100,"X","🔥"),
                    M("Piqué Phénix","Plonge du ciel en laissant un sillage de feu.",110,55,4.5f,200,"C","💫"),
                    M("Renaissance","Régénère 50% des HP et brûle les ennemis proches.",0,100,15.0f,400,"F","✨"),
                }},

            new(){ Name="Fruit Glace",         Icon="❄️", Type=FruitType.Élémentaire,
                Rarity=Rarity.Rare, IsOwned=false, BuyPrice=0, Mastery=0,
                Description="Logia du froid absolu. Gèle les ennemis et crée des structures de glace.",
                Moves=new[]{
                    M("Souffle Glacé","Projette un souffle de glace.",28,10,0.4f,0,"Z","🌬️"),
                    M("Lance de Glace","Lance une lance d'ice perçante.",70,30,1.0f,100,"X","🧊"),
                    M("Tempête Gelée","Tempête de neige sur toute la zone.",95,55,5.0f,200,"C","🌨️"),
                    M("Époque Glaciaire","Transforme le sol en glace et stun les ennemis.",0,95,5.5f,400,"F","🏔️"),
                }},

            new(){ Name="Fruit du Gaz",        Icon="☁️", Type=FruitType.Élémentaire,
                Rarity=Rarity.Rare, IsOwned=false, BuyPrice=3800, Mastery=0,
                Description="Logia du gaz toxique. Empoisonnement et nuages létaux.",
                Moves=new[]{
                    M("Nuage Toxique","Crée un nuage empoisonnant.",25,10,0.4f,0,"Z","🌫️"),
                    M("Explosion Gaz","Explosion du gaz accumulé.",80,35,1.2f,100,"X","💥"),
                    M("Chambre Gaz","Remplit la zone de gaz mortel.",70,60,6.0f,200,"C","☣️"),
                    M("Règne du Gaz","Transformation gazeuse — immunité 4s.",0,90,12.5f,400,"F","🌪️"),
                }},

            // ── ÉPIQUES ──────────────────────────────────────────
            new(){ Name="Fruit de l'Éclair",   Icon="⚡", Type=FruitType.Élémentaire,
                Rarity=Rarity.Epic, IsOwned=false, BuyPrice=0, Mastery=0,
                Description="Logia de la foudre. Vitesse absolue et attaques électriques.",
                Moves=new[]{
                    M("Éclair Rapide","Coup de foudre instantané.",40,15,0.35f,0,"Z","⚡"),
                    M("Éclair de Zeus","Foudre du ciel dévastatrice.",85,35,1.0f,100,"X","🌩️"),
                    M("Tempête Élec.","Décharge électrique en zone.",120,60,4.0f,200,"C","🔌"),
                    M("Vitesse Lumière","Dash instantané à travers la zone.",0,100,4.5f,400,"F","💨"),
                }},

            new(){ Name="Fruit du Sphinx",     Icon="🦁", Type=FruitType.Bête,
                Rarity=Rarity.Epic, IsOwned=false, BuyPrice=0, Mastery=0,
                Description="Transformation hybride lion-homme. Force brute et rugissement dévastateur.",
                Moves=new[]{
                    M("Griffe Léonine","Griffe puissante à 3 coups.",38,12,0.4f,0,"Z","🐾"),
                    M("Rugissement","Rugissement qui stun les ennemis 2s.",0,30,2.5f,100,"X","📢"),
                    M("Bond du Fauve","Saut sur l'ennemi le plus proche.",100,55,4.5f,200,"C","🦁"),
                    M("Forme Titan","Transformation complète en lion géant 8s.",0,110,14.0f,400,"F","👑"),
                }},

            new(){ Name="Fruit du Son",        Icon="🎵", Type=FruitType.Naturel,
                Rarity=Rarity.Epic, IsOwned=false, BuyPrice=10000, Mastery=0,
                Description="Contrôle des ondes sonores. Paralysie et attaques soniques.",
                Moves=new[]{
                    M("Onde Sonique","Lance une onde de choc sonore.",35,12,0.4f,0,"Z","〰️"),
                    M("Barrière Son","Bouclier de sons qui repousse.",0,28,2.0f,100,"X","🔊"),
                    M("Cri Ultime","Cri dévastateur qui stun tout le monde.",80,65,5.0f,200,"C","📣"),
                    M("Symphonie Mortelle","Mélodie qui inflige DoT à tous les ennemis.",60,95,5.5f,400,"F","🎶"),
                }},

            // ── LÉGENDAIRES ───────────────────────────────────────
            new(){ Name="Fruit des Ombres",    Icon="🌑", Type=FruitType.Élémentaire,
                Rarity=Rarity.Legendary, IsOwned=false, BuyPrice=0, Mastery=0,
                Description="Logia des ténèbres absolues. Absorption de la lumière et pouvoir des ombres.",
                Moves=new[]{
                    M("Griffe d'Ombre","Griffe sortant de l'ombre.",50,15,0.4f,0,"Z","👤"),
                    M("Absorption","Aspire les ennemis proches.",85,40,2.5f,100,"X","🕳️"),
                    M("Monde des Ombres","Plonge la zone dans l'obscurité.",120,70,2.5f,200,"C","🌑"),
                    M("Ultime Ténèbre","Libère un cataclysme de ténèbres.",200,120,7.5f,400,"F","💀"),
                },
                CanTransform=true, TransformAuraKey="fx_aura_shadow", TransformAtkMult=1.7f, TransformSpeedMult=1.25f},

            new(){ Name="Fruit du Magma",      Icon="🌋", Type=FruitType.Élémentaire,
                Rarity=Rarity.Legendary, IsOwned=false, BuyPrice=0, Mastery=0,
                Description="Logia de lave. Supérieur au feu — brûle même l'eau.",
                Moves=new[]{
                    M("Poing Magma","Poing de lave en fusion.",55,15,0.4f,0,"Z","🔴"),
                    M("Volcan","Éruption de lave en zone.",100,45,2.5f,100,"X","🌋"),
                    M("Déluge Magma","Pluie de lave sur toute la zone.",145,75,6.0f,200,"C","☄️"),
                    M("Île de Feu","Transforme le sol en lave — dégâts continus.",0,130,14.0f,400,"F","🏝️"),
                },
                CanTransform=true, TransformAuraKey="fx_aura_magma", TransformAtkMult=1.8f, TransformSpeedMult=1.15f},

            new(){ Name="Fruit de la Lumière", Icon="☀️", Type=FruitType.Élémentaire,
                Rarity=Rarity.Legendary, IsOwned=false, BuyPrice=0, Mastery=0,
                Description="Logia de lumière. Le plus rapide, laser et vitesse de la lumière.",
                Moves=new[]{
                    M("Laser Solaire","Laser de lumière rapide.",45,14,0.3f,0,"Z","🔆"),
                    M("Mille Flèches","Pluie de flèches lumineuses.",110,45,1.2f,100,"X","✨"),
                    M("Tempête Solaire","Explosion de lumière aveuglante.",150,75,3.0f,200,"C","💫"),
                    M("Vitesse Absolue","Frappe tous les ennemis visibles en 0.5s.",180,140,7.5f,400,"F","⚡"),
                },
                CanTransform=true, TransformAuraKey="fx_aura_light", TransformAtkMult=1.6f, TransformSpeedMult=1.5f},

            // ── MYTHIQUES ─────────────────────────────────────────
            new(){ Name="Sekai Sekai no Mi",   Icon="🌀", Type=FruitType.Naturel,
                Rarity=Rarity.Mythical, IsOwned=false, BuyPrice=0, Mastery=0,
                Description="Fruit du Monde. Ouvre des portails entre dimensions et contrôle l'espace-temps.",
                Moves=new[]{
                    M("Portail Offensif","Envoie l'ennemi dans un micro-portail puis le projette.",60,20,0.5f,0,"Z","🌀"),
                    M("Distorsion","Crée une distorsion spatiale qui repousse tout.",100,50,2.5f,100,"X","💫"),
                    M("Portail Massif","Ouvre un portail géant qui aspire les ennemis.",160,80,5.0f,200,"C","🕳️"),
                    M("Grand Tour","Traversée dimensionnelle — téléporte partout et frappe tout.",250,150,17.5f,400,"F","🌌"),
                }},

            new(){ Name="Fruit du Dragon",     Icon="🐉", Type=FruitType.Bête,
                Rarity=Rarity.Mythical, IsOwned=false, BuyPrice=100000, Mastery=0,
                Description="Transformation dragon ancienne. La puissance ultime des bêtes.",
                Moves=new[]{
                    M("Souffle Dragon","Feu de dragon concentré.",65,20,0.45f,0,"Z","🔥"),
                    M("Griffes Dragon","Griffes draconiennes massives.",120,50,1.2f,100,"X","🐾"),
                    M("Vol du Dragon","Attaque aérienne en piqué dévastateur.",175,85,6.0f,200,"C","🦅"),
                    M("Forme Dragon","Transformation dragon complète 10s — toutes stats ×2.",0,160,20.0f,400,"F","🐉"),
                }},

            new(){ Name="Fruit du Vide",       Icon="🕳️", Type=FruitType.Élémentaire,
                Rarity=Rarity.Mythical, IsOwned=false, BuyPrice=90000, Mastery=0,
                Description="Logia du vide absolu. Trou noir, gravité et néant.",
                Moves=new[]{
                    M("Rayon du Vide","Rayon de néant qui désintègre.",60,18,0.45f,0,"Z","⚫"),
                    M("Trou Noir","Aspire tout dans un mini trou noir.",130,55,3.0f,100,"X","🌑"),
                    M("Singularité","Trou noir géant qui dévaste la zone.",190,90,7.0f,200,"C","🕳️"),
                    M("Néant Absolu","Efface tout dans un rayon massif — dégâts ultimes.",280,170,20.0f,400,"F","💀"),
                }},

            // ── Événement Monde : fruits natifs des îles élites (drop boss, comme les autres) ──
            new(){ Name="Fruit de l'Ombre Monarque", Icon="👁️", Type=FruitType.Élémentaire,
                Rarity=Rarity.Legendary, IsOwned=false, BuyPrice=0, Mastery=0,
                Description="Domaine du Monarque de l'Ombre. Invoque des ombres serviles et frappe depuis l'obscurité.",
                Moves=new[]{
                    M("Lame d'Ombre","Frappe tranchante surgie de l'ombre.",40,15,0.5f,0,"Z","🗡️"),
                    M("Invocation Servile","Invoque une ombre qui combat à ses côtés.",70,35,3.0f,100,"X","👤"),
                    M("Domaine Silencieux","Zone d'ombre qui affaiblit tous les ennemis.",110,60,5.0f,200,"C","🌑"),
                    M("Monarque Déchu","Libère la pleine puissance du monarque — dégâts massifs.",200,130,16.0f,400,"F","👁️"),
                }},
            new(){ Name="Fruit du Roi des Mers", Icon="⚓", Type=FruitType.Bête,
                Rarity=Rarity.Legendary, IsOwned=false, BuyPrice=0, Mastery=0,
                Description="Commande les océans et les créatures des abysses.",
                Moves=new[]{
                    M("Lame de Vague","Tranchant d'eau compressée.",42,15,0.5f,0,"Z","🌊"),
                    M("Appel des Abysses","Invoque une créature marine pour attaquer.",72,35,3.0f,100,"X","🐙"),
                    M("Raz-de-Marée","Vague géante qui submerge la zone.",115,60,5.0f,200,"C","🌊"),
                    M("Couronne des Mers","Déchaîne la tempête ultime du Roi des Mers.",205,130,16.0f,400,"F","👑"),
                }},
            new(){ Name="Fruit du Sage des Six Voies", Icon="⛩️", Type=FruitType.Naturel,
                Rarity=Rarity.Legendary, IsOwned=false, BuyPrice=0, Mastery=0,
                Description="Chakra ancestral scellé dans le temple de la montagne. Maîtrise des six voies.",
                Moves=new[]{
                    M("Poing Chakra","Coup chargé de chakra pur.",41,15,0.5f,0,"Z","👊"),
                    M("Chemin Animal","Invoque un esprit-guide pour assister au combat.",71,35,3.0f,100,"X","🦊"),
                    M("Sceau des Six Voies","Libère un sceau qui frappe toute la zone.",112,60,5.0f,200,"C","⛩️"),
                    M("Voie du Sage","Éveil complet du chakra des six voies.",202,130,16.0f,400,"F","🌀"),
                }},
        };

        static QuestReward G(int amt) => new(){ RewardType="gold", Amount=amt };
        static QuestReward M(string mat, int amt) => new(){ RewardType="material", Key=mat, Amount=amt };
        static QuestReward X(int amt) => new(){ RewardType="xp", Amount=amt };

        public static List<QuestData> Quests = new()
        {
            // ── DÉBUTANT ──────────────────────────────────────────
            new(){ Name="Premiers Pas",         Icon="👣", Category="Débutant",   Objective=QuestObjectiveType.KillEnemies,      Target=10,   Description="Vaincre 10 ennemis",                 Rewards=new[]{ G(300),  X(80) } },
            new(){ Name="Guerrier en Herbe",    Icon="⚔️", Category="Débutant",   Objective=QuestObjectiveType.KillEnemies,      Target=50,   Description="Vaincre 50 ennemis",                 Rewards=new[]{ G(800),  M("CristalFeu",3) } },
            new(){ Name="Premier Donjon",       Icon="🏰", Category="Débutant",   Objective=QuestObjectiveType.CompleteDungeons, Target=1,    Description="Terminer un donjon",                 Rewards=new[]{ G(500),  X(100) } },
            new(){ Name="Collectionneur",       Icon="🗡️", Category="Débutant",   Objective=QuestObjectiveType.OwnWeapons,       Target=3,    Description="Posséder 3 armes",                   Rewards=new[]{ G(600),  M("EclatFoudre",3) } },
            new(){ Name="Combo Débutant",       Icon="💥", Category="Débutant",   Objective=QuestObjectiveType.DoCombo,          Target=3,    Description="Réaliser un combo x3",               Rewards=new[]{ G(400),  M("CristalFeu",2) } },
            // ── COMBAT ────────────────────────────────────────────
            new(){ Name="Chasseur de Donjons",  Icon="🗺️", Category="Combat",     Objective=QuestObjectiveType.CompleteDungeons, Target=5,    Description="Terminer 5 donjons",                 Rewards=new[]{ G(1200), M("LarmePhoenix",3) } },
            new(){ Name="Briseur de Boss",      Icon="👹", Category="Combat",     Objective=QuestObjectiveType.KillBosses,       Target=1,    Description="Vaincre un boss",                    Rewards=new[]{ G(1000), M("EssenceOmbres",3) } },
            new(){ Name="Exterminateur",        Icon="💀", Category="Combat",     Objective=QuestObjectiveType.KillEnemies,      Target=200,  Description="Vaincre 200 ennemis",                Rewards=new[]{ G(2500), M("CristalNoir",2) } },
            new(){ Name="Chasseur de Boss",     Icon="🌑", Category="Combat",     Objective=QuestObjectiveType.KillBosses,       Target=5,    Description="Vaincre 5 boss",                     Rewards=new[]{ G(3000), M("AmeDechue",1) } },
            new(){ Name="Maître des Combos",    Icon="🌀", Category="Combat",     Objective=QuestObjectiveType.DoCombo,          Target=5,    Description="Réaliser un combo x5",               Rewards=new[]{ G(1500), M("PierreCeleste",2) } },
            new(){ Name="Chasseur de Magiciens",Icon="🧙", Category="Combat",     Objective=QuestObjectiveType.KillMages,        Target=9,    Description="Vaincre 9 magiciens",                Rewards=new[]{ G(1400), M("GemmeLunaire",3) } },
            new(){ Name="Fléau des Mages",      Icon="🔮", Category="Maîtrise",   Objective=QuestObjectiveType.KillMages,        Target=30,   Description="Vaincre 30 magiciens",               Rewards=new[]{ G(5000), M("CristalNoir",3), X(500) } },
            // ── EXPLORATION ───────────────────────────────────────
            new(){ Name="Grand Explorateur",    Icon="🌍", Category="Exploration", Objective=QuestObjectiveType.CompleteDungeons,Target=10,   Description="Terminer 10 donjons",                Rewards=new[]{ G(2000), M("PierreCeleste",3) } },
            new(){ Name="Rang D",               Icon="🏅", Category="Exploration", Objective=QuestObjectiveType.ReachRank,        Target=1,    Description="Atteindre le rang D",                Rewards=new[]{ G(800),  X(200) } },
            new(){ Name="Rang C",               Icon="🥈", Category="Exploration", Objective=QuestObjectiveType.ReachRank,        Target=2,    Description="Atteindre le rang C",                Rewards=new[]{ G(1500), M("GemmeLunaire",3) } },
            new(){ Name="Rang B",               Icon="🥇", Category="Exploration", Objective=QuestObjectiveType.ReachRank,        Target=3,    Description="Atteindre le rang B",                Rewards=new[]{ G(3000), M("CristalNoir",3) } },
            new(){ Name="Collectionneur Fruits",Icon="🍎", Category="Exploration", Objective=QuestObjectiveType.OwnFruits,        Target=3,    Description="Posséder 3 fruits du démon",         Rewards=new[]{ G(2000), M("AmeDechue",1) } },
            // ── MAÎTRISE ──────────────────────────────────────────
            new(){ Name="Guerrier Confirmé",    Icon="⭐", Category="Maîtrise",   Objective=QuestObjectiveType.ReachLevel,       Target=20,   Description="Atteindre le niveau 20",             Rewards=new[]{ G(3000), M("PierreCeleste",4), X(500) } },
            new(){ Name="Champion",             Icon="👑", Category="Maîtrise",   Objective=QuestObjectiveType.ReachLevel,       Target=50,   Description="Atteindre le niveau 50",             Rewards=new[]{ G(8000), M("AmeDechue",3) } },
            new(){ Name="Rang A",               Icon="💎", Category="Maîtrise",   Objective=QuestObjectiveType.ReachRank,        Target=4,    Description="Atteindre le rang A",                Rewards=new[]{ G(6000), M("AmeDechue",2) } },
            new(){ Name="Gauntlet Maître",      Icon="🔥", Category="Maîtrise",   Objective=QuestObjectiveType.KillBosses,       Target=20,   Description="Vaincre 20 boss",                    Rewards=new[]{ G(10000), M("AmeDechue",5) } },
            new(){ Name="Légende",              Icon="🌌", Category="Maîtrise",   Objective=QuestObjectiveType.KillEnemies,      Target=1000, Description="Vaincre 1000 ennemis",               Rewards=new[]{ G(20000), M("AmeDechue",5), X(2000) } },
            // ── ÎLE (Événement Monde) ───────────────────────────────
            new(){ Name="Nettoyeur de Faille",    Icon="🔴", Category="Île", Objective=QuestObjectiveType.KillEnemies,   Target=15, Description="Vaincre 15 ennemis (Île de la Porte Écarlate)",        Rewards=new[]{ G(900),  M("EssenceOmbres",2) } },
            new(){ Name="Éclaireur de la Porte",  Icon="🧭", Category="Île", Objective=QuestObjectiveType.ExploreIsland, Target=1,  Description="Explorer une île de l'Événement Monde",                Rewards=new[]{ G(600),  X(150) } },
            new(){ Name="Purge du Sanctuaire",    Icon="🗝️", Category="Île", Objective=QuestObjectiveType.KillEnemies,   Target=30, Description="Vaincre 30 ennemis (Sanctuaire du Chasseur Gris)",      Rewards=new[]{ G(1600), M("GemmeLunaire",2) } },
            new(){ Name="Le Pacte Brisé",         Icon="👻", Category="Île", Objective=QuestObjectiveType.KillBosses,    Target=2,  Description="Vaincre 2 boss (Sanctuaire du Chasseur Gris)",          Rewards=new[]{ G(2200), M("EssenceOmbres",3) } },
            new(){ Name="Le Gardien Muet",        Icon="👁️", Category="Île", Objective=QuestObjectiveType.KillBosses,    Target=4,  Description="Vaincre 4 boss (Abîme du Monarque Silencieux)",         Rewards=new[]{ G(4200), M("AmeDechue",2) } },
            new(){ Name="Éclat du Monarque",      Icon="💠", Category="Île", Objective=QuestObjectiveType.ExploreIsland, Target=3,  Description="Explorer 3 îles de l'Événement Monde",                  Rewards=new[]{ G(2500), M("PierreCeleste",3) } },
            new(){ Name="Pilleur de Récif",       Icon="🏴‍☠️", Category="Île", Objective=QuestObjectiveType.KillEnemies,   Target=18, Description="Vaincre 18 ennemis (Île du Grand Récif)",               Rewards=new[]{ G(1000), M("CristalFeu",2) } },
            new(){ Name="Rencontre au Port",      Icon="⚓", Category="Île", Objective=QuestObjectiveType.TalkToNpc,     Target=1,  Description="Parler à un PNJ sur une île",                          Rewards=new[]{ G(500),  X(120) } },
            new(){ Name="Chasse à la Tempête",    Icon="🌊", Category="Île", Objective=QuestObjectiveType.KillEnemies,   Target=35, Description="Vaincre 35 ennemis (Archipel des Tempêtes)",            Rewards=new[]{ G(1900), M("EclatFoudre",3) } },
            new(){ Name="Le Journal du Naufragé", Icon="📜", Category="Île", Objective=QuestObjectiveType.TalkToNpc,     Target=3,  Description="Parler à 3 PNJ différents",                             Rewards=new[]{ G(1400), M("GemmeLunaire",2) } },
            new(){ Name="L'Amiral Fantôme",       Icon="⚓", Category="Île", Objective=QuestObjectiveType.KillBosses,    Target=5,  Description="Vaincre 5 boss (Baie du Roi Naufragé)",                Rewards=new[]{ G(4500), M("AmeDechue",2) } },
            new(){ Name="Trésor du Roi Englouti", Icon="💰", Category="Île", Objective=QuestObjectiveType.ExploreIsland, Target=6,  Description="Explorer 6 îles de l'Événement Monde",                  Rewards=new[]{ G(4000), M("CristalNoir",3) } },
            new(){ Name="Chasse aux Renégats",    Icon="🍃", Category="Île", Objective=QuestObjectiveType.KillEnemies,   Target=20, Description="Vaincre 20 ennemis (Village de la Feuille Grise)",      Rewards=new[]{ G(1100), M("EclatFoudre",2) } },
            new(){ Name="Le Message du Village",  Icon="✉️", Category="Île", Objective=QuestObjectiveType.TalkToNpc,     Target=2,  Description="Parler à 2 PNJ différents",                            Rewards=new[]{ G(900),  X(150) } },
            new(){ Name="Brouillard Interdit",    Icon="🌫️", Category="Île", Objective=QuestObjectiveType.KillEnemies,   Target=40, Description="Vaincre 40 ennemis (Vallée de la Brume Rouge)",         Rewards=new[]{ G(2100), M("GemmeLunaire",3) } },
            new(){ Name="Pêcheur du Village",     Icon="🎣", Category="Île", Objective=QuestObjectiveType.CatchFish,     Target=5,  Description="Pêcher 5 poissons",                                     Rewards=new[]{ G(1200), M("CristalFeu",2) } },
            new(){ Name="Le Sceau Interdit",      Icon="⛩️", Category="Île", Objective=QuestObjectiveType.KillBosses,    Target=6,  Description="Vaincre 6 boss (Temple du Sceau Ancestral)",            Rewards=new[]{ G(5000), M("AmeDechue",3) } },
            new(){ Name="Chakra du Temple",       Icon="🌀", Category="Île", Objective=QuestObjectiveType.ExploreIsland, Target=9,  Description="Explorer les 9 îles de l'Événement Monde",              Rewards=new[]{ G(8000), M("AmeDechue",3), X(1000) } },
            new(){ Name="Le Successeur de l'Absolu", Icon="👑", Category="Maîtrise", Objective=QuestObjectiveType.DefeatAbsolu, Target=1, Description="Affronter et vaincre L'Absolu, le gardien du Grand Tour", Rewards=new[]{ G(50000), M("AmeDechue",10), X(5000) } },
        };

        public static List<ArtifactData> Artifacts = new()
        {
            // ── CHAPEAUX 🎩 ───────────────────────────────────────
            new(){ Name="Casquette de Rue",       Icon="🧢", Slot=ArtifactSlot.Chapeau,  Rarity=Rarity.Common,    Effect=ArtifactEffect.SpeedBoost,    Value=0.08f, BuyPrice=1200,  IsOwned=false, Description="Une casquette streetwear légère — boost de mobilité." },
            new(){ Name="Chapeau du Capitaine",   Icon="🎩", Slot=ArtifactSlot.Chapeau,  Rarity=Rarity.Rare,      Effect=ArtifactEffect.GoldBoost,     Value=0.20f, BuyPrice=5000,  IsOwned=false, Description="Chapeau de pirate dimensionnel — attire l'or des ennemis." },
            new(){ Name="Turban de Foudre",       Icon="⚡", Slot=ArtifactSlot.Chapeau,  Rarity=Rarity.Epic,      Effect=ArtifactEffect.CooldownReduce,Value=0.25f, BuyPrice=13000, IsOwned=false, Description="Turban chargé d'éclairs — réduit les temps de recharge." },
            new(){ Name="Couronne de l'Absolu",   Icon="👑", Slot=ArtifactSlot.Chapeau,  Rarity=Rarity.Legendary, Effect=ArtifactEffect.AtkBoost,      Value=0.35f, BuyPrice=40000, IsOwned=false, Description="Couronne forgée dans la dimension zéro. Puissance absolue." },
            // ── AMULETTES 📿 ──────────────────────────────────────
            new(){ Name="Amulette de Pierre",     Icon="🪨", Slot=ArtifactSlot.Amulette, Rarity=Rarity.Common,    Effect=ArtifactEffect.HpBoost,       Value=0.10f, BuyPrice=1500,  IsOwned=false, Description="Fragment de roc dimensionnel — renforce la vitalité." },
            new(){ Name="Pendentif des Ombres",   Icon="🌑", Slot=ArtifactSlot.Amulette, Rarity=Rarity.Rare,      Effect=ArtifactEffect.MeleeDmgBoost, Value=0.18f, BuyPrice=5500,  IsOwned=false, Description="Extrait des ombres d'un boss — amplifie les frappes." },
            new(){ Name="Cœur du Phénix",         Icon="🔥", Slot=ArtifactSlot.Amulette, Rarity=Rarity.Epic,      Effect=ArtifactEffect.FruitDmgBoost, Value=0.28f, BuyPrice=15000, IsOwned=false, Description="Larme cristallisée — décuple la puissance des fruits." },
            new(){ Name="Cristal de l'Absolu",    Icon="💎", Slot=ArtifactSlot.Amulette, Rarity=Rarity.Legendary, Effect=ArtifactEffect.HpBoost,       Value=0.45f, BuyPrice=38000, IsOwned=false, Description="Fragment de l'armure de l'Absolu. Résistance transcendante." },
            // ── BAGUES 💍 ─────────────────────────────────────────
            new(){ Name="Bague de Feu",           Icon="🔴", Slot=ArtifactSlot.Bague,    Rarity=Rarity.Common,    Effect=ArtifactEffect.AtkBoost,      Value=0.08f, BuyPrice=1500,  IsOwned=false, Description="Cristal volcanique — enflamme les attaques." },
            new(){ Name="Sceau de l'Éclair",      Icon="💍", Slot=ArtifactSlot.Bague,    Rarity=Rarity.Rare,      Effect=ArtifactEffect.SwordDmgBoost, Value=0.20f, BuyPrice=6000,  IsOwned=false, Description="Bague électrique — booste les attaques d'épée." },
            new(){ Name="Pierre Céleste",         Icon="⭐", Slot=ArtifactSlot.Bague,    Rarity=Rarity.Epic,      Effect=ArtifactEffect.XpBoost,       Value=0.30f, BuyPrice=12000, IsOwned=false, Description="Minéral céleste — accélère la progression." },
            new(){ Name="Anneau des Rois",        Icon="🌌", Slot=ArtifactSlot.Bague,    Rarity=Rarity.Legendary, Effect=ArtifactEffect.MeleeDmgBoost, Value=0.38f, BuyPrice=42000, IsOwned=false, Description="Forgé dans la dimension zéro — dégâts de mêlée transcendants." },
            // ── CAPES 🧣 ──────────────────────────────────────────
            new(){ Name="Cape du Voyageur",       Icon="🧣", Slot=ArtifactSlot.Cape,     Rarity=Rarity.Common,    Effect=ArtifactEffect.DefBoost,      Value=0.10f, BuyPrice=1200,  IsOwned=false, Description="Cape légère — absorbe les coups faibles." },
            new(){ Name="Talisman Lunaire",       Icon="🌙", Slot=ArtifactSlot.Cape,     Rarity=Rarity.Rare,      Effect=ArtifactEffect.XpBoost,       Value=0.22f, BuyPrice=5000,  IsOwned=false, Description="Gemme lunaire — collecte l'XP ambiante en marchant." },
            new(){ Name="Manteau de l'Ombre",     Icon="🌑", Slot=ArtifactSlot.Cape,     Rarity=Rarity.Epic,      Effect=ArtifactEffect.DefBoost,      Value=0.28f, BuyPrice=14000, IsOwned=false, Description="Tissu des ombres — immunité partielle aux dégâts." },
            new(){ Name="Rune du Grand Tour",     Icon="🌊", Slot=ArtifactSlot.Cape,     Rarity=Rarity.Legendary, Effect=ArtifactEffect.GoldBoost,     Value=0.50f, BuyPrice=45000, IsOwned=false, Description="Cape runique du Tommy Mayo — or doublé dans tous les combats." },
        };

        public static List<string> Backgrounds = new()
        {
            "Cosmos","Volcan","Océan","Forêt","Néon City","Tempête","Sakura","Néant"
        };

        public static readonly string[] StoryChapters = {
            "ACTE I — L'ÉVEIL DU DERNIER CHASSEUR\n\nKai Shadowstep est le chasseur le plus faible du monde — rang E dans un système " +
            "qui classe les humains selon leur capacité à combattre les monstres surgissant de portails dimensionnels.\n\n" +
            "Lors d'une mission qui devait être simple, il est abandonné dans un donjon double-portail et faillit mourir.\n\n" +
            "Mais au lieu de périr, quelque chose s'éveille en lui : le Système, une interface invisible que lui seul peut voir.\n\n" +
            "\"Quête activée : Devenez le plus fort.\"\n\n" +
            "Son pouvoir unique : l'Adaptation Infinie — il absorbe la force de chaque ennemi vaincu et ne cesse jamais de croître.",

            "ACTE II — LE FRUIT DU VOYAGEUR\n\nEn explorant les ruines d'un portail ancien, Kai découvre un fruit étrange — " +
            "le Sekai Sekai no Mi, Fruit du Monde.\n\nIl lui confère le pouvoir d'ouvrir des portails entre dimensions.\n\n" +
            "C'est là qu'il rencontre Jimmy, navigateur excentrique, Sakura Storm, combattante au souffle de cyclone, " +
            "et Ryo Thunder dont les poings canalisent la foudre.\n\n" +
            "Ensemble, ils forment l'équipage du Grand Tour, naviguant à bord du légendaire véhicule Tommy Mayo.",

            "ACTE III — LE ROYAUME DES CHAKRAS\n\nLe portail les propulse dans le Royaume des Chakras — un monde où " +
            "la force intérieure s'exprime à travers des techniques anciennes transmises de maître à élève.\n\n" +
            "Kai apprend à marier son Adaptation Infinie avec le chakra : naît alors le Rasengan Dimensionnel, " +
            "une attaque qui déchire l'espace-temps.\n\n" +
            "Le Syndicat du Néant — douze individus masqués — cherche à capturer tous les utilisateurs de portails " +
            "pour fusionner les dimensions en un seul chaos.",

            "ACTE IV — L'ABSOLU\n\nAu cœur de toutes les dimensions se dresse L'Absolu — un être qui a transcendé " +
            "tous les systèmes de puissance.\n\nIl vainc n'importe quel ennemi d'un seul geste. Il s'ennuie. Il attend.\n\n" +
            "Kai et son équipage l'affrontent dans la dimension zéro.\n\n" +
            "L'Absolu est le gardien du Grand Tour. Chaque épreuve n'était qu'un test pour trouver un successeur.\n\n" +
            "\"Je ne cherche pas à être le plus fort. Je cherche à protéger ce voyage pour tous.\"\n\n" +
            "L'Absolu sourit pour la première fois depuis un millénaire.",
        };
    }
}
