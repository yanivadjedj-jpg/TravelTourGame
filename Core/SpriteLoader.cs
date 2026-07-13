using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using System.IO;

namespace TravelTour.Core
{
    public static class SpriteLoader
    {
        static GraphicsDevice _gd = null!;
        static readonly Dictionary<string, Texture2D?> _cache = new();

        static readonly string _dir = Path.Combine(
            System.AppDomain.CurrentDomain.BaseDirectory,
            "..", "..", "..", "Content", "Sprites", "generated");

        public static void Init(GraphicsDevice gd) => _gd = gd;

        public static Texture2D? Get(string name)
        {
            if (_cache.TryGetValue(name, out var cached)) return cached;

            var path = Path.Combine(_dir, name + ".png");
            if (!File.Exists(path))
            {
                _cache[name] = null;
                return null;
            }
            try
            {
                var tex = Texture2D.FromFile(_gd, path);
                _cache[name] = tex;
                return tex;
            }
            catch { _cache[name] = null; return null; }
        }

        // Personnages — mappe nom → fichier sprite
        static readonly System.Collections.Generic.Dictionary<string, string> _charMap = new()
        {
            { "Jimmy",        "char_jimmy"  },
            { "Kaito Shadow", "char_kaito"  },
            { "Ryo Thunder",  "char_ryo"    },
            { "Sakura Storm", "char_sakura" },
            { "Nova Blaze",   "char_nova"   },
            { "Void Walker",  "char_void"   },
            { "Lion Céleste", "char_lion"   },
            { "Dragon Fist",   "char_dragon"  },
            { "Zephyr Storm",  "char_zephyr"  },
            { "Eclipse",       "char_eclipse" },
        };

        public static Texture2D? Character(string name)
        {
            if (_charMap.TryGetValue(name, out var key)) return Get(key);
            return null;
        }

        // Raccourcis
        public static Texture2D? Player(bool attacking)
        {
            // Sprite du vrai leader (CurrentTeam[0])
            string? leaderName = PlayerSave.CurrentTeam[0];
            if (!string.IsNullOrEmpty(leaderName))
            {
                var charSprite = Character(leaderName);
                if (charSprite != null) return charSprite;
            }
            // Fallback : premier perso possédé
            var fallback = Catalog.Characters.Find(c => c.IsOwned);
            if (fallback != null)
            {
                var charSprite = Character(fallback.Name);
                if (charSprite != null) return charSprite;
            }
            return Get(attacking ? "jimmy_attack" : "jimmy_idle");
        }
        public static Texture2D? Enemy(bool isBoss)     => Get(isBoss ? "boss_demon" : "enemy_ninja");
        public static Texture2D? Effect(string key)     => Get("fx_orb_" + key);
        public static Texture2D? BgDungeon()            => Get("bg_combat") ?? Get("bg_dungeon");
        public static Texture2D? BgCity()               => Get("bg_menu") ?? Get("bg_city");
        public static Texture2D? Moto()                 => Get("moto_bike");

        // Images de cartes du menu principal
        static readonly Dictionary<string, string> _menuCardMap = new()
        {
            { "Wallet",     "menu_stats"      },
            { "Inventory",  "menu_inventory"  },
            { "Crosspark",  "menu_crosspark"  },
            { "Boutique",   "menu_boutique"   },
            { "Training",   "menu_training"   },
            { "Story",      "menu_story"      },
            { "Fruits",     "menu_fruits"     },
            { "CardGame",   "menu_cardgame"   },
            { "Background", "menu_background" },
        };
        public static Texture2D? MenuCard(string stateKey) =>
            _menuCardMap.TryGetValue(stateKey, out var k) ? Get(k) : null;

        // Véhicules
        static readonly Dictionary<string, string> _vehicleMap = new()
        {
            { "Tommy Mayo",          "vehicle_tommy"    },
            { "Dragster de l'Abîme", "vehicle_dragster" },
            { "Phoenix Rider",       "vehicle_phoenix"  },
            { "Vaisseau Céleste",    "vehicle_vaisseau" },
            { "Loup des Steppes",    "vehicle_loup"     },
        };
        public static Texture2D? Vehicle(string name) =>
            _vehicleMap.TryGetValue(name, out var k) ? Get(k) : null;

        // Capacités — préfixe "ability_" + clé simplifiée
        static readonly Dictionary<string, string> _abilityMap = new()
        {
            { "Domaine du Monarque",     "ability_monarque"      },
            { "Fruit du Golem",          "ability_golem"         },
            { "Rasengan Dimensionnel",   "ability_rasengan"      },
            { "Frappe Sérieuse",         "ability_frappe"        },
            { "Haki des Rois",           "ability_haki"          },
            { "Invocation des Ombres",   "ability_invoc_ombres"  },
            { "Armure des Ombres",       "ability_armure_ombres" },
            { "Extraction des Ombres",   "ability_extraction"    },
            { "Chaîne Électrique",       "ability_chaine_elec"   },
            { "Vitesse du Tonnerre",     "ability_vitesse_tonne" },
            { "Tempête de Foudre",       "ability_tempete_foudre"},
            { "Écailles du Dragon",      "ability_ecailles"      },
            { "Souffle de Feu Dragon",   "ability_souffle_feu"   },
            { "Forme Draconique",        "ability_forme_draco"   },
            { "Frappe Fatale",           "ability_frappe_fatale" },
            { "Voile de Brume",          "ability_voile_brume"   },
            { "Sentence de Mort",        "ability_sentence_mort" },
            { "Soin Divin",              "ability_soin_divin"    },
            { "Bouclier Sacré",          "ability_bouclier_sacre"},
            { "Jugement Céleste",        "ability_jugement"      },
        };
        public static Texture2D? Ability(string name) =>
            _abilityMap.TryGetValue(name, out var k) ? Get(k) : null;
    }
}
