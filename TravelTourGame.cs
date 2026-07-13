using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using FontStashSharp;
using System.IO;
using TravelTour.Core;
using TravelTour.States;
using TravelTour.UI;

namespace TravelTour
{
    public class TravelTourGame : Game
    {
        readonly GraphicsDeviceManager _graphics;
        SpriteBatch   _sb     = null!;
        Texture2D     _pixel  = null!;
        FontSystem    _fontSystem    = null!;
        FontSystem    _fontBigSystem = null!;
        SpriteFontBase _font    = null!;
        SpriteFontBase _bigFont = null!;

        IGameState? _currentState;
        GameState   _nextState = GameState.MainMenu;
        bool        _changeRequested;

        Color[] _bgGradient = { new Color(2,6,24), new Color(10,21,80), new Color(32,8,64) };
        DungeonData? _pendingDungeon;
        IslandData?  _pendingIsland;

        bool _isPaused = false;
        bool _prevF11  = false;
        bool _prevF12  = false;
        bool _screenshotPending = false;

        public void ToggleFullscreen()
        {
            _graphics.IsFullScreen = !_graphics.IsFullScreen;
            if (_graphics.IsFullScreen)
            {
                _graphics.PreferredBackBufferWidth  = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Width;
                _graphics.PreferredBackBufferHeight = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Height;
            }
            else
            {
                _graphics.PreferredBackBufferWidth  = 1280;
                _graphics.PreferredBackBufferHeight = 720;
            }
            _graphics.ApplyChanges();
        }

        public TravelTourGame()
        {
            _graphics = new GraphicsDeviceManager(this)
            {
                PreferredBackBufferWidth  = 1280,
                PreferredBackBufferHeight = 720,
                IsFullScreen              = false,
            };
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
            Window.Title   = "Travel Tour - Action Adventure 2D";
            Window.AllowUserResizing = false;
        }

        protected override void OnDeactivated(object sender, System.EventArgs args)
        {
            base.OnDeactivated(sender, args);
            _isPaused = true;
        }

        protected override void OnActivated(object sender, System.EventArgs args)
        {
            base.OnActivated(sender, args);
            _isPaused = false;
        }

        protected override void Initialize()
        {
            base.Initialize();
            // Icône de la fenêtre — MonoGame DesktopGL charge Icon.bmp automatiquement
            // si présent dans le répertoire d'exécution (géré par SDL)
            string iconPath = Path.Combine(Directory.GetCurrentDirectory(), "Icon.bmp");
            if (File.Exists(iconPath))
            {
                try
                {
                    using var iconTex = Texture2D.FromFile(GraphicsDevice, iconPath);
                    // MonoGame ne permet pas de setter l'icône directement via l'API publique,
                    // mais SDL la charge automatiquement depuis Icon.bmp au démarrage.
                }
                catch { /* Silently ignore if icon can't load */ }
            }
        }

        protected override void LoadContent()
        {
            _sb = new SpriteBatch(GraphicsDevice);

            _pixel = new Texture2D(GraphicsDevice, 1, 1);
            _pixel.SetData(new[] { Color.White });

            // Load fonts directly from TTF (no Content Pipeline needed)
            string fontDir = Path.Combine(Directory.GetCurrentDirectory(), "Content", "Fonts");

            _fontSystem = new FontSystem();
            _fontSystem.AddFont(File.ReadAllBytes(Path.Combine(fontDir, "Regular.ttf")));
            _font = _fontSystem.GetFont(16);

            _fontBigSystem = new FontSystem();
            _fontBigSystem.AddFont(File.ReadAllBytes(Path.Combine(fontDir, "Bold.ttf")));
            _bigFont = _fontBigSystem.GetFont(32);

            // Init sprite loader
            TravelTour.Core.SpriteLoader.Init(GraphicsDevice);

            // Load persistent save
            SaveSystem.Load();

            bool firstLaunch = PlayerPrefs_GetBool("TutorialSeen") == false;
            ChangeState(firstLaunch ? GameState.Tutorial : GameState.MainMenu);
            PlayerPrefs_SetBool("TutorialSeen", true);
        }

        public void ChangeState(GameState state)
        {
            _nextState = state;
            _changeRequested = true;
        }

