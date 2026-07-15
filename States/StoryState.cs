using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using FontStashSharp;
using System.Collections.Generic;
using TravelTour.Core;
using TravelTour.UI;

namespace TravelTour.States
{
    public class StoryState : IGameState
    {
        readonly TravelTourGame _game;
        Texture2D _pixel = null!;
        SpriteFontBase _font = null!, _bigFont = null!;
        UIButton _backBtn = null!;
        List<UIButton> _actBtns = new();
        UIButton _prevBtn = null!, _nextBtn = null!, _fightBtn = null!;
        int _selectedAct = 0;      // 0-4
        int _selectedChapterInAct = 0; // index within current act
        float _time;

        // 50 chapters completed flags (global index 0-49)
        public static readonly bool[] ChaptersCompleted = new bool[54];
        public static void MarkChapterCompleted(int chapterIndex)
        {
            if (chapterIndex >= 0 && chapterIndex < ChaptersCompleted.Length)
            {
                ChaptersCompleted[chapterIndex] = true;
                // Synchronise avec PlayerSave pour la persistance
                if (chapterIndex < PlayerSave.StoryProgress.Length)
                    PlayerSave.StoryProgress[chapterIndex] = true;
                SaveSystem.Save();
            }
        }
        // Legacy alias kept for external callers
        public static void MarkCompleted(int idx) => MarkChapterCompleted(idx);

        // Act accent colors
        static readonly Color[] ActAccent = {
            new Color(0, 200, 255),      // Act 1 Solo Leveling - blue
            new Color(240, 192, 64),     // Act 2 One Piece - gold
            new Color(255, 128, 64),     // Act 3 Naruto - orange
            new Color(168, 85, 247),     // Act 4 One Punch Man - purple
            new Color(255, 255, 255),    // Act 5 Grand Tour - rainbow (handled separately)
        };
        static readonly Color[] ActBanner = {
            new Color(4, 8, 30),
            new Color(30, 15, 0),
            new Color(25, 8, 0),
            new Color(6, 0, 18),
            new Color(5, 0, 15),
        };

