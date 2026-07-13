using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace TravelTour.Core
{
    // Simple JSON-like save system without external dependencies
    public static class SaveSystem
    {
        static string SavePath => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "TravelTour", "save.dat");

        public static void Save()
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(SavePath)!);
                var sb = new StringBuilder();
                sb.AppendLine($"gold={PlayerSave.Gold}");
                sb.AppendLine($"level={PlayerSave.PlayerLevel}");
                sb.AppendLine($"levelxp={PlayerSave.LevelXp}");
                sb.AppendLine($"statmelee={PlayerSave.StatMelee}");
                sb.AppendLine($"statdefense={PlayerSave.StatDefense}");
                sb.AppendLine($"statsword={PlayerSave.StatSword}");
                sb.AppendLine($"statfruit={PlayerSave.StatFruit}");
                sb.AppendLine($"statspeed={PlayerSave.StatSpeed}");
                sb.AppendLine($"freepoints={PlayerSave.FreeStatPoints}");
                sb.AppendLine($"classname={PlayerSave.PlayerClassName}");
                sb.AppendLine($"classicon={PlayerSave.PlayerClassIcon}");
                sb.AppendLine($"classdone={PlayerSave.ClassDungeonDone}");
                sb.AppendLine($"background={PlayerSave.SelectedBackground}");
                sb.AppendLine($"team={string.Join(",", PlayerSave.CurrentTeam ?? new string[3])}");
                foreach (var kv in PlayerSave.Materials)
                    sb.AppendLine($"mat:{kv.Key}={kv.Value}");
                foreach (var w in PlayerSave.OwnedWeapons)
                    sb.AppendLine($"weapon:{w}");
                foreach (var c in PlayerSave.OwnedChars)
                    sb.AppendLine($"char:{c}");
                foreach (var v in PlayerSave.OwnedVehicles)
                    sb.AppendLine($"vehicle:{v}");
                // Fruits possédés + équipé
                foreach (var f in Catalog.Fruits)
                    if (f.IsOwned) sb.AppendLine($"fruit:{f.Name}");
                // Maîtrise des fruits et des personnages
                foreach (var f in Catalog.Fruits)
                    if (f.Mastery > 0) sb.AppendLine($"fruitmastery:{f.Name}={f.Mastery}");
                foreach (var c in Catalog.Characters)
                    if (c.Mastery > 0) sb.AppendLine($"charmastery:{c.Name}={c.Mastery}");
                if (PlayerSave.EquippedFruitName != null)
                    sb.AppendLine($"equip_fruit={PlayerSave.EquippedFruitName}");
                if (PlayerSave.EquippedWeaponName != null)
                    sb.AppendLine($"equip_weapon={PlayerSave.EquippedWeaponName}");
                if (PlayerSave.EquippedVehicleName != null)
                    sb.AppendLine($"equip_vehicle={PlayerSave.EquippedVehicleName}");
                for (int i = 0; i < PlayerSave.StoryProgress.Length; i++)
                    sb.AppendLine($"story:{i}={PlayerSave.StoryProgress[i]}");
                sb.AppendLine($"lastchapter={PlayerSave.LastChapterIndex}");
                // Artefacts possédés + équipés
                foreach (var a in Catalog.Artifacts)
                    if (a.IsOwned) sb.AppendLine($"artifact:{a.Name}");
                foreach (var kv in PlayerSave.EquippedArtifactBySlot)
                    sb.AppendLine($"equip_artifact:{kv.Key}={kv.Value}");
                // Quêtes : progression / réclamation
                foreach (var q in Catalog.Quests)
                    if (q.IsCompleted || q.RewardClaimed)
                        sb.AppendLine($"quest:{q.Name}={q.IsCompleted}|{q.RewardClaimed}");
                // Événement Monde : îles, PNJ, pêche
                foreach (var isl in PlayerSave.VisitedIslands)
                    sb.AppendLine($"islandvisited:{isl}");
                foreach (var npc in PlayerSave.NpcsMet)
                    sb.AppendLine($"npcmet:{npc}");
                sb.AppendLine($"fishcaught={PlayerSave.FishCaught}");
                sb.AppendLine($"legendaryfish={PlayerSave.LegendaryFishCaught}");
                sb.AppendLine($"absoludefeated={PlayerSave.AbsoluDefeated}");
                foreach (var r in PlayerSave.OwnedFishingRods)
                    sb.AppendLine($"rod:{r}");
                if (PlayerSave.EquippedFishingRod != null)
                    sb.AppendLine($"equip_rod={PlayerSave.EquippedFishingRod}");
                foreach (var kv in PlayerSave.FishInventory)
                    sb.AppendLine($"fish:{kv.Key}={kv.Value}");
                File.WriteAllText(SavePath, sb.ToString());
            }
            catch { /* Silently fail if can't write */ }
        }

        public static void Load()
        {
            if (!File.Exists(SavePath)) return;
            try
            {
                foreach (var raw in File.ReadAllLines(SavePath))
                {
                    var line = raw.Trim();
                    if (line.StartsWith("gold="))          PlayerSave.Gold = int.Parse(line[5..]);
                    else if (line.StartsWith("level="))    PlayerSave.PlayerLevel = System.Math.Max(1, int.Parse(line[6..]));
                    else if (line.StartsWith("levelxp="))  PlayerSave.LevelXp = int.Parse(line[8..]);
                    else if (line.StartsWith("statmelee="))   PlayerSave.StatMelee   = int.Parse(line[10..]);
                    else if (line.StartsWith("statdefense=")) PlayerSave.StatDefense = int.Parse(line[12..]);
                    else if (line.StartsWith("statsword="))   PlayerSave.StatSword   = int.Parse(line[10..]);
                    else if (line.StartsWith("statfruit="))   PlayerSave.StatFruit   = int.Parse(line[10..]);
                    else if (line.StartsWith("statspeed="))   PlayerSave.StatSpeed   = int.Parse(line[10..]);
                    else if (line.StartsWith("freepoints="))  PlayerSave.FreeStatPoints   = int.Parse(line[11..]);
                    else if (line.StartsWith("classname="))   PlayerSave.PlayerClassName  = line[10..];
                    else if (line.StartsWith("classicon="))   PlayerSave.PlayerClassIcon  = line[10..];
                    else if (line.StartsWith("classdone="))   PlayerSave.ClassDungeonDone = line[10..] == "True";
                    else if (line.StartsWith("background=")) PlayerSave.SelectedBackground = line[11..];
                    else if (line.StartsWith("team="))
                    {
                        var parts = line[5..].Split(',');
                        for (int i = 0; i < 3 && i < parts.Length; i++)
                            PlayerSave.CurrentTeam[i] = parts[i] == "" ? null! : parts[i];
                    }
                    else if (line.StartsWith("mat:"))
                    {
                        var eq = line.IndexOf('=');
                        var key = line[4..eq];
                        PlayerSave.Materials[key] = int.Parse(line[(eq+1)..]);
                    }
                    else if (line.StartsWith("weapon:"))      PlayerSave.OwnedWeapons.Add(line[7..]);
                    else if (line.StartsWith("char:"))        PlayerSave.OwnedChars.Add(line[5..]);
                    else if (line.StartsWith("vehicle:"))     PlayerSave.OwnedVehicles.Add(line[8..]);
                    else if (line.StartsWith("fruit:"))       PlayerSave.OwnedFruits.Add(line[6..]);
                    else if (line.StartsWith("fruitmastery:"))
                    {
                        var eq = line.IndexOf('=');
                        var name = line[13..eq];
                        var f = Catalog.Fruits.Find(fr => fr.Name == name);
                        if (f != null) f.Mastery = int.Parse(line[(eq + 1)..]);
                    }
                    else if (line.StartsWith("charmastery:"))
                    {
                        var eq = line.IndexOf('=');
                        var name = line[12..eq];
                        var c = Catalog.Characters.Find(ch => ch.Name == name);
                        if (c != null) c.Mastery = int.Parse(line[(eq + 1)..]);
                    }
                    else if (line.StartsWith("equip_fruit=")) PlayerSave.EquippedFruitName = line[12..];
                    else if (line.StartsWith("equip_weapon=")) PlayerSave.EquippedWeaponName = line[13..];
                    else if (line.StartsWith("equip_vehicle=")) PlayerSave.EquippedVehicleName = line[14..];
                    else if (line.StartsWith("story:"))
                    {
                        var eq = line.IndexOf('=');
                        int idx = int.Parse(line[6..eq]);
                        if (idx < PlayerSave.StoryProgress.Length)
                            PlayerSave.StoryProgress[idx] = line[(eq+1)..] == "True";
                    }
                    else if (line.StartsWith("lastchapter=")) PlayerSave.LastChapterIndex = int.Parse(line[12..]);
                    else if (line.StartsWith("artifact:"))    { var n = line[9..]; var a = Catalog.Artifacts.Find(x => x.Name == n); if (a != null) a.IsOwned = true; }
                    else if (line.StartsWith("equip_artifact:"))
                    {
                        var eq = line.IndexOf('=');
                        var slot = line[15..eq];
                        var name = line[(eq+1)..];
                        PlayerSave.EquippedArtifactBySlot[slot] = name;
                        var a = Catalog.Artifacts.Find(x => x.Name == name);
                        if (a != null) a.IsEquipped = true;
                    }
                    else if (line.StartsWith("quest:"))
                    {
                        var eq = line.IndexOf('=');
                        var name = line[6..eq];
                        var parts = line[(eq+1)..].Split('|');
                        var q = Catalog.Quests.Find(x => x.Name == name);
                        if (q != null && parts.Length == 2)
                        {
                            q.IsCompleted   = parts[0] == "True";
                            q.RewardClaimed = parts[1] == "True";
                        }
                    }
                    else if (line.StartsWith("islandvisited:")) PlayerSave.VisitedIslands.Add(line[14..]);
                    else if (line.StartsWith("npcmet:"))        PlayerSave.NpcsMet.Add(line[7..]);
                    else if (line.StartsWith("fishcaught="))    PlayerSave.FishCaught = int.Parse(line[11..]);
                    else if (line.StartsWith("legendaryfish=")) PlayerSave.LegendaryFishCaught = int.Parse(line[14..]);
                    else if (line.StartsWith("absoludefeated=")) PlayerSave.AbsoluDefeated = line[15..] == "True";
                    else if (line.StartsWith("rod:"))
                    {
                        var n = line[4..];
                        if (!PlayerSave.OwnedFishingRods.Contains(n)) PlayerSave.OwnedFishingRods.Add(n);
                    }
                    else if (line.StartsWith("equip_rod="))     PlayerSave.EquippedFishingRod = line[10..];
                    else if (line.StartsWith("fish:"))
                    {
                        var eq = line.IndexOf('=');
                        var key = line[5..eq];
                        PlayerSave.FishInventory[key] = int.Parse(line[(eq+1)..]);
                    }
                }
                // Sync story progress to StoryState
                for (int i = 0; i < PlayerSave.StoryProgress.Length; i++)
                    if (PlayerSave.StoryProgress[i])
                        States.StoryState.MarkCompleted(i);
                // Sync owned weapons/chars to catalog
                foreach (var name in PlayerSave.OwnedWeapons)
                {
                    var w = Catalog.Weapons.Find(x => x.Name == name);
                    if (w != null) w.IsOwned = true;
                }
                foreach (var name in PlayerSave.OwnedChars)
                {
                    var c = Catalog.Characters.Find(x => x.Name == name);
                    if (c != null) c.IsOwned = true;
                }
                foreach (var name in PlayerSave.OwnedVehicles)
                {
                    var v = Catalog.Vehicles.Find(x => x.Name == name);
                    if (v != null) v.IsOwned = true;
                }
                // Fruits
                foreach (var name in PlayerSave.OwnedFruits)
                {
                    var f = Catalog.Fruits.Find(x => x.Name == name);
                    if (f != null) f.IsOwned = true;
                }
                if (PlayerSave.EquippedFruitName != null)
                    PlayerSave.EquipFruit(PlayerSave.EquippedFruitName);
                if (PlayerSave.EquippedWeaponName != null)
                    PlayerSave.EquipWeapon(PlayerSave.EquippedWeaponName);
                // Cannes à pêche
                foreach (var name in PlayerSave.OwnedFishingRods)
                {
                    var r = Catalog.FishingRods.Find(x => x.Name == name);
                    if (r != null) r.IsOwned = true;
                }
                if (PlayerSave.EquippedFishingRod != null)
                {
                    var er = Catalog.FishingRods.Find(x => x.Name == PlayerSave.EquippedFishingRod);
                    if (er != null) er.IsEquipped = true;
                }
                if (PlayerSave.EquippedVehicleName != null)
                    PlayerSave.EquipVehicle(PlayerSave.EquippedVehicleName);
            }
            catch { /* Corrupt save — ignore */ }
        }
    }
}