        public void StartDungeon(DungeonData d)
        {
            _pendingDungeon = d;
            ChangeState(GameState.Combat);
        }

        IslandData? _pendingReturnIsland;
        public void StartIslandDungeon(DungeonData d, IslandData island)
        {
            _pendingDungeon = d;
            _pendingReturnIsland = island;
            ChangeState(GameState.Combat);
        }

        int _storyChapterIndex = -1;
        public void StartStoryDungeon(DungeonData d, int chapterIdx)
        {
            _pendingDungeon    = d;
            _storyChapterIndex = chapterIdx;
            ChangeState(GameState.Combat);
        }

        public void NotifyStoryVictory()
        {
            if (_storyChapterIndex >= 0)
            {
                States.StoryState.MarkCompleted(_storyChapterIndex);
                States.StoryState.RequestedChapterIdx = _storyChapterIndex;
                _storyChapterIndex = -1;
            }
            ChangeState(GameState.Story);
        }

        public void SetBackground(Color[] gradient) => _bgGradient = gradient;

        public void EnterIsland(IslandData island)
        {
            _pendingIsland = island;
            ChangeState(GameState.WorldIsland);
        }

        IslandData? _pendingFishingIsland;
        public void EnterFishing(IslandData island)
        {
            _pendingFishingIsland = island;
            ChangeState(GameState.Fishing);
        }

        // Permet d'ouvrir l'écran des quêtes depuis n'importe où (menu, mer, île)
        // et d'y revenir correctement en sortant.
        public GameState  QuestReturnState  { get; private set; } = GameState.MainMenu;
        public IslandData? QuestReturnIsland { get; private set; }
        public void OpenQuest(GameState returnState, IslandData? returnIsland = null)
        {
            QuestReturnState  = returnState;
            QuestReturnIsland = returnIsland;
            ChangeState(GameState.Quest);
        }
        public void ExitQuest()
        {
            if (QuestReturnIsland != null) EnterIsland(QuestReturnIsland);
            else ChangeState(QuestReturnState);
        }

        static bool PlayerPrefs_GetBool(string key) =>
            System.IO.File.Exists(System.IO.Path.Combine(
                System.Environment.GetFolderPath(System.Environment.SpecialFolder.ApplicationData),
                "TravelTour", key));
        static void PlayerPrefs_SetBool(string key, bool val)
        {
            string dir = System.IO.Path.Combine(
                System.Environment.GetFolderPath(System.Environment.SpecialFolder.ApplicationData),
                "TravelTour");
            System.IO.Directory.CreateDirectory(dir);
            if (val) System.IO.File.WriteAllText(System.IO.Path.Combine(dir, key), "1");
        }