        // ──────────────────────────────────────────────────────────────────────
        // 50 CHAPTERS
        // ──────────────────────────────────────────────────────────────────────
        public static readonly StoryChapter[] Chapters = {
            // ── ACT 1 — Solo Leveling ─────────────────────────────────────────
            new(){
                Act=1, ChapterNum=1, Title="L'Éveil du Rang E", Tag="Solo Leveling",
                Summary="Kai Shadowstep, chasseur de rang E méprisé par tous, pénètre dans un donjon mineur pour survivre.\n"+
                        "À l'intérieur, une voix mystérieuse résonne dans son esprit : le Système s'éveille.\n"+
                        "Pour la première fois, il voit des statistiques flotter devant ses yeux.",
                Dungeon=new DungeonData{ Name="Crypte des Ombres Oubliées", Icon="🕳️",
                    Difficulty=DifficultyLevel.Easy, RequiredRank=0, EnemyCount=4, GoldReward=80,
                    Rewards=new List<MaterialReward>{ new(){Material="EssenceOmbres",Min=1,Max=2}}}},
            new(){
                Act=1, ChapterNum=2, Title="Le Système Parle", Tag="Solo Leveling",
                Summary="Le Système attribue à Kai sa première quête journalière : survivre cent pompes ou mourir.\n"+
                        "Kai comprend que ce n'est pas une métaphore.\n"+
                        "Il progresse dans un donjon de bois pourri guidé par les notifications en temps réel.",
                Dungeon=new DungeonData{ Name="Forêt Pourrissante de Mireth", Icon="🌑",
                    Difficulty=DifficultyLevel.Easy, RequiredRank=0, EnemyCount=5, GoldReward=110,
                    Rewards=new List<MaterialReward>{ new(){Material="EssenceOmbres",Min=1,Max=2}, new(){Material="CristalNoir",Min=1,Max=1}}}},
            new(){
                Act=1, ChapterNum=3, Title="Adaptation Infinie", Tag="Solo Leveling",
                Summary="Kai découvre l'aptitude secrète du Système : l'Adaptation Infinie,\n"+
                        "qui copie et renforce les compétences des monstres vaincus.\n"+
                        "Il affronte des slimes corrosifs et absorbe leur résistance chimique.",
                Dungeon=new DungeonData{ Name="Marécage des Slimes Acides", Icon="🟢",
                    Difficulty=DifficultyLevel.Easy, RequiredRank=0, EnemyCount=6, GoldReward=150,
                    Rewards=new List<MaterialReward>{ new(){Material="CristalNoir",Min=1,Max=2}, new(){Material="EssenceOmbres",Min=1,Max=2}}}},
            new(){
                Act=1, ChapterNum=4, Title="Le Donjon Interdit", Tag="Solo Leveling",
                Summary="Un donjon de rang D est officiellement interdit aux chasseurs rang E.\n"+
                        "Kai décide d'y entrer seul, ignorant les avertissements.\n"+
                        "Les squelettes armés à l'intérieur ne lui laissent aucun répit.",
                Dungeon=new DungeonData{ Name="Nécropole de l'Aube Brisée", Icon="💀",
                    Difficulty=DifficultyLevel.Medium, RequiredRank=0, EnemyCount=6, GoldReward=160,
                    Rewards=new List<MaterialReward>{ new(){Material="AmeDechue",Min=1,Max=2}, new(){Material="CristalNoir",Min=1,Max=2}}}},
            new(){
                Act=1, ChapterNum=5, Title="Ombres qui Suivent", Tag="Solo Leveling",
                Summary="Kai réalise que les monstres qu'il tue peuvent devenir ses soldats fantômes.\n"+
                        "Dans un manoir hanté plein de spectres furieux, il tente d'invoquer son premier soldat.\n"+
                        "L'armée de l'ombre commence à naître.",
                Dungeon=new DungeonData{ Name="Manoir des Spectres Furieux", Icon="👻",
                    Difficulty=DifficultyLevel.Medium, RequiredRank=0, EnemyCount=8, GoldReward=220,
                    Rewards=new List<MaterialReward>{ new(){Material="AmeDechue",Min=1,Max=3}, new(){Material="GemmeLunaire",Min=1,Max=2}}}},
            new(){
                Act=1, ChapterNum=6, Title="Trahison au Rang D", Tag="Solo Leveling",
                Summary="Une guilde de rang D tente d'éliminer Kai dans un donjon partagé pour s'approprier ses ressources.\n"+
                        "Le Système déclenche une alerte rouge et débride temporairement ses statistiques.\n"+
                        "Kai survit à la trahison et en absorbe les leçons amères comme une nouvelle compétence.",
                Dungeon=new DungeonData{ Name="Caverne des Serments Rompus", Icon="⚔️",
                    Difficulty=DifficultyLevel.Medium, RequiredRank=0, EnemyCount=9, GoldReward=280,
                    Rewards=new List<MaterialReward>{ new(){Material="EclatFoudre",Min=1,Max=2}, new(){Material="AmeDechue",Min=1,Max=2}}}},
            new(){
                Act=1, ChapterNum=7, Title="La Tempête de Cristal", Tag="Solo Leveling",
                Summary="Un donjon de rang C apparu d'urgence menace une ville entière.\n"+
                        "Kai, toujours officiellement rang E, s'y infiltre pendant que les chasseurs supérieurs refusent d'intervenir.\n"+
                        "Des élémentaires de glace cristallisée gardent un artefact ancien.",
                Dungeon=new DungeonData{ Name="Citadelle de Glace Éternelle", Icon="❄️",
                    Difficulty=DifficultyLevel.Hard, RequiredRank=0, EnemyCount=10, GoldReward=310,
                    Rewards=new List<MaterialReward>{ new(){Material="PierreCeleste",Min=1,Max=2}, new(){Material="EclatFoudre",Min=1,Max=2}}}},
            new(){
                Act=1, ChapterNum=8, Title="Rang D — Le Seuil du Feu", Tag="Solo Leveling",
                Summary="Le Système propose à Kai une épreuve de franchissement de rang.\n"+
                        "Une salle brûlante peuplée de démons de lave teste sa résistance et sa stratégie.\n"+
                        "Monter de rang signifie accepter de ne plus jamais être ordinaire.",
                Dungeon=new DungeonData{ Name="Sanctuaire des Démons de Lave", Icon="🔥",
                    Difficulty=DifficultyLevel.Hard, RequiredRank=0, EnemyCount=12, GoldReward=400,
                    Rewards=new List<MaterialReward>{ new(){Material="CristalFeu",Min=1,Max=3}, new(){Material="LarmePhoenix",Min=1,Max=2}}}},
            new(){
                Act=1, ChapterNum=9, Title="Le Gardien du Système", Tag="Solo Leveling",
                Summary="Un boss légendaire surgit d'un donjon de rang B : le Gardien du Système.\n"+
                        "Le Système révèle à Kai que ce combat était prévu depuis son éveil.\n"+
                        "Kai engage le combat avec son armée fantôme naissante et toute sa volonté.",
                Dungeon=new DungeonData{ Name="Abîme du Gardien Éternel", Icon="🌀",
                    Difficulty=DifficultyLevel.Boss, RequiredRank=0, EnemyCount=8, GoldReward=450,
                    Rewards=new List<MaterialReward>{ new(){Material="GemmeLunaire",Min=1,Max=3}, new(){Material="PierreCeleste",Min=1,Max=2}}}},
            new(){
                Act=1, ChapterNum=10, Title="L'Ascension — Rang C Confirmé", Tag="Solo Leveling",
                Summary="Après avoir vaincu le Gardien, Kai reçoit une notification jamais vue : montée de rang forcée vers C.\n"+
                        "Il obtient un titre unique — Marcheur des Ombres.\n"+
                        "Le monde des chasseurs commence à murmurer son nom. L'Acte 1 se clôt sur une nouvelle ère.",
                Dungeon=new DungeonData{ Name="Trône de l'Ombre Absolue", Icon="👑",
                    Difficulty=DifficultyLevel.Boss, BossGauntlet=true, RequiredRank=0, EnemyCount=12, GoldReward=600,
                    Rewards=new List<MaterialReward>{ new(){Material="CristalNoir",Min=2,Max=4}, new(){Material="AmeDechue",Min=1,Max=3}}}},

            // ── ACT 2 — One Piece ─────────────────────────────────────────────
            new(){
                Act=2, ChapterNum=11, Title="Le Détroit des Brumes Perdues", Tag="One Piece",
                Summary="L'équipage du Grand Tour pénètre dans un détroit maudit où les brumes effacent les souvenirs.\n"+
                        "Des pirates fantômes gardent l'entrée de la Route des Étoiles.",
                Dungeon=new DungeonData{ Name="Détroit des Brumes Perdues", Icon="🌫️",
                    Difficulty=DifficultyLevel.Easy, RequiredRank=0, EnemyCount=5, GoldReward=120,
                    Rewards=new List<MaterialReward>{ new(){Material="Ecaille de Brume",Min=1,Max=2}, new(){Material="Perle Fantome",Min=1,Max=1}}}},
            new(){
                Act=2, ChapterNum=12, Title="L'Île des Tempêtes Éternelles", Tag="One Piece",
                Summary="Sakura Storm reconnaît les signes d'une île-prison dimensionnelle entourée d'ouragans perpétuels.\n"+
                        "Les pirates de la Flotte des Vents Hurlants barrent la route.",
                Dungeon=new DungeonData{ Name="Archipel des Tempêtes Éternelles", Icon="⛈️",
                    Difficulty=DifficultyLevel.Easy, RequiredRank=0, EnemyCount=6, GoldReward=150,
                    Rewards=new List<MaterialReward>{ new(){Material="Fragment de Foudre",Min=1,Max=2}, new(){Material="Voile Dechire",Min=1,Max=1}}}},
            new(){
                Act=2, ChapterNum=13, Title="Les Ruines du Roi des Mers", Tag="One Piece",
                Summary="Au fond d'une mer dimensionnelle, l'équipage découvre les ruines d'un ancien royaume englouti.\n"+
                        "Ryo Thunder détecte que le Sekai Sekai no Mi y a laissé une trace.",
                Dungeon=new DungeonData{ Name="Ruines Immergées du Roi des Mers", Icon="🏛️",
                    Difficulty=DifficultyLevel.Easy, RequiredRank=0, EnemyCount=7, GoldReward=200,
                    Rewards=new List<MaterialReward>{ new(){Material="Pierre Abyssale",Min=1,Max=2}, new(){Material="Corail Ancien",Min=1,Max=2}}}},
            new(){
                Act=2, ChapterNum=14, Title="La Flotte du Capitaine Vortex", Tag="One Piece",
                Summary="Le redoutable Capitaine Vortex commande une armada de vaisseaux pirates dimensionnels.\n"+
                        "Jimmy décide d'affronter la flotte de front pour forcer un passage vers l'île centrale.",
                Dungeon=new DungeonData{ Name="Mer du Grand Vortex", Icon="🌀",
                    Difficulty=DifficultyLevel.Medium, RequiredRank=0, EnemyCount=7, GoldReward=200,
                    Rewards=new List<MaterialReward>{ new(){Material="Boussole Maudite",Min=1,Max=2}, new(){Material="Ancre Brisee",Min=1,Max=1}}}},
            new(){
                Act=2, ChapterNum=15, Title="Le Marché des Pirates Dimensionnels", Tag="One Piece",
                Summary="Kai infiltre un port clandestin où se négocient les fruits du démon volés.\n"+
                        "Le Sekai Sekai no Mi aurait été mis aux enchères.\n"+
                        "L'équipage doit le récupérer avant un mystérieux acheteur.",
                Dungeon=new DungeonData{ Name="Port Clandestin de Freewind", Icon="⚓",
                    Difficulty=DifficultyLevel.Medium, RequiredRank=0, EnemyCount=9, GoldReward=280,
                    Rewards=new List<MaterialReward>{ new(){Material="Jeton Pirate",Min=1,Max=3}, new(){Material="Carte Cryptee",Min=1,Max=2}}}},
            new(){
                Act=2, ChapterNum=16, Title="La Forteresse des Quatre Amiraux", Tag="One Piece",
                Summary="Quatre amiraux pirates dimensionnels occupent une citadelle insulaire.\n"+
                        "Chacun maîtrise une dimension différente.\n"+
                        "L'équipage doit les vaincre pour obtenir les clés de la Chambre du Fruit.",
                Dungeon=new DungeonData{ Name="Citadelle des Quatre Amiraux", Icon="🏰",
                    Difficulty=DifficultyLevel.Medium, RequiredRank=0, EnemyCount=10, GoldReward=350,
                    Rewards=new List<MaterialReward>{ new(){Material="Medaillon Amiral",Min=1,Max=2}, new(){Material="Eclat de Dimension",Min=1,Max=2}}}},
            new(){
                Act=2, ChapterNum=17, Title="Le Maelström de l'Au-delà", Tag="One Piece",
                Summary="Un maelström gigantesque aspire les dimensions vers un néant central.\n"+
                        "Les pirates élites du Cercle du Néant protègent ce gouffre\n"+
                        "où le Sekai Sekai no Mi aurait été caché.",
                Dungeon=new DungeonData{ Name="Maelström de l'Au-delà", Icon="🌊",
                    Difficulty=DifficultyLevel.Hard, RequiredRank=0, EnemyCount=10, GoldReward=350,
                    Rewards=new List<MaterialReward>{ new(){Material="Essence du Neant",Min=1,Max=2}, new(){Material="Larme de l'Abime",Min=1,Max=2}}}},
            new(){
                Act=2, ChapterNum=18, Title="L'Île Céleste des Pirates Libres", Tag="One Piece",
                Summary="Au sommet des nuages dimensionnels, une île flottante abrite la confrérie des Pirates Libres.\n"+
                        "Gardiens du vrai sens de la liberté, ils testent l'équipage dans des épreuves de combat aérien.",
                Dungeon=new DungeonData{ Name="Île Céleste Libertas", Icon="☁️",
                    Difficulty=DifficultyLevel.Hard, RequiredRank=0, EnemyCount=13, GoldReward=500,
                    Rewards=new List<MaterialReward>{ new(){Material="Plume de Vent Celeste",Min=1,Max=3}, new(){Material="Nuage Petrifie",Min=1,Max=2}}}},
            new(){
                Act=2, ChapterNum=19, Title="Le Roi des Pirates Dimensionnels", Tag="One Piece",
                Summary="Barbe-Dimension, le Roi auto-proclamé des mers interdites, révèle qu'il possède déjà le Sekai Sekai no Mi.\n"+
                        "Ryo Thunder et Sakura Storm s'unissent pour briser son armure dimensionnelle.",
                Dungeon=new DungeonData{ Name="Trône des Mers Interdites", Icon="💀",
                    Difficulty=DifficultyLevel.Boss, RequiredRank=0, EnemyCount=12, GoldReward=600,
                    Rewards=new List<MaterialReward>{ new(){Material="Couronne Brisee",Min=1,Max=2}, new(){Material="Ame de Pirate",Min=1,Max=3}}}},
            new(){
                Act=2, ChapterNum=20, Title="Le Sekai Sekai no Mi", Tag="One Piece",
                Summary="Le fruit légendaire est enfin à portée, mais une entité ancienne née de toutes les dimensions fusionnées surgit.\n"+
                        "L'équipage au complet doit s'unir dans un combat final pour la liberté absolue.",
                Dungeon=new DungeonData{ Name="Sanctuaire du Fruit du Monde", Icon="🍎",
                    Difficulty=DifficultyLevel.Boss, BossGauntlet=true, RequiredRank=0, EnemyCount=15, GoldReward=800,
                    Rewards=new List<MaterialReward>{ new(){Material="Sekai Sekai no Mi",Min=1,Max=1}, new(){Material="Larme de Liberte",Min=2,Max=4}}}},

            // ── ACT 3 — Naruto ────────────────────────────────────────────────
            new(){
                Act=3, ChapterNum=21, Title="Les Forêts de l'Éveil", Tag="Naruto",
                Summary="Kai et son équipage pénètrent dans les Forêts de l'Éveil, une étendue mystique où les arbres pulsent de chakra brut.\n"+
                        "Les éclaireurs du Syndicat du Néant ont posté des ninjas-ombres pour intercepter tout intrus.\n"+
                        "Kai doit apprendre à percevoir le flux du chakra pour naviguer les pièges invisibles.",
                Dungeon=new DungeonData{ Name="Forêt de l'Éveil Chakra", Icon="🌿",
                    Difficulty=DifficultyLevel.Medium, RequiredRank=0, EnemyCount=6, GoldReward=180,
                    Rewards=new List<MaterialReward>{ new(){Material="Feuille de Chakra",Min=1,Max=2}, new(){Material="Kunai Spectral",Min=1,Max=2}}}},
            new(){
                Act=3, ChapterNum=22, Title="Le Village Caché des Brumes", Tag="Naruto",
                Summary="L'équipage arrive au Village Caché des Brumes, une cité dimensionnelle suspendue entre deux plans.\n"+
                        "Le Syndicat du Néant y a infiltré les rangs des ninjas locaux, semant la méfiance.\n"+
                        "Kai doit démêler la trahison et neutraliser les agents avant que le village ne soit absorbé.",
                Dungeon=new DungeonData{ Name="Village des Brumes Éternelles", Icon="🌫️",
                    Difficulty=DifficultyLevel.Medium, RequiredRank=0, EnemyCount=8, GoldReward=240,
                    Rewards=new List<MaterialReward>{ new(){Material="Cristal de Brume",Min=1,Max=2}, new(){Material="Sceau Ninja Corrompu",Min=1,Max=2}}}},
            new(){
                Act=3, ChapterNum=23, Title="Le Défilé des Ombres", Tag="Naruto",
                Summary="Pour atteindre le premier temple de chakra, l'équipage doit traverser le Défilé des Ombres.\n"+
                        "Un canyon où la lumière est dévorée par des techniques de genjutsu puissantes.\n"+
                        "Kai doit percer les illusions et ouvrir le passage vers les terres sacrées.",
                Dungeon=new DungeonData{ Name="Défilé des Ombres Ninja", Icon="🌑",
                    Difficulty=DifficultyLevel.Medium, RequiredRank=0, EnemyCount=9, GoldReward=300,
                    Rewards=new List<MaterialReward>{ new(){Material="Plume d'Ombre",Min=1,Max=2}, new(){Material="Parchemin de Genjutsu",Min=1,Max=2}}}},
            new(){
                Act=3, ChapterNum=24, Title="Temple du Chakra de Feu", Tag="Naruto",
                Summary="Le Temple du Chakra de Feu est l'un des cinq piliers dimensionnels que le Syndicat cherche à corrompre.\n"+
                        "Des gardiens-ninjas enflammés et des disciples du Syndicat défendent l'autel sacré.\n"+
                        "L'équipage doit purifier le temple avant que sa flamme éternelle ne devienne balise du néant.",
                Dungeon=new DungeonData{ Name="Temple du Feu Éternel", Icon="🔥",
                    Difficulty=DifficultyLevel.Hard, RequiredRank=0, EnemyCount=9, GoldReward=300,
                    Rewards=new List<MaterialReward>{ new(){Material="Flamme de Chakra Pur",Min=1,Max=2}, new(){Material="Cendre de Ninjutsu",Min=1,Max=2}}}},
            new(){
                Act=3, ChapterNum=25, Title="La Citadelle des Vents Scellés", Tag="Naruto",
                Summary="Perchée au sommet de falaises dimensionnelles, la Citadelle des Vents Scellés abrite d'anciens parchemins interdits.\n"+
                        "Une escouade d'élite de ninjas-vent masqués a pris la citadelle d'assaut.\n"+
                        "Kai et ses alliés doivent les déloger et neutraliser le quatrième membre du Syndicat.",
                Dungeon=new DungeonData{ Name="Citadelle des Vents Scellés", Icon="💨",
                    Difficulty=DifficultyLevel.Hard, RequiredRank=0, EnemyCount=11, GoldReward=400,
                    Rewards=new List<MaterialReward>{ new(){Material="Parchemin du Vent Ancien",Min=1,Max=2}, new(){Material="Masque Brise du Syndicat",Min=1,Max=2}}}},
            new(){
                Act=3, ChapterNum=26, Title="Les Marais du Chakra Sombre", Tag="Naruto",
                Summary="Les Marais du Chakra Sombre sont contaminés par les expériences du Syndicat du Néant.\n"+
                        "Le chakra y est devenu un poison corrosif transformant les créatures en monstres instables.\n"+
                        "L'équipage doit retrouver un informateur prisonnier détenant la liste complète du Syndicat.",
                Dungeon=new DungeonData{ Name="Marais du Chakra Sombre", Icon="🟤",
                    Difficulty=DifficultyLevel.Hard, RequiredRank=0, EnemyCount=13, GoldReward=500,
                    Rewards=new List<MaterialReward>{ new(){Material="Boue de Chakra Toxique",Min=1,Max=2}, new(){Material="Antidote Ninja",Min=1,Max=2}}}},
            new(){
                Act=3, ChapterNum=27, Title="Le Sanctuaire des Cinq Dimensions", Tag="Naruto",
                Summary="Au cœur du Royaume des Chakras se dresse le Sanctuaire des Cinq Dimensions.\n"+
                        "Le Syndicat y a convoqué cinq membres pour lancer le rituel préliminaire de fusion.\n"+
                        "Kai affronte une bataille marathon pour défaire chaque membre l'un après l'autre.",
                Dungeon=new DungeonData{ Name="Sanctuaire des Cinq Dimensions", Icon="⭐",
                    Difficulty=DifficultyLevel.Boss, RequiredRank=0, EnemyCount=12, GoldReward=600,
                    Rewards=new List<MaterialReward>{ new(){Material="Eclat Dimensionnel",Min=1,Max=3}, new(){Material="Sphere du Neant Fragmentee",Min=1,Max=2}}}},
            new(){
                Act=3, ChapterNum=28, Title="La Tour des Âmes Ninja", Tag="Naruto",
                Summary="La Tour des Âmes Ninja est une prison dimensionnelle où le Syndicat emprisonne les chakras des ninjas tombés.\n"+
                        "Le commandant libère des âmes corrompues comme soldats fantômes.\n"+
                        "L'équipage doit gravir la tour étage par étage et affronter le commandant masqué au sommet.",
                Dungeon=new DungeonData{ Name="Tour des Âmes Ninja", Icon="🗼",
                    Difficulty=DifficultyLevel.Boss, RequiredRank=0, EnemyCount=15, GoldReward=800,
                    Rewards=new List<MaterialReward>{ new(){Material="Essence d'Ame Ninja",Min=1,Max=3}, new(){Material="Chaine de l'Emprisonnement",Min=1,Max=2}}}},
            new(){
                Act=3, ChapterNum=29, Title="Le Palais du Vide Absolu", Tag="Naruto",
                Summary="Kai pénètre dans le Palais du Vide Absolu, quartier général du Syndicat flottant entre les dimensions.\n"+
                        "Onze des douze membres masqués sont réunis pour le rituel final.\n"+
                        "L'équipage déclenche une offensive totale pendant que Kai avance vers le cœur du palais.",
                Dungeon=new DungeonData{ Name="Palais du Vide Absolu", Icon="🌑",
                    Difficulty=DifficultyLevel.Boss, RequiredRank=0, EnemyCount=15, GoldReward=850,
                    Rewards=new List<MaterialReward>{ new(){Material="Cristal du Neant Pur",Min=1,Max=3}, new(){Material="Sceau de Fusion Dimensionnelle",Min=1,Max=2}}}},
            new(){
                Act=3, ChapterNum=30, Title="Le Douzième Masque — Maître du Néant", Tag="Naruto",
                Summary="Le douzième membre du Syndicat se révèle : le Maître du Néant.\n"+
                        "Un être qui a sacrifié sa propre dimension pour fusionner toutes les réalités en un vide unifié.\n"+
                        "Kai doit canaliser tout le chakra accumulé pour briser le rituel et sauver le Royaume des Chakras.",
                Dungeon=new DungeonData{ Name="Nexus du Néant Primordial", Icon="👁️",
                    Difficulty=DifficultyLevel.Boss, BossGauntlet=true, RequiredRank=0, EnemyCount=18, GoldReward=1000,
                    Rewards=new List<MaterialReward>{ new(){Material="Coeur du Neant Brise",Min=1,Max=2}, new(){Material="Sceau Ultime des Dimensions",Min=1,Max=1}}}},

            // ── ACT 4 — One Punch Man ─────────────────────────────────────────
            new(){
                Act=4, ChapterNum=31, Title="L'Arène des Héros Brisés", Tag="One Punch Man",
                Summary="L'équipage arrive dans une dimension où des héros déchus s'affrontent pour regagner leur rang.\n"+
                        "Les associations héroïques se sont effondrées — seuls les plus forts survivent.\n"+
                        "Kai doit prouver sa valeur face aux chevaliers de classe S corrompus par le vide.",
                Dungeon=new DungeonData{ Name="Arène des Héros Déchus", Icon="🏟️",
                    Difficulty=DifficultyLevel.Hard, RequiredRank=0, EnemyCount=10, GoldReward=420,
                    Rewards=new List<MaterialReward>{ new(){Material="EclatFoudre",Min=1,Max=3}, new(){Material="CristalFeu",Min=1,Max=2}}}},
            new(){
                Act=4, ChapterNum=32, Title="La Ville en Ruines", Tag="One Punch Man",
                Summary="Une mégapole dimensionnelle a été rasée en quelques secondes par un être inconnu.\n"+
                        "Des monstres de classe Dragon rodent dans les décombres.\n"+
                        "L'équipage doit traverser la ville fantôme avant que la zone ne soit scellée pour toujours.",
                Dungeon=new DungeonData{ Name="Cité Zéro — Décombres Dimensionnels", Icon="🏚️",
                    Difficulty=DifficultyLevel.Hard, RequiredRank=0, EnemyCount=12, GoldReward=560,
                    Rewards=new List<MaterialReward>{ new(){Material="EssenceOmbres",Min=2,Max=4}, new(){Material="GemmeLunaire",Min=1,Max=2}}}},
            new(){
                Act=4, ChapterNum=33, Title="Les Associés du Néant", Tag="One Punch Man",
                Summary="Les derniers membres du Syndicat du Néant se sont alliés à des monstres de classe S.\n"+
                        "Ils cherchent à neutraliser Kai avant qu'il n'atteigne L'Absolu.\n"+
                        "Ryo Thunder et Kaito Shadow tiennent le front pendant que Kai perce la ligne ennemie.",
                Dungeon=new DungeonData{ Name="Quartier Général du Syndicat", Icon="🕶️",
                    Difficulty=DifficultyLevel.Hard, RequiredRank=0, EnemyCount=14, GoldReward=680,
                    Rewards=new List<MaterialReward>{ new(){Material="CristalNoir",Min=2,Max=3}, new(){Material="LarmePhoenix",Min=1,Max=2}}}},
            new(){
                Act=4, ChapterNum=34, Title="Le Titan de Métal", Tag="One Punch Man",
                Summary="Dans la dimension des Forges Maudites surgit Métal Colosse — un cyborg titan créé pour détruire quiconque approche L'Absolu.\n"+
                        "Ses armures régénèrent en temps réel.\n"+
                        "Seule une attaque à puissance absolue peut le vaincre.",
                Dungeon=new DungeonData{ Name="Dimension des Forges Maudites", Icon="⚙️",
                    Difficulty=DifficultyLevel.Boss, RequiredRank=0, EnemyCount=12, GoldReward=750,
                    Rewards=new List<MaterialReward>{ new(){Material="PierreCeleste",Min=1,Max=3}, new(){Material="EclatFoudre",Min=2,Max=3}}}},
            new(){
                Act=4, ChapterNum=35, Title="Le Roi des Monstres", Tag="One Punch Man",
                Summary="Gorava, Roi des Monstres, commande une armée issue des profondeurs du vide dimensionnel.\n"+
                        "Il a absorbé cent héros pour acquérir leurs pouvoirs combinés.\n"+
                        "Chaque coup porté nourrit sa puissance — il faut frapper vite, fort, une seule fois.",
                Dungeon=new DungeonData{ Name="Trône du Vide — Domaine de Gorava", Icon="👹",
                    Difficulty=DifficultyLevel.Boss, RequiredRank=0, EnemyCount=16, GoldReward=980,
                    Rewards=new List<MaterialReward>{ new(){Material="AmeDechue",Min=2,Max=4}, new(){Material="CristalNoir",Min=2,Max=3}}}},
            new(){
                Act=4, ChapterNum=36, Title="La Prophétie du Successeur", Tag="One Punch Man",
                Summary="L'Absolu envoie ses trois Gardiens — des êtres capables de détruire des étoiles.\n"+
                        "Chacun porte un fragment du Sceau du Successeur que Kai doit briser pour avancer.\n"+
                        "Pour la première fois, l'équipage sent qu'un seul coup ne suffira peut-être pas.",
                Dungeon=new DungeonData{ Name="Sanctuaire des Trois Gardiens", Icon="🔱",
                    Difficulty=DifficultyLevel.Boss, RequiredRank=0, EnemyCount=18, GoldReward=1150,
                    Rewards=new List<MaterialReward>{ new(){Material="EssenceOmbres",Min=2,Max=4}, new(){Material="PierreCeleste",Min=1,Max=3}}}},
            new(){
                Act=4, ChapterNum=37, Title="La Dimension des Géants", Tag="One Punch Man",
                Summary="Le portail s'ouvre sur une dimension où tout est démesuré.\n"+
                        "Des géants cosmiques comme simples soldats de L'Absolu patrouillent le ciel.\n"+
                        "Kai doit surpasser les limites du Système pour frapper à l'échelle de l'univers.",
                Dungeon=new DungeonData{ Name="Dimension Colossale — Champs des Titans", Icon="🌌",
                    Difficulty=DifficultyLevel.Legendary, RequiredRank=0, EnemyCount=15, GoldReward=1350,
                    Rewards=new List<MaterialReward>{ new(){Material="CristalNoir",Min=2,Max=4}, new(){Material="AmeDechue",Min=2,Max=3}}}},
            new(){
                Act=4, ChapterNum=38, Title="L'Écho de Puissance", Tag="One Punch Man",
                Summary="L'Absolu projette une réplique de lui-même à 10% de sa puissance réelle.\n"+
                        "Même cette ombre dépasse tout ce que l'équipage a affronté.\n"+
                        "Kai comprend : il cherche à comprendre la nature de l'Absolu, pas seulement à le vaincre.",
                Dungeon=new DungeonData{ Name="Arène Dimensionnelle — Épreuve de l'Écho", Icon="💜",
                    Difficulty=DifficultyLevel.Legendary, RequiredRank=0, EnemyCount=20, GoldReward=1750,
                    Rewards=new List<MaterialReward>{ new(){Material="PierreCeleste",Min=2,Max=4}, new(){Material="GemmeLunaire",Min=2,Max=3}}}},
            new(){
                Act=4, ChapterNum=39, Title="Le Jugement de l'Absolu", Tag="One Punch Man",
                Summary="L'Absolu observe depuis son trône de lumière vide. Il ne combat pas encore — il juge.\n"+
                        "Chaque ennemi envoyé est une question : pourquoi te bats-tu ?\n"+
                        "Kai répond par ses actes — protéger son équipage, pas régner sur les dimensions.",
                Dungeon=new DungeonData{ Name="Trône de Lumière Vide — Salle du Jugement", Icon="⚖️",
                    Difficulty=DifficultyLevel.Legendary, RequiredRank=0, EnemyCount=22, GoldReward=2000,
                    Rewards=new List<MaterialReward>{ new(){Material="AmeDechue",Min=2,Max=5}, new(){Material="EssenceOmbres",Min=2,Max=4}}}},
            new(){
                Act=4, ChapterNum=40, Title="L'Absolu : Un Seul Coup", Tag="One Punch Man",
                Summary="L'Absolu se lève. Un seul regard arrête le temps dans la Dimension Zéro.\n"+
                        "Kai concentre tout — Adaptation Infinie, Rasengan Dimensionnel, amour pour son équipage.\n"+
                        "Un seul coup. L'Absolu sourit pour la première fois depuis un millénaire. Le successeur est trouvé.",
                Dungeon=new DungeonData{ Name="Dimension Zéro — L'Épreuve Finale", Icon="👑",
                    Difficulty=DifficultyLevel.Legendary, BossGauntlet=true, RequiredRank=0, EnemyCount=25, GoldReward=2500,
                    Rewards=new List<MaterialReward>{ new(){Material="AmeDechue",Min=3,Max=5}, new(){Material="CristalNoir",Min=2,Max=4}}}},

            // ── ACT 5 — Grand Tour ────────────────────────────────────────────
            new(){
                Act=5, ChapterNum=41, Title="L'Éveil du Système Absolu", Tag="Solo Leveling",
                Summary="Le Système de Kai fusionne avec l'énergie des quatre mondes.\n"+
                        "Les gardiens du Néant surgissent de toutes les dimensions.\n"+
                        "Utilise l'Adaptation Infinie pour absorber leur puissance et ouvrir la voie.",
                Dungeon=new DungeonData{ Name="Nexus des Portails Fusionnés", Icon="🌀",
                    Difficulty=DifficultyLevel.Boss, RequiredRank=0, EnemyCount=15, GoldReward=2000,
                    Rewards=new List<MaterialReward>{ new(){Material="CristalNoir",Min=2,Max=4}, new(){Material="EssenceOmbres",Min=2,Max=4}}}},
            new(){
                Act=5, ChapterNum=42, Title="La Mer du Chaos Primordial", Tag="One Piece",
                Summary="Le Fruit du Monde ouvre un portail sur une mer entre les dimensions.\n"+
                        "L'équipage affronte les Amiraux du Vide qui veulent sceller le Grand Tour.\n"+
                        "Kai et Jimmy combattent bord à bord, haki et système unis.",
                Dungeon=new DungeonData{ Name="Mer du Chaos Primordial", Icon="🌊",
                    Difficulty=DifficultyLevel.Boss, RequiredRank=0, EnemyCount=17, GoldReward=2400,
                    Rewards=new List<MaterialReward>{ new(){Material="LarmePhoenix",Min=2,Max=4}, new(){Material="GemmeLunaire",Min=2,Max=3}}}},
            new(){
                Act=5, ChapterNum=43, Title="La Forêt des Chakras Corrompus", Tag="Naruto",
                Summary="Le Syndicat du Néant a corrompu le chakra de toute la forêt sacrée.\n"+
                        "Les ninjas des Ombres attaquent en vagues sans fin.\n"+
                        "Ryo maîtrise le Rasengan Dimensionnel et perce la barrière maudite.",
                Dungeon=new DungeonData{ Name="Forêt Sacrée Corrompue", Icon="🌿",
                    Difficulty=DifficultyLevel.Boss, RequiredRank=0, EnemyCount=20, GoldReward=3000,
                    Rewards=new List<MaterialReward>{ new(){Material="EclatFoudre",Min=3,Max=5}, new(){Material="CristalNoir",Min=2,Max=4}}}},
            new(){
                Act=5, ChapterNum=44, Title="Le Sanctuaire de l'Absolu", Tag="One Punch Man",
                Summary="Kai pénètre dans le Sanctuaire de l'Absolu — lieu hors du temps.\n"+
                        "Les Héros Fantômes, corrompus par le Néant, défendent le seuil.\n"+
                        "Un seul coup ne suffit plus : il faut transcender la limite de la Frappe Sérieuse.",
                Dungeon=new DungeonData{ Name="Sanctuaire de l'Absolu", Icon="👁️",
                    Difficulty=DifficultyLevel.Legendary, RequiredRank=0, EnemyCount=18, GoldReward=3000,
                    Rewards=new List<MaterialReward>{ new(){Material="AmeDechue",Min=3,Max=5}, new(){Material="PierreCeleste",Min=2,Max=4}}}},
            new(){
                Act=5, ChapterNum=45, Title="Le Rang SS du Système", Tag="Solo Leveling",
                Summary="Le Système révèle une classe cachée : Monarque Dimensionnel.\n"+
                        "Les Fantômes du Niveau Zéro tentent d'effacer Kai de l'existence.\n"+
                        "Il doit atteindre le rang SS avant que le portail central ne se ferme.",
                Dungeon=new DungeonData{ Name="Tour du Niveau Zéro", Icon="🏰",
                    Difficulty=DifficultyLevel.Legendary, RequiredRank=0, EnemyCount=22, GoldReward=4000,
                    Rewards=new List<MaterialReward>{ new(){Material="CristalNoir",Min=3,Max=5}, new(){Material="AmeDechue",Min=2,Max=4}}}},
            new(){
                Act=5, ChapterNum=46, Title="L'Île Hors du Monde", Tag="One Piece",
                Summary="Le Fruit du Monde conduit l'équipage à Raftel Dimensionnel.\n"+
                        "Les Empereurs du Vide ont scellé la vérité du Grand Tour.\n"+
                        "Kai et Sakura Storm déferlent comme une tempête sur les quatre Empereurs.",
                Dungeon=new DungeonData{ Name="Raftel Dimensionnel", Icon="🏴",
                    Difficulty=DifficultyLevel.Legendary, RequiredRank=0, EnemyCount=25, GoldReward=5000,
                    Rewards=new List<MaterialReward>{ new(){Material="LarmePhoenix",Min=3,Max=5}, new(){Material="EssenceOmbres",Min=3,Max=5}}}},
            new(){
                Act=5, ChapterNum=47, Title="La Vallée de la Fin Dimensionnelle", Tag="Naruto",
                Summary="Au bord de la Vallée de la Fin Dimensionnelle, deux forces s'affrontent.\n"+
                        "L'Avatar du Syndicat incarne tous les chakras volés.\n"+
                        "Kai libère le Rasengan Dimensionnel dans sa forme ultime — l'Explosion Suprême.",
                Dungeon=new DungeonData{ Name="Vallée de la Fin Dimensionnelle", Icon="💥",
                    Difficulty=DifficultyLevel.Legendary, RequiredRank=0, EnemyCount=20, GoldReward=5000,
                    Rewards=new List<MaterialReward>{ new(){Material="EclatFoudre",Min=3,Max=6}, new(){Material="CristalNoir",Min=3,Max=5}}}},
            new(){
                Act=5, ChapterNum=48, Title="Le Point d'Origine du Grand Tour", Tag="One Punch Man",
                Summary="Le Point d'Origine — là où tout a commencé — est envahi par les dieux du Vide.\n"+
                        "Ils sont omnipotents. Ils sont ennuyés. Ils veulent la destruction.\n"+
                        "Kai comprend que la puissance seule ne suffit pas : il faut un sens.",
                Dungeon=new DungeonData{ Name="Point d'Origine du Grand Tour", Icon="⭐",
                    Difficulty=DifficultyLevel.Legendary, RequiredRank=0, EnemyCount=25, GoldReward=6000,
                    Rewards=new List<MaterialReward>{ new(){Material="PierreCeleste",Min=3,Max=5}, new(){Material="AmeDechue",Min=3,Max=5}}}},
            new(){
                Act=5, ChapterNum=49, Title="La Dimension Zéro — Avant-Garde", Tag="Solo Leveling",
                Summary="La Dimension Zéro — vide absolu entre tous les mondes — s'ouvre.\n"+
                        "Les quatre pouvoirs fusionnent en Kai : Système, Fruit, Rasengan, Frappe Sérieuse.\n"+
                        "Il affronte l'Avant-Garde du Néant, armée de mille ombres.",
                Dungeon=new DungeonData{ Name="Dimension Zéro — Avant-Garde", Icon="🌑",
                    Difficulty=DifficultyLevel.Legendary, RequiredRank=0, EnemyCount=30, GoldReward=8000,
                    Rewards=new List<MaterialReward>{ new(){Material="AmeDechue",Min=4,Max=6}, new(){Material="CristalNoir",Min=3,Max=5}}}},
            new(){
                Act=5, ChapterNum=50, Title="Le Grand Tour Final — L'Absolu Renaît", Tag="One Piece",
                Summary="L'Absolu lui-même renaît sous une forme corrompue par le Néant.\n"+
                        "Kai, Jimmy, Sakura, Ryo — l'équipage entier unit ses forces pour la dernière fois.\n"+
                        "Un voyage. Une frappe. Un monde sauvé. Le Grand Tour est accompli.",
                Dungeon=new DungeonData{ Name="Trône de l'Absolu Renaissant", Icon="👑",
                    Difficulty=DifficultyLevel.Legendary, BossGauntlet=true, RequiredRank=0, EnemyCount=30, GoldReward=10000,
                    Rewards=new List<MaterialReward>{ new(){Material="AmeDechue",Min=4,Max=6}, new(){Material="PierreCeleste",Min=3,Max=5}}}},
            new(){
                Act=5, ChapterNum=51, Title="Les Gardiens de l'Éternité", Tag="Grand Tour",
                Summary="Après la renaissance de l'Absolu, Kai découvre qu'un dernier verrou protège la frontière entre les dimensions.\n"+
                        "Sept Gardiens de l'Éternité, oubliés depuis l'aube des mondes, surgissent pour tester le successeur légitime.\n"+
                        "L'équipage se sépare pour les affronter simultanément, avant que les portails ne se referment à jamais.",
                Dungeon=new DungeonData{ Name="Nexus des Sept Gardiens", Icon="⚔️",
                    Difficulty=DifficultyLevel.Legendary, RequiredRank=0, EnemyCount=28, GoldReward=9000,
                    Rewards=new List<MaterialReward>{ new(){Material="AmeDechue",Min=4,Max=6}, new(){Material="CristalNoir",Min=3,Max=5}}}},
            new(){
                Act=5, ChapterNum=52, Title="Le Voyage Sans Fin", Tag="Grand Tour",
                Summary="Kai, désormais Monarque Dimensionnel, comprend que chaque fin est un nouveau départ.\n"+
                        "De nouvelles dimensions s'ouvrent — plus vastes, plus dangereuses, plus merveilleuses.\n"+
                        "Le Tommy Mayo démarre une dernière fois. Le Grand Tour recommence, plus grand qu'avant.",
                Dungeon=new DungeonData{ Name="Portail de l'Infini", Icon="🌌",
                    Difficulty=DifficultyLevel.Legendary, BossGauntlet=true, RequiredRank=0, EnemyCount=35, GoldReward=12000,
                    Rewards=new List<MaterialReward>{ new(){Material="AmeDechue",Min=5,Max=8}, new(){Material="PierreCeleste",Min=4,Max=6}}}},
            new(){
                Act=5, ChapterNum=53, Title="L'Éveil du Cristal Primordial", Tag="Grand Tour",
                Summary="Au cœur d'une dimension cristalline jamais cartographiée, Kai perçoit une pulsation ancestrale.\n"+
                        "Le Cristal Primordial — source de tout chakra dans l'univers — est en train de se fissurer sous l'assaut de créatures nées du vide.\n"+
                        "L'équipage doit le défendre avant que sa destruction ne plonge tous les mondes dans un silence éternel.",
                Dungeon=new DungeonData{ Name="Sanctuaire du Cristal Primordial", Icon="💎",
                    Difficulty=DifficultyLevel.Legendary, RequiredRank=0, EnemyCount=28, GoldReward=11000,
                    Rewards=new List<MaterialReward>{ new(){Material="PierreCeleste",Min=4,Max=6}, new(){Material="AmeDechue",Min=3,Max=5}}}},
            new(){
                Act=5, ChapterNum=54, Title="L'Horizon des Mondes Inversés", Tag="Grand Tour",
                Summary="Une fissure dimensionnelle inverse les lois de la réalité : les alliés deviennent ennemis, les ombres prennent vie et le sol devient ciel.\n"+
                        "Kai doit affronter son propre reflet corrompu — le Monarque des Mondes Inversés — dans un duel où chaque frappe retourne contre lui.\n"+
                        "Seule la maîtrise absolue du chakra peut briser le miroir et refermer la fissure pour toujours.",
                Dungeon=new DungeonData{ Name="Miroir des Mondes Inversés", Icon="🌀",
                    Difficulty=DifficultyLevel.Legendary, BossGauntlet=true, RequiredRank=0, EnemyCount=32, GoldReward=13000,
                    Rewards=new List<MaterialReward>{ new(){Material="AmeDechue",Min=5,Max=8}, new(){Material="CristalNoir",Min=4,Max=6}}}},
        };

