using System.Collections.Generic;

namespace TravelTour.Core
{
    public class FishingRodData
    {
        public string  Name = "", Icon = "";
        public Rarity  Rarity;
        public int     BuyPrice;
        public bool    IsOwned;
        public bool    IsEquipped;
        public float   BiteSpeedBonus  = 0f;  // % réduction du temps d'attente de touche
        public float   RareChanceBonus = 0f;  // % ajouté aux chances de rareté élevée
        public bool    AutoSucceedCommonRare = false;

        public string EffectLabel() =>
            $"-{BiteSpeedBonus*100:F0}% attente  +{RareChanceBonus*100:F0}% rareté";
    }

    public class FishData
    {
        public string   Name = "", Icon = "";
        public Rarity   Rarity;
        public string[] Islands = System.Array.Empty<string>();  // noms d'îles, vide = partout
    }

    public static class FishInfo
    {
        public static string GetIcon(string name)  => Catalog.Fish.Find(f => f.Name == name)?.Icon ?? "🐟";
        public static string GetLabel(string name)  => name;

        public static int SellPrice(string name)
        {
            var f = Catalog.Fish.Find(x => x.Name == name);
            if (f == null) return 20;
            return f.Rarity switch {
                Rarity.Common    => 50,
                Rarity.Rare      => 150,
                Rarity.Epic      => 400,
                Rarity.Legendary => 1000,
                Rarity.Mythical  => 2500,
                _                => 20,
            };
        }

        // Poissons pêchables sur une île donnée (communs partagés + exclusifs de l'île)
        public static List<FishData> ForIsland(string islandName) =>
            Catalog.Fish.FindAll(f => f.Islands.Length == 0 || System.Array.Exists(f.Islands, n => n == islandName));
    }

    public static partial class Catalog
    {
        public static List<FishingRodData> FishingRods = new()
        {
            new(){ Name="Canne en Bois",               Icon="🎣", Rarity=Rarity.Common,    BuyPrice=0,     IsOwned=true,  BiteSpeedBonus=0f,    RareChanceBonus=0f },
            new(){ Name="Canne en Bambou Renforcée",    Icon="🎋", Rarity=Rarity.Rare,      BuyPrice=4800,  IsOwned=false, BiteSpeedBonus=0.15f, RareChanceBonus=0.10f },
            new(){ Name="Canne de Maître Pêcheur",      Icon="🪝", Rarity=Rarity.Epic,      BuyPrice=14000, IsOwned=false, BiteSpeedBonus=0.30f, RareChanceBonus=0.25f },
            new(){ Name="Canne du Roi des Mers",        Icon="👑", Rarity=Rarity.Legendary, BuyPrice=36000, IsOwned=false, BiteSpeedBonus=0.45f, RareChanceBonus=0.45f },
            new(){ Name="Canne Céleste",                Icon="✨", Rarity=Rarity.Mythical,  BuyPrice=80000, IsOwned=false, BiteSpeedBonus=0.60f, RareChanceBonus=0.70f, AutoSucceedCommonRare=true },
        };

        public static List<FishData> Fish = new()
        {
            // Communs / rares partagés (toutes les îles)
            new(){ Name="Sardine des Docks",   Icon="🐟", Rarity=Rarity.Common },
            new(){ Name="Maquereau Rayé",      Icon="🐠", Rarity=Rarity.Common },
            new(){ Name="Anguille Foudre",     Icon="⚡", Rarity=Rarity.Rare },
            new(){ Name="Poisson-Lune Abyssal",Icon="🌕", Rarity=Rarity.Rare },

            // Épiques exclusifs par île (Chasseur)
            new(){ Name="Carpe des Ombres",           Icon="🐡", Rarity=Rarity.Epic, Islands=new[]{ "Île de la Porte Écarlate" } },
            new(){ Name="Anguille du Gardien",        Icon="🐍", Rarity=Rarity.Epic, Islands=new[]{ "Sanctuaire du Chasseur Gris" } },
            new(){ Name="Poisson-Cristal du Monarque",Icon="💎", Rarity=Rarity.Epic, Islands=new[]{ "Abîme du Monarque Silencieux" } },
            // Épiques exclusifs par île (Pirate)
            new(){ Name="Poulpe Doré",         Icon="🐙", Rarity=Rarity.Epic, Islands=new[]{ "Île du Grand Récif" } },
            new(){ Name="Espadon Marine",       Icon="🗡️", Rarity=Rarity.Epic, Islands=new[]{ "Archipel des Tempêtes" } },
            new(){ Name="Requin-Tigre Royal",   Icon="🦈", Rarity=Rarity.Epic, Islands=new[]{ "Baie du Roi Naufragé" } },
            // Épiques exclusifs par île (Ninja)
            new(){ Name="Carpe Chakra",         Icon="🎏", Rarity=Rarity.Epic, Islands=new[]{ "Village de la Feuille Grise" } },
            new(){ Name="Anguille du Renard",   Icon="🦊", Rarity=Rarity.Epic, Islands=new[]{ "Vallée de la Brume Rouge" } },
            new(){ Name="Poisson-Dragon des Kages", Icon="🐉", Rarity=Rarity.Epic, Islands=new[]{ "Temple du Sceau Ancestral" } },

            // Légendaires "poissons-boss", un par thème, pêchables sur les 3 îles du thème
            new(){ Name="Ombre Léviathan",       Icon="🐋", Rarity=Rarity.Legendary,
                   Islands=new[]{ "Île de la Porte Écarlate", "Sanctuaire du Chasseur Gris", "Abîme du Monarque Silencieux" } },
            new(){ Name="Roi des Mers Éternel",  Icon="🌊", Rarity=Rarity.Legendary,
                   Islands=new[]{ "Île du Grand Récif", "Archipel des Tempêtes", "Baie du Roi Naufragé" } },
            new(){ Name="Kyubi des Profondeurs", Icon="🦊", Rarity=Rarity.Legendary,
                   Islands=new[]{ "Village de la Feuille Grise", "Vallée de la Brume Rouge", "Temple du Sceau Ancestral" } },
        };
    }
}
