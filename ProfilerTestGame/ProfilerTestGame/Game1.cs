using System.Threading;
using Gearset;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using ProfilerTestGame.Gearset;

namespace ProfilerTestGame
{
    /// <summary>
    /// This is the main type for your game
    /// </summary>
    public class Game1 : Microsoft.Xna.Framework.Game
    {
        GraphicsDeviceManager _graphics;
        SpriteBatch _spriteBatch;

        //Gearset
        FpsCounter _fpsCounter;
        MemoryMonitor _memoryMonitor;

        public Game1()
        {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
        }

        protected override void Initialize()
        {
            GS.Initialize(this);
            _fpsCounter = new FpsCounter(this);
            _memoryMonitor = new MemoryMonitor(this);
            Components.Add(_fpsCounter);
            Components.Add(_memoryMonitor);

            base.Initialize();
        }

        /// <summary>
        /// LoadContent will be called once per game and is the place to load
        /// all of your content.
        /// </summary>
        protected override void LoadContent()
        {
            // Create a new SpriteBatch, which can be used to draw textures.
            _spriteBatch = new SpriteBatch(GraphicsDevice);

            // TODO: use this.Content to load your game content here
        }

        /// <summary>
        /// UnloadContent will be called once per game and is the place to unload
        /// all content.
        /// </summary>
        protected override void UnloadContent()
        {
            // TODO: Unload any non ContentManager content here
        }

        /// <summary>
        /// Allows the game to run logic such as updating the world,
        /// checking for collisions, gathering input, and playing audio.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Update(GameTime gameTime)
        {
            // Allows the game to exit
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed)
                this.Exit();

            //We must call StartFrame at the top of Update to indicate to the TimeRuler that a new frame has started.
            GS.StartFrame();
            GS.BeginMark("Update", FlatTheme.PeterRiver);

            GS.Plot("FPS", _fpsCounter.Fps);
            GS.Plot("Total Memory K", _memoryMonitor.TotalMemoryK, 240);
            GS.Plot("Tick Memory K", _memoryMonitor.TickMemoryK);


            #if USE_GEARSET
                //Test for CPU / GPU bound
                if (GS.GearsetComponent.Console.Profiler.DoUpdate() == false)
                {
                    GS.EndMark("Update");
                    return;
                }   
            #endif

            Thread.Sleep(1);
            GS.EndMark("Update");

            base.Update(gameTime);
        }

        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);

            GS.BeginMark("Draw", FlatTheme.Pomegrantate);

            GS.BeginMark(1, "Draw Background", FlatTheme.Pumpkin);
            Thread.Sleep(1);
            GS.EndMark(1, "Draw Background");

            GS.BeginMark(1, "Draw Sprites", FlatTheme.Sunflower);
            Thread.Sleep(3);
            GS.EndMark(1, "Draw Sprites");

            GS.BeginMark(1, "Draw Particles", FlatTheme.Sunflower);
            Thread.Sleep(4);
            GS.EndMark(1, "Draw Particles");

            GS.EndMark("Draw");

            base.Draw(gameTime);
        }
    }
}