        // Chapters grouped by act (act index 0-4 → chapters 0-9, 10-19, 20-29, 30-39, 40-49)
        static readonly int[] ActStartIndex = { 0, 10, 20, 30, 40 };
        static readonly int[] ActChapterCount = { 10, 10, 10, 10, 14 };

        // Index de chapitre à ouvrir (positionné par TravelTourGame après une victoire)
        public static int RequestedChapterIdx = -1;

        // ──────────────────────────────────────────────────────────────────────
        public StoryState(TravelTourGame game) => _game = game;

        public void Load(Texture2D pixel, SpriteFontBase font, SpriteFontBase bigFont)
        {
            _pixel = pixel; _font = font; _bigFont = bigFont;
            int W = _game.GraphicsDevice.Viewport.Width;
            _backBtn = new UIButton(new Rectangle(16, 16, 110, 36), "← Menu",
                () => _game.ChangeState(GameState.MainMenu));
            // Build act tab buttons
            _actBtns.Clear();
            string[] actLabels = { "Acte I", "Acte II", "Acte III", "Acte IV", "Acte V" };
            int tabW = 120, tabH = 34, tabY = 60;
            int totalW = 5 * tabW + 4 * 8;
            int startX = W / 2 - totalW / 2;
            for (int a = 0; a < 5; a++)
            {
                int ai = a;
                _actBtns.Add(new UIButton(
                    new Rectangle(startX + ai * (tabW + 8), tabY, tabW, tabH),
                    actLabels[ai],
                    () => { _selectedAct = ai; _selectedChapterInAct = 0; RebuildNavButtons(); }
                ));
            }

            // Ouvre au chapitre demandé (après victoire en donjon), sinon reprend le dernier chapitre consulté
            int idx = System.Math.Clamp(
                RequestedChapterIdx >= 0 ? RequestedChapterIdx : PlayerSave.LastChapterIndex, 0, 51);
            for (int a = 0; a < ActStartIndex.Length - 1; a++)
            {
                if (idx < ActStartIndex[a + 1])
                {
                    _selectedAct          = a;
                    _selectedChapterInAct = idx - ActStartIndex[a];
                    break;
                }
            }
            if (idx >= ActStartIndex[ActStartIndex.Length - 1])
            {
                _selectedAct          = ActStartIndex.Length - 1;
                _selectedChapterInAct = idx - ActStartIndex[_selectedAct];
            }
            RequestedChapterIdx = -1;

            RebuildNavButtons();
        }