        void DoChangeState()
        {
            _currentState?.Dispose();
            IGameState next = _nextState switch
            {
                GameState.MainMenu   => new MainMenuState(this),
                GameState.Crosspark  => new CrossparkState(this),
                GameState.Team       => new TeamState(this),
                GameState.Boutique   => new BoutiqueState(this),
                GameState.Training   => new TrainingState(this),
                GameState.Story      => new StoryState(this),
                GameState.Background => new BackgroundState(this),
                GameState.Combat     => new CombatState(this),
                GameState.Tutorial   => new TutorialState(this),
                GameState.Fruits     => new FruitState(this),
                GameState.Wallet     => new WalletState(this),
                GameState.Inventory  => new InventoryState(this),
                GameState.Quest      => new QuestState(this),
                GameState.Artifact   => new ArtifactState(this),
                GameState.WorldSea   => new WorldSeaState(this),
                GameState.WorldIsland=> new WorldIslandState(this),
                GameState.Fishing    => new FishingState(this),
                _                    => new MainMenuState(this)
            };

            // Set dungeon BEFORE Load so SpawnWave() has data
            if (next is CombatState cs && _pendingDungeon != null)
            {
                cs.SetDungeon(_pendingDungeon);
                cs.IsStoryDungeon = (_storyChapterIndex >= 0);
                _pendingDungeon = null;
                if (_pendingReturnIsland != null)
                {
                    cs.ReturnToIsland = _pendingReturnIsland;
                    _pendingReturnIsland = null;
                }
            }

            if (next is WorldIslandState wis && _pendingIsland != null)
            {
                wis.SetIsland(_pendingIsland);
                _pendingIsland = null;
            }

            if (next is FishingState fs && _pendingFishingIsland != null)
            {
                fs.SetIsland(_pendingFishingIsland);
                _pendingFishingIsland = null;
            }

            switch (next)
            {
                case MainMenuState   m:  m.Load(_pixel, _font, _bigFont);  break;
                case CrossparkState  c:  c.Load(_pixel, _font, _bigFont);  break;
                case TeamState       t:  t.Load(_pixel, _font, _bigFont);  break;
                case BoutiqueState   b:  b.Load(_pixel, _font, _bigFont);  break;
                case TrainingState   tr: tr.Load(_pixel, _font, _bigFont); break;
                case StoryState      s:  s.Load(_pixel, _font, _bigFont);  break;
                case BackgroundState bg: bg.Load(_pixel, _font, _bigFont); break;
                case CombatState     cb: cb.Load(_pixel, _font, _bigFont); break;
                case TutorialState   tu: tu.Load(_pixel, _font, _bigFont); break;
                case FruitState      fr: fr.Load(_pixel, _font, _bigFont); break;
                case WalletState     wa: wa.Load(_pixel, _font, _bigFont); break;
                case InventoryState  iv: iv.Load(_pixel, _font, _bigFont); break;
                case QuestState      qs: qs.Load(_pixel, _font, _bigFont); break;
                case ArtifactState   ar: ar.Load(_pixel, _font, _bigFont); break;
                case WorldSeaState    ws: ws.Load(_pixel, _font, _bigFont); break;
                case WorldIslandState wi: wi.Load(_pixel, _font, _bigFont); break;
                case FishingState     fi: fi.Load(_pixel, _font, _bigFont); break;
            }
            _currentState = next;
        }

        // Global HUD state
        float _popupTimer;
        string _popupText = "";
        Color  _popupColor = Color.White;
        float  _hudTime;
        UIButton? _walletBtn;

