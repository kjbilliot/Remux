using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Emux.GameBoy;
using System.Windows.Forms;
using System.IO;
using Emux.GameBoy.Cartridge;
using Keys = Microsoft.Xna.Framework.Input.Keys;
using Emux.GameBoy.Graphics;
using System.Collections.Generic;
using System.Diagnostics;
using System;

namespace Remux
{
    /// <summary>
    /// This is the main type for your game.
    /// </summary>
    public class MainWindow : Game, IVideoOutput
    {
        GraphicsDeviceManager graphics;
        SpriteBatch spriteBatch;
        GameBoy vm;
        byte[] lastFrameData;

        public MainWindow()
        {
            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
            IsFixedTimeStep = true;
        }

        /// <summary>
        /// Allows the game to perform any initialization it needs to before starting to run.
        /// This is where it can query for any required services and load any non-graphic
        /// related content.  Calling base.Initialize will enumerate through any components
        /// and initialize them as well.
        /// </summary>
        protected override void Initialize()
        {
            // TODO: Add your initialization logic here

            base.Initialize();
        }

        /// <summary>
        /// LoadContent will be called once per game and is the place to load
        /// all of your content.
        /// </summary>
        protected override void LoadContent()
        {
            spriteBatch = new SpriteBatch(GraphicsDevice);
        }

        /// <summary>
        /// UnloadContent will be called once per game and is the place to unload
        /// game-specific content.
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
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == Microsoft.Xna.Framework.Input.ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();

            //if (vm != null) Console.WriteLine($"${vm.Cpu.Registers.PC:X4} -> {vm.Cpu.LastInstruction}");


            if (IsPressed(Keys.LeftControl, Keys.O) || IsPressed(Keys.RightControl, Keys.O))
            {
                LoadNewRom();
            }

            if (vm != null)
            {
                if (IsPressed(Keys.Up))
                {
                    vm.KeyPad.PressedButtons |= Emux.GameBoy.Input.GameBoyPadButton.Up;
                }
                else
                {
                    vm.KeyPad.PressedButtons &= ~Emux.GameBoy.Input.GameBoyPadButton.Up;
                }

                if (IsPressed(Keys.Down))
                {
                    vm.KeyPad.PressedButtons |= Emux.GameBoy.Input.GameBoyPadButton.Down;
                }
                else
                {
                    vm.KeyPad.PressedButtons &= ~Emux.GameBoy.Input.GameBoyPadButton.Down;
                }

                if (IsPressed(Keys.Left))
                {
                    vm.KeyPad.PressedButtons |= Emux.GameBoy.Input.GameBoyPadButton.Left;
                }
                else
                {
                    vm.KeyPad.PressedButtons &= ~Emux.GameBoy.Input.GameBoyPadButton.Left;
                }

                if (IsPressed(Keys.Right))
                {
                    vm.KeyPad.PressedButtons |= Emux.GameBoy.Input.GameBoyPadButton.Right;
                }
                else
                {
                    vm.KeyPad.PressedButtons &= ~Emux.GameBoy.Input.GameBoyPadButton.Right;
                }

                if (IsPressed(Keys.Z))
                {
                    vm.KeyPad.PressedButtons |= Emux.GameBoy.Input.GameBoyPadButton.A;
                }
                else
                {
                    vm.KeyPad.PressedButtons &= ~Emux.GameBoy.Input.GameBoyPadButton.A;
                }

                if (IsPressed(Keys.X))
                {
                    vm.KeyPad.PressedButtons |= Emux.GameBoy.Input.GameBoyPadButton.B;
                }
                else
                {
                    vm.KeyPad.PressedButtons &= ~Emux.GameBoy.Input.GameBoyPadButton.B;
                }

                if (IsPressed(Keys.Enter))
                {
                    vm.KeyPad.PressedButtons |= Emux.GameBoy.Input.GameBoyPadButton.Start;
                }
                else
                {
                    vm.KeyPad.PressedButtons &= ~Emux.GameBoy.Input.GameBoyPadButton.Start;
                }

                if (IsPressed(Keys.RightShift))
                {
                    vm.KeyPad.PressedButtons |= Emux.GameBoy.Input.GameBoyPadButton.Select;
                }
                else
                {
                    vm.KeyPad.PressedButtons &= ~Emux.GameBoy.Input.GameBoyPadButton.Select;
                }
            }

            base.Update(gameTime);
        }

        private void LoadNewRom()
        {
            OpenFileDialog ofd = new OpenFileDialog();
            if (ofd.ShowDialog() == DialogResult.OK)
            {
                vm?.Terminate();
                byte[] rom = File.ReadAllBytes(ofd.FileName);
                vm = new GameBoy(new EmulatedCartridge(rom));
                vm.Gpu.VideoOutput = this;
                vm.Cpu.Run();
            }
        }
        
        private bool IsPressed(params Keys[] keys)
        {
            bool pressed = true;
            foreach (Keys k in keys)
            {
                if (!Keyboard.GetState().IsKeyDown(k))
                {
                    pressed = false;
                    break;
                }
            }
            return pressed;
        }

        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        Stopwatch sw = new Stopwatch();
        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Microsoft.Xna.Framework.Color.Black);
            sw.Start();
            if (lastFrameData != null)
            {
                spriteBatch.Begin();
                byte[] fixedVram = FixVram(lastFrameData);
                Texture2D displayImage = new Texture2D(graphics.GraphicsDevice, 160, 144);
                displayImage.SetData(fixedVram);
                spriteBatch.Draw(displayImage, new Rectangle(0, 0, (int)(160*2), (int)(144*2)), Microsoft.Xna.Framework.Color.White);
                spriteBatch.End();
            }
            sw.Stop();
            Window.Title = $"Remux (Frame Time: {sw.ElapsedMilliseconds}ms | Cpu Time: {vm?.Cpu.FramesPerSecond:0.00} fps)";
            sw.Reset();
            base.Draw(gameTime);
        }

        private byte[] FixVram(byte[] rawData)
        {
            List<byte> data = new List<byte>();
            for (int i = 0; i < rawData.Length - 2; i += 3)
            {
                data.Add(rawData[i]);
                data.Add(rawData[i+1]);
                data.Add(rawData[i+2]);
                data.Add(255);
            }
            return data.ToArray();
        }

        public void RenderFrame(byte[] pixelData)
        {
            lastFrameData = pixelData;
        }
    }
}