        void RebuildNavButtons()
        {
            int W = _game.GraphicsDevice.Viewport.Width;
            int H = _game.GraphicsDevice.Viewport.Height;
            int count = ActChapterCount[_selectedAct];
            _prevBtn = new UIButton(new Rectangle(60, H - 80, 100, 40), "◀ Préc",
                () => { if (_selectedChapterInAct > 0) { _selectedChapterInAct--; RebuildNavButtons(); } },
                UIHelper.Dark2, new Color(30, 30, 60));
            _nextBtn = new UIButton(new Rectangle(W - 160, H - 80, 100, 40), "Suivant ▶",
                () => { if (_selectedChapterInAct < count - 1) { _selectedChapterInAct++; RebuildNavButtons(); } },
                UIHelper.Dark2, new Color(30, 30, 60));
            int globalIdx = ActStartIndex[_selectedAct] + _selectedChapterInAct;
            if (PlayerSave.LastChapterIndex != globalIdx)
            {
                PlayerSave.LastChapterIndex = globalIdx;
                SaveSystem.Save();
            }
            var ch = Chapters[globalIdx];
            bool done = ChaptersCompleted[globalIdx];
            _fightBtn = new UIButton(
                new Rectangle(W / 2 - 150, H - 80, 300, 52),
                done ? "✔ REJOUER" : "⚔  COMBATTRE",
                () => { ch.Dungeon.StoryActIndex = ch.Act - 1; _game.StartStoryDungeon(ch.Dungeon, globalIdx); },
                done ? new Color(10, 40, 10) : new Color(70, 0, 0),
                done ? new Color(20, 60, 20) : new Color(120, 10, 10)
            )
            { TextColor = done ? Color.LightGreen : Color.White };
        }