        protected override void Update(GameTime gt)
        {
            // Pause quand la fenêtre perd le focus
            if (_isPaused) { base.Update(gt); return; }

            // F11 → toggle plein écran
            bool f11 = Keyboard.GetState().IsKeyDown(Keys.F11);
            if (f11 && !_prevF11) ToggleFullscreen();
            _prevF11 = f11;

            // F12 → screenshot
            bool f12 = Keyboard.GetState().IsKeyDown(Keys.F12);
            if (f12 && !_prevF12) _screenshotPending = true;
            _prevF12 = f12;

            if (_changeRequested) { DoChangeState(); _changeRequested = false; }
            if (_nextState == GameState.MainMenu && Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();

            float dt = (float)gt.ElapsedGameTime.TotalSeconds;
            _hudTime    += dt;
            _popupTimer -= dt;

            // Drain popup queue
            if (_popupTimer <= 0 && PlayerSave.Popups.Count > 0)
            {
                _popupText  = PlayerSave.Popups.Dequeue();
                _popupColor = _popupText.Contains("💰") ? new Color(240,192,64) : new Color(0,200,255);
                _popupTimer = 1.8f;
            }

            // Wallet button (top-right, always visible except in wallet itself)
            if (_nextState != GameState.Wallet && _nextState != GameState.Tutorial)
            {
                int W = GraphicsDevice.Viewport.Width;
                _walletBtn ??= new UI.UIButton(
                    new Rectangle(W - 180, 8, 170, 34),
                    $"",
                    () => ChangeState(GameState.Wallet));
                _walletBtn.Bounds = new Rectangle(W - 180, 8, 170, 34);
                _walletBtn.Update(Mouse.GetState());
            }
            else _walletBtn = null;

            _currentState?.Update(gt);
            base.Update(gt);
        }

        void SaveScreenshot()
        {
            int W = GraphicsDevice.Viewport.Width;
            int H = GraphicsDevice.Viewport.Height;
            var colors = new Color[W * H];
            GraphicsDevice.GetBackBufferData(colors);
            using var tex = new Texture2D(GraphicsDevice, W, H);
            tex.SetData(colors);
            string dir = System.IO.Path.Combine(
                System.Environment.GetFolderPath(System.Environment.SpecialFolder.UserProfile),
                "TravelTour_Screenshots");
            System.IO.Directory.CreateDirectory(dir);
            string path = System.IO.Path.Combine(dir, $"screenshot_{System.DateTime.Now:yyyyMMdd_HHmmss}.png");
            using var stream = System.IO.File.OpenWrite(path);
            tex.SaveAsPng(stream, W, H);
            PlayerSave.Popups.Enqueue($"📸 Screenshot : {System.IO.Path.GetFileName(path)}");
        }

        protected override void Draw(GameTime gt)
        {
            DrawGradientBg();
            _sb.Begin(samplerState: SamplerState.PointClamp);
            _currentState?.Draw(_sb);
            DrawGlobalHUD(_sb);
            // Overlay PAUSE
            if (_isPaused)
            {
                int W = GraphicsDevice.Viewport.Width;
                int H = GraphicsDevice.Viewport.Height;
                _sb.Draw(_pixel, new Rectangle(0, 0, W, H), Color.Black * 0.6f);
                UIHelper.DrawCenteredText(_sb, _bigFont, "⏸  PAUSE",
                    new Rectangle(0, H/2 - 40, W, 80), new Color(255, 200, 0), 0.9f);
                UIHelper.DrawCenteredText(_sb, _font, "Cliquez sur la fenêtre pour reprendre",
                    new Rectangle(0, H/2 + 30, W, 30), new Color(150, 150, 200), 0.8f);
            }
            _sb.End();

            if (_screenshotPending) { SaveScreenshot(); _screenshotPending = false; }

            base.Draw(gt);
        }

        void DrawGlobalHUD(SpriteBatch sb)
        {
            int W = GraphicsDevice.Viewport.Width;
            int H = GraphicsDevice.Viewport.Height;

            // Wallet button (top-right)
            if (_walletBtn != null)
            {
                // Background pill
                var r = _walletBtn.Bounds;
                var pixel2 = _pixel;
                sb.Draw(pixel2, r, new Color(12,14,28));
                sb.Draw(pixel2, new Rectangle(r.X,r.Y,r.Width,1), new Color(240,192,64)*0.6f);
                sb.Draw(pixel2, new Rectangle(r.X,r.Bottom-1,r.Width,1), new Color(240,192,64)*0.6f);
                sb.Draw(pixel2, new Rectangle(r.X,r.Y,1,r.Height), new Color(240,192,64)*0.6f);
                sb.Draw(pixel2, new Rectangle(r.Right-1,r.Y,1,r.Height), new Color(240,192,64)*0.6f);

                string goldStr = $"💰 {PlayerSave.Gold:N0}";
                UI.UIHelper.DrawCenteredText(sb, _font, goldStr, r, new Color(240,192,64), 0.88f);
            }

            // Floating popup (bottom-center)
            if (_popupTimer > 0 && !string.IsNullOrEmpty(_popupText))
            {
                float alpha = _popupTimer > 0.4f ? 1f : _popupTimer / 0.4f;
                float rise  = (1.8f - _popupTimer) * 28f;
                var tsz = _font.MeasureString(_popupText);
                int px = (int)(W/2f - tsz.X/2f - 14);
                int py = (int)(H - 120 - rise);
                sb.Draw(_pixel, new Rectangle(px, py, (int)tsz.X+28, 34),
                    new Color(8,10,20) * (alpha * 0.9f));
                sb.DrawString(_font, _popupText,
                    new Vector2(px+14, py+8), _popupColor * alpha);
            }
        }

        void DrawGradientBg()
        {
            GraphicsDevice.Clear(_bgGradient[0]);
            _sb.Begin();
            int W = GraphicsDevice.Viewport.Width;
            int H = GraphicsDevice.Viewport.Height;
            for (int y = 0; y < H; y++)
            {
                float t = (float)y / H;
                Color c = t < 0.5f
                    ? Color.Lerp(_bgGradient[0], _bgGradient[1], t * 2f)
                    : Color.Lerp(_bgGradient[1], _bgGradient[2], (t - 0.5f) * 2f);
                _sb.Draw(_pixel, new Rectangle(0, y, W, 1), c);
            }
            _sb.End();
        }

        protected override void UnloadContent()
        {
            _currentState?.Dispose();
            _pixel.Dispose();
            _fontSystem.Dispose();
            _fontBigSystem.Dispose();
        }
    }
}
