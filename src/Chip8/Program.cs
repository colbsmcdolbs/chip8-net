using System;
using Chip8.Helpers;
using Raylib_cs;
using static Raylib_cs.Raylib;

namespace Chip8
{
    class Program
    {
        static void Main(string[] args)
        {
            const int videoScale = 15;
            const int clockRateHz = 600;  // TODO - Make configurable
            const int refreshRateHz = 60;
            const int instructionsPerCycle = clockRateHz / refreshRateHz;
            var keyboardState = new bool[16];

            var emulator = new Chip8();
            emulator.Initialize();
            emulator.LoadRom();

            InitWindow(64 * videoScale, 32 * videoScale, "Chip-8 Interpreter");
            SetTargetFPS(refreshRateHz);

            while (!WindowShouldClose())
            {
                keyboardState.Initialize();
                int index = 0;
                foreach (var key in Chip8Helpers.GetKeyCodes())
                {
                    if (IsKeyDown(key)) keyboardState[index] = true;
                    else if(IsKeyUp(key)) keyboardState[index] = false;
                    index++;
                }

                emulator.UpdateKeyboardState(keyboardState);
                emulator.Tick(instructionsPerCycle);

                // Draw
                //----------------------------------------------------------------------------------
                unsafe // ;_;
                {
                    fixed(uint *framebuffer = emulator.GetFramebuffer()) 
                    {
                        var framebufferRef = new IntPtr(framebuffer);
                        var image = new Image
                        {
                            data = framebufferRef,
                            format = PixelFormat.PIXELFORMAT_UNCOMPRESSED_R8G8B8A8,
                            width = 64,
                            height = 32,
                            mipmaps = 1
                        };
                        var texture = LoadTextureFromImage(image);

                        BeginDrawing();
                        ClearBackground(Color.BLACK);
                        DrawTextureEx(texture, new System.Numerics.Vector2(), 0, 15, Color.WHITE);
                        EndDrawing();

                        UnloadTexture(texture);
                        emulator.ResetDrawingFlag();
                    }
                }

                emulator.DecrementTimers();
                if (emulator.IsSoundTimerActive())
                {
                    // Play or pause audio
                }
                //----------------------------------------------------------------------------------
            }

            CloseWindow();
        }
    }
}