        bool IsChapterUnlocked(int globalIdx)
        {
            if (globalIdx == 0) return true;
            return ChaptersCompleted[globalIdx - 1];
        }

        public void Update(GameTime gt)
        {
            _time += (float)gt.ElapsedGameTime.TotalSeconds;
            var ms = Mouse.GetState();
            _backBtn.Update(ms);
            for (int i = 0; i < _actBtns.Count; i++) _actBtns[i].Update(ms);
            _prevBtn?.Update(ms);
            _nextBtn?.Update(ms);
            int globalIdx = ActStartIndex[_selectedAct] + _selectedChapterInAct;
            if (IsChapterUnlocked(globalIdx)) _fightBtn?.Update(ms);
            if (Keyboard.GetState().IsKeyDown(Keys.Escape)) _game.ChangeState(GameState.MainMenu);
        }

        // Returns current act accent color (handles rainbow for act 5)
        Color GetActAccent(int actIdx)
        {
            if (actIdx != 4) return ActAccent[actIdx];
            // Rainbow cycling for act 5
            float t = _time * 0.8f;
            float r = (float)System.Math.Sin(t) * 0.5f + 0.5f;
            float g = (float)System.Math.Sin(t + 2.094f) * 0.5f + 0.5f;
            float b = (float)System.Math.Sin(t + 4.189f) * 0.5f + 0.5f;
            return new Color(r, g, b);
        }

