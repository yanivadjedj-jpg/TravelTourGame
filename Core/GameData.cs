using System.Collections.Generic;

namespace TravelTour.Core
{
    // ── Enums ──────────────────────────────────────────────
    public enum Rarity      { Common, Rare, Epic, Legendary, Mythical }
    public enum GameState   { MainMenu, Crosspark, Team, Boutique, Training, Story, Background, Combat, Tutorial, Wallet, Fruits, Inventory, CardGame }
    public enum AbilityType { DomainMonarque, FruitGolem, RasenganDimensionnel, FrappeSérieuse, HakiRois }
    public enum WeaponType  { Sword, Shield, Bow, Staff, Gauntlet, Scythe }
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
    }

    public class WeaponData
    {
        public string     Name, Icon;
        public WeaponType Type;
        public float      BaseDamage;
        public Rarity     Rarity;
        public int        Level = 1, MaxLevel = 10;
        public bool       IsOwned;
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

        // Tracked separately for save/load
        public static List<string> OwnedWeapons   = new() { "Épée Six Seven", "Bouclier Trois Cieux" };
        public static List<string> OwnedChars     = new() { "Jimmy", "Kaito Shadow", "Ryo Thunder" };
        public static List<string> OwnedVehicles  = new() { "Tommy Mayo" };
        public static List<string> OwnedFruits    = new() { "Fruit du Golem" };  // fruits possédés
        public static bool[]       StoryProgress  = new bool[50];  // 50 chapitres

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
    public static class Catalog
    {
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
                    M("Poing de Pierre","Coup de poing renforcé de roc.",30,10,0.5f,  0,"Z","👊"),
                    M("Mur de Roc",    "Crée un mur qui blesse les ennemis proches.",50,25,4f,100,"X","🧱"),
                    M("Avalanche",     "Bombardement de rochers sur la zone.",90,50,8f, 200,"C","🌋"),
                    M("Corps de Titan","Corps en pierre géant +DEF×3 pendant 6s.",0, 80,20f,400,"F","🗿"),
                }},

            new(){ Name="Fruit de la Fleur",  Icon="🌸", Type=FruitType.Naturel,
                Rarity=Rarity.Common, IsOwned=false, BuyPrice=800, Mastery=0,
                Description="Contrôle les plantes et fleurs. Soins et pièges.",
                Moves=new[]{
                    M("Épines",      "Lance des épines acérées.",20,8,0.4f,   0,"Z","🌿"),
                    M("Soin Floral", "Se soigne de 30% des HP max.",0, 35,8f, 100,"X","💐"),
                    M("Forêt Piège", "Lianes qui immobilisent les ennemis.",0, 50,10f,200,"C","🌲"),
                    M("Jardin Eden", "Aura de régénération de 5 secondes.",0, 90,25f,400,"F","🌺"),
                }},

            // ── RARES ────────────────────────────────────────────
            new(){ Name="Fruit du Phénix",    Icon="🔥", Type=FruitType.Bête,
                Rarity=Rarity.Rare, IsOwned=false, BuyPrice=3500, Mastery=0,
                Description="Transformation en Phénix de feu. Régénération et flammes.",
                Moves=new[]{
                    M("Plume de Feu",   "Lance des plumes enflammées.",35,12,0.5f, 0,"Z","🪶"),
                    M("Aile Brûlante", "Frappe avec une aile de flammes.",65,28,4f,100,"X","🔥"),
                    M("Piqué Phénix",  "Plonge du ciel en laissant un sillage de feu.",110,55,9f,200,"C","💫"),
                    M("Renaissance",   "Régénère 50% des HP et brûle les ennemis proches.",0,100,30f,400,"F","✨"),
                }},

            new(){ Name="Fruit Glace",         Icon="❄️", Type=FruitType.Élémentaire,
                Rarity=Rarity.Rare, IsOwned=false, BuyPrice=4000, Mastery=0,
                Description="Logia du froid absolu. Gèle les ennemis et crée des structures de glace.",
                Moves=new[]{
                    M("Souffle Glacé", "Projette un souffle de glace.",28,10,0.4f,  0,"Z","🌬️"),
                    M("Lance de Glace","Lance une lance d'ice perçante.",70,30,4f,100,"X","🧊"),
                    M("Tempête Gelée", "Tempête de neige sur toute la zone.",95,55,10f,200,"C","🌨️"),
                    M("Époque Glaciaire","Transforme le sol en glace et stun les ennemis.",0,95,22f,400,"F","🏔️"),
                }},

            new(){ Name="Fruit du Gaz",        Icon="☁️", Type=FruitType.Élémentaire,
                Rarity=Rarity.Rare, IsOwned=false, BuyPrice=3800, Mastery=0,
                Description="Logia du gaz toxique. Empoisonnement et nuages létaux.",
                Moves=new[]{
                    M("Nuage Toxique","Crée un nuage empoisonnant.",25,10,0.4f,  0,"Z","🌫️"),
                    M("Explosion Gaz","Explosion du gaz accumulé.",80,35,5f,   100,"X","💥"),
                    M("Chambre Gaz",  "Remplit la zone de gaz mortel.",70,60,12f, 200,"C","☣️"),
                    M("Règne du Gaz", "Transformation gazeuse — immunité 4s.",0,90,25f,400,"F","🌪️"),
                }},

            // ── ÉPIQUES ──────────────────────────────────────────
            new(){ Name="Fruit de l'Éclair",   Icon="⚡", Type=FruitType.Élémentaire,
                Rarity=Rarity.Epic, IsOwned=false, BuyPrice=9000, Mastery=0,
                Description="Logia de la foudre. Vitesse absolue et attaques électriques.",
                Moves=new[]{
                    M("Éclair Rapide","Coup de foudre instantané.",40,15,0.35f, 0,"Z","⚡"),
                    M("Éclair de Zeus","Foudre du ciel dévastatrice.",85,35,4f, 100,"X","🌩️"),
                    M("Tempête Élec.", "Décharge électrique en zone.",120,60,8f, 200,"C","🔌"),
                    M("Vitesse Lumière","Dash instantané à travers la zone.",0,100,18f,400,"F","💨"),
                }},

            new(){ Name="Fruit du Sphinx",     Icon="🦁", Type=FruitType.Bête,
                Rarity=Rarity.Epic, IsOwned=false, BuyPrice=11000, Mastery=0,
                Description="Transformation hybride lion-homme. Force brute et rugissement dévastateur.",
                Moves=new[]{
                    M("Griffe Léonine","Griffe puissante à 3 coups.",38,12,0.4f,  0,"Z","🐾"),
                    M("Rugissement",   "Rugissement qui stun les ennemis 2s.",0, 30,5f,100,"X","📢"),
                    M("Bond du Fauve", "Saut sur l'ennemi le plus proche.",100,55,9f,200,"C","🦁"),
                    M("Forme Titan",   "Transformation complète en lion géant 8s.",0,110,28f,400,"F","👑"),
                }},

            new(){ Name="Fruit du Son",        Icon="🎵", Type=FruitType.Naturel,
                Rarity=Rarity.Epic, IsOwned=false, BuyPrice=10000, Mastery=0,
                Description="Contrôle des ondes sonores. Paralysie et attaques soniques.",
                Moves=new[]{
                    M("Onde Sonique","Lance une onde de choc sonore.",35,12,0.4f,   0,"Z","〰️"),
                    M("Barrière Son","Bouclier de sons qui repousse.",0, 28,4f,   100,"X","🔊"),
                    M("Cri Ultime",  "Cri dévastateur qui stun tout le monde.",80,65,10f,200,"C","📣"),
                    M("Symphonie Mortelle","Mélodie qui inflige DoT à tous les ennemis.",60,95,22f,400,"F","🎶"),
                }},

            // ── LÉGENDAIRES ───────────────────────────────────────
            new(){ Name="Fruit des Ombres",    Icon="🌑", Type=FruitType.Élémentaire,
                Rarity=Rarity.Legendary, IsOwned=false, BuyPrice=25000, Mastery=0,
                Description="Logia des ténèbres absolues. Absorption de la lumière et pouvoir des ombres.",
                Moves=new[]{
                    M("Griffe d'Ombre","Griffe sortant de l'ombre.",50,15,0.4f,  0,"Z","👤"),
                    M("Absorption",    "Aspire les ennemis proches.",85,40,5f,  100,"X","🕳️"),
                    M("Monde des Ombres","Plonge la zone dans l'obscurité.",120,70,10f,200,"C","🌑"),
                    M("Ultime Ténèbre","Libère un cataclysme de ténèbres.",200,120,30f,400,"F","💀"),
                }},

            new(){ Name="Fruit du Magma",      Icon="🌋", Type=FruitType.Élémentaire,
                Rarity=Rarity.Legendary, IsOwned=false, BuyPrice=28000, Mastery=0,
                Description="Logia de lave. Supérieur au feu — brûle même l'eau.",
                Moves=new[]{
                    M("Poing Magma",  "Poing de lave en fusion.",55,15,0.4f,  0,"Z","🔴"),
                    M("Volcan",       "Éruption de lave en zone.",100,45,5f, 100,"X","🌋"),
                    M("Déluge Magma", "Pluie de lave sur toute la zone.",145,75,12f,200,"C","☄️"),
                    M("Île de Feu",   "Transforme le sol en lave — dégâts continus.",0,130,28f,400,"F","🏝️"),
                }},

            new(){ Name="Fruit de la Lumière", Icon="☀️", Type=FruitType.Élémentaire,
                Rarity=Rarity.Legendary, IsOwned=false, BuyPrice=30000, Mastery=0,
                Description="Logia de lumière. Le plus rapide, laser et vitesse de la lumière.",
                Moves=new[]{
                    M("Laser Solaire","Laser de lumière rapide.",45,14,0.30f, 0,"Z","🔆"),
                    M("Mille Flèches","Pluie de flèches lumineuses.",110,45,5f,100,"X","✨"),
                    M("Tempête Solaire","Explosion de lumière aveuglante.",150,75,12f,200,"C","💫"),
                    M("Vitesse Absolue","Frappe tous les ennemis visibles en 0.5s.",180,140,30f,400,"F","⚡"),
                }},

            // ── MYTHIQUES ─────────────────────────────────────────
            new(){ Name="Sekai Sekai no Mi",   Icon="🌀", Type=FruitType.Naturel,
                Rarity=Rarity.Mythical, IsOwned=false, BuyPrice=80000, Mastery=0,
                Description="Fruit du Monde. Ouvre des portails entre dimensions et contrôle l'espace-temps.",
                Moves=new[]{
                    M("Portail Offensif","Envoie l'ennemi dans un micro-portail puis le projette.",60,20,0.5f, 0,"Z","🌀"),
                    M("Distorsion",      "Crée une distorsion spatiale qui repousse tout.",100,50,5f,100,"X","💫"),
                    M("Portail Massif",  "Ouvre un portail géant qui aspire les ennemis.",160,80,10f,200,"C","🕳️"),
                    M("Grand Tour",      "Traversée dimensionnelle — téléporte partout et frappe tout.",250,150,35f,400,"F","🌌"),
                }},

            new(){ Name="Fruit du Dragon",     Icon="🐉", Type=FruitType.Bête,
                Rarity=Rarity.Mythical, IsOwned=false, BuyPrice=100000, Mastery=0,
                Description="Transformation dragon ancienne. La puissance ultime des bêtes.",
                Moves=new[]{
                    M("Souffle Dragon","Feu de dragon concentré.",65,20,0.45f,  0,"Z","🔥"),
                    M("Griffes Dragon","Griffes draconiennes massives.",120,50,5f,100,"X","🐾"),
                    M("Vol du Dragon", "Attaque aérienne en piqué dévastateur.",175,85,12f,200,"C","🦅"),
                    M("Forme Dragon",  "Transformation dragon complète 10s — toutes stats ×2.",0,160,40f,400,"F","🐉"),
                }},

            new(){ Name="Fruit du Vide",       Icon="🕳️", Type=FruitType.Élémentaire,
                Rarity=Rarity.Mythical, IsOwned=false, BuyPrice=90000, Mastery=0,
                Description="Logia du vide absolu. Trou noir, gravité et néant.",
                Moves=new[]{
                    M("Rayon du Vide", "Rayon de néant qui désintègre.",60,18,0.45f, 0,"Z","⚫"),
                    M("Trou Noir",     "Aspire tout dans un mini trou noir.",130,55,6f, 100,"X","🌑"),
                    M("Singularité",   "Trou noir géant qui dévaste la zone.",190,90,14f,200,"C","🕳️"),
                    M("Néant Absolu",  "Efface tout dans un rayon massif — dégâts ultimes.",280,170,40f,400,"F","💀"),
                }},
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