        public void Draw(SpriteBatch sb)
        {
            int W = _game.GraphicsDevice.Viewport.Width;
            int H = _game.GraphicsDevice.Viewport.Height;
            int globalIdx = ActStartIndex[_selectedAct] + _selectedChapterInAct;
            var ch = Chapters[globalIdx];
            Color accent = GetActAccent(_selectedAct);
            Color banner = ActBanner[_selectedAct];

            // Background
            sb.Draw(_pixel, new Rectangle(0, 0, W, H), UIHelper.Dark);
            sb.Draw(_pixel, new Rectangle(0, 0, W, H), banner * 0.7f);

            // Act-specific animated background effects
            DrawActBackground(sb, W, H, accent);

            // Back button + title
            _backBtn.Draw(sb, _pixel, _font, 0.85f);
            UIHelper.DrawCenteredText(sb, _bigFont, "📖  HISTOIRE — TRAVEL TOUR",
                new Rectangle(0, 14, W, 46), UIHelper.Purple, 0.7f);

            // Act tab buttons
            for (int a = 0; a < _actBtns.Count; a++)
            {
                bool sel = a == _selectedAct;
                Color tabAccent = GetActAccent(a);
                _actBtns[a].NormalColor = sel ? tabAccent * 0.25f : UIHelper.CardBg;
                _actBtns[a].TextColor = sel ? tabAccent : UIHelper.TextDim;
                _actBtns[a].Draw(sb, _pixel, _font, 0.78f);
                // Completion indicator: check if all chapters in act are done
                int aStart = ActStartIndex[a], aCount = ActChapterCount[a];
                bool allDone = true;
                for (int ci = 0; ci < aCount; ci++) if (!ChaptersCompleted[aStart + ci]) { allDone = false; break; }
                if (allDone) sb.DrawString(_font, "✔",
                    new Vector2(_actBtns[a].Bounds.Right - 18, _actBtns[a].Bounds.Y + 6), Color.LightGreen);
            }

            // Chapter card
            int cx = 50, cy = 107, cw = W - 100, cH = H - 205;
            UIHelper.DrawBox(sb, _pixel, new Rectangle(cx, cy, cw, cH), UIHelper.Dark2, accent * 0.5f, 2);

            // Header band
            sb.Draw(_pixel, new Rectangle(cx, cy, cw, 68), accent * 0.15f);
            sb.Draw(_pixel, new Rectangle(cx, cy + 66, cw, 2), accent);

            // Act badge + tag
            string actLabel = $"Acte {ch.Act}";
            var tsz = _font.MeasureString(actLabel);
            UIHelper.DrawBox(sb, _pixel,
                new Rectangle(cx + 14, cy + 12, (int)tsz.X + 20, 26),
                accent * 0.2f, accent, 1);
            sb.DrawString(_font, actLabel, new Vector2(cx + 24, cy + 15), accent);

            string tagStr = $"[ {ch.Tag} ]";
            var tsz2 = _font.MeasureString(tagStr);
            sb.DrawString(_font, tagStr,
                new Vector2(cx + 14 + (int)tsz.X + 30, cy + 15), accent * 0.7f);

            // Chapter number + title
            string chapLabel = $"Chapitre {ch.ChapterNum}";
            sb.DrawString(_font, chapLabel, new Vector2(cx + 14, cy + 74), accent * 0.8f);
            sb.DrawString(_bigFont, ch.Title, new Vector2(cx + 14, cy + 96), accent);

            // Dungeon info
            int infoY = cy + 148;
            sb.Draw(_pixel, new Rectangle(cx + 14, infoY - 4, cw - 28, 1), UIHelper.TextDim * 0.25f);
            Color dc = UIHelper.DifficultyColor(ch.Dungeon.Difficulty);
            sb.DrawString(_font, $"  {ch.Dungeon.Icon}  {ch.Dungeon.Name}",
                new Vector2(cx + 14, infoY + 4), UIHelper.TextMain);
            sb.DrawString(_font, $"Difficulté : {UIHelper.DifficultyName(ch.Dungeon.Difficulty)}",
                new Vector2(cx + 14, infoY + 28), dc);
            sb.DrawString(_font, $"Ennemis : {ch.Dungeon.EnemyCount}",
                new Vector2(cx + 260, infoY + 28), UIHelper.TextDim);
            for (int i = 0; i < 5; i++)
                sb.DrawString(_font, i <= (int)ch.Dungeon.Difficulty ? "★" : "☆",
                    new Vector2(cx + cw - 115 + i * 18, infoY + 26),
                    i <= (int)ch.Dungeon.Difficulty ? dc : UIHelper.TextDim * 0.3f);

            // Summary
            int sumY = infoY + 64;
            sb.Draw(_pixel, new Rectangle(cx + 14, sumY - 6, cw - 28, 1), UIHelper.TextDim * 0.2f);
            string[] lines = ch.Summary.Split('\n');
            for (int i = 0; i < lines.Length; i++)
                sb.DrawString(_font, lines[i],
                    new Vector2(cx + 22, sumY + i * 28), UIHelper.TextMain * 0.95f);

            // Rewards
            int ry = sumY + lines.Length * 28 + 14;
            sb.Draw(_pixel, new Rectangle(cx + 14, ry, cw - 28, 1), UIHelper.TextDim * 0.2f);
            sb.DrawString(_font, "Récompenses :", new Vector2(cx + 22, ry + 8), UIHelper.Gold);
            sb.DrawString(_font, $"💰 {ch.Dungeon.GoldReward} or",
                new Vector2(cx + 22, ry + 30), UIHelper.Gold);
            int rx2 = cx + 160;
            for (int i = 0; i < ch.Dungeon.Rewards.Count; i++)
            {
                var r = ch.Dungeon.Rewards[i];
                sb.DrawString(_font, $"+ {r.Material} ×{r.Min}-{r.Max}",
                    new Vector2(rx2, ry + 30), UIHelper.TextDim);
                rx2 += 200;
            }

            // Progression indicator
            int count = ActChapterCount[_selectedAct];
            string progStr = $"Chapitre {_selectedChapterInAct + 1}/{count} de l'Acte {_selectedAct + 1}";
            var pSz = _font.MeasureString(progStr);
            sb.DrawString(_font, progStr,
                new Vector2(W / 2f - pSz.X / 2f, H - 142), UIHelper.TextDim * 0.75f);

            // Pulsing border accent
            float pulse = (float)System.Math.Sin(_time * 2.5f) * 0.3f + 0.7f;
            sb.Draw(_pixel, new Rectangle(cx, cy, cw, 2), accent * pulse);
            sb.Draw(_pixel, new Rectangle(cx, cy + cH - 2, cw, 2), accent * pulse);

            // Lock overlay if not unlocked
            bool unlocked = IsChapterUnlocked(globalIdx);
            if (!unlocked)
            {
                sb.Draw(_pixel, new Rectangle(W / 2 - 200, H - 92, 400, 60), Color.Black * 0.6f);
                UIHelper.DrawCenteredText(sb, _font, "🔒 Terminez le chapitre précédent pour débloquer",
                    new Rectangle(W / 2 - 240, H - 85, 480, 46), UIHelper.TextDim, 0.82f);
            }
            else
            {
                _fightBtn?.Draw(sb, _pixel, _bigFont, 0.62f);
            }

            // Navigation buttons
            _prevBtn?.Draw(sb, _pixel, _font, 0.8f);
            _nextBtn?.Draw(sb, _pixel, _font, 0.8f);

            // Help text
            sb.DrawString(_font, "◀ ▶ Naviguer  |  Onglets : Actes  |  ESC Menu",
                new Vector2(W / 2f - 150, H - 26), UIHelper.TextDim * 0.5f);
        }

        void DrawActBackground(SpriteBatch sb, int W, int H, Color accent)
        {
            switch (_selectedAct)
            {
                case 0: // Solo Leveling — scan lines bleues
                    for (int i = 0; i < 8; i++)
                    {
                        float off = (_time * 35f + i * 130) % (H + 20);
                        sb.Draw(_pixel, new Rectangle(0, (int)off, W, 2), new Color(0, 200, 255) * 0.06f);
                    }
                    break;
                case 1: // One Piece — vagues dorées
                    for (int i = 0; i < 5; i++)
                    {
                        float wave = (float)System.Math.Sin(_time * 1.2f + i * 1.3f) * 30f;
                        float yPos = H * 0.6f + i * 40f + wave;
                        sb.Draw(_pixel, new Rectangle(0, (int)yPos, W, 3), new Color(240, 192, 64) * 0.07f);
                    }
                    break;
                case 2: // Naruto — feuilles tombantes (rectangles orange)
                    for (int i = 0; i < 12; i++)
                    {
                        float fallY = (_time * 50f * (0.5f + i * 0.12f) + i * 87f) % (H + 30);
                        float fallX = (float)System.Math.Sin(_time * 0.7f + i * 0.9f) * 40f + (W / 12f) * i;
                        sb.Draw(_pixel, new Rectangle((int)fallX, (int)fallY, 6, 10),
                            new Color(255, 128, 64) * 0.18f);
                    }
                    break;
                case 3: // One Punch Man — éclairs verticaux violets
                    if ((int)(_time * 3f) % 4 == 0)
                    {
                        int lx = (int)((_time * 97f) % W);
                        sb.Draw(_pixel, new Rectangle(lx, 0, 2, H), new Color(168, 85, 247) * 0.12f);
                        sb.Draw(_pixel, new Rectangle(lx + 8, 0, 1, H), new Color(168, 85, 247) * 0.07f);
                    }
                    for (int i = 0; i < 4; i++)
                    {
                        float off = (_time * 60f + i * 200f) % (H + 20);
                        sb.Draw(_pixel, new Rectangle(0, (int)off, W, 1), new Color(168, 85, 247) * 0.05f);
                    }
                    break;
                case 4: // Act 5 — pulsing multicolor bands
                    for (int i = 0; i < 6; i++)
                    {
                        float t2 = _time * 0.6f + i * 1.05f;
                        float r = (float)System.Math.Sin(t2) * 0.5f + 0.5f;
                        float g = (float)System.Math.Sin(t2 + 2.094f) * 0.5f + 0.5f;
                        float b2 = (float)System.Math.Sin(t2 + 4.189f) * 0.5f + 0.5f;
                        float off = (_time * 45f + i * 160f) % (H + 20);
                        sb.Draw(_pixel, new Rectangle(0, (int)off, W, 3),
                            new Color(r, g, b2) * 0.08f);
                    }
                    break;
            }
        }

        public void Dispose() { }
    }

    public class StoryChapter
    {
        public int Act;
        public int ChapterNum;
        public string Title = "", Tag = "", Summary = "";
        public DungeonData Dungeon = null!;
    }
}
