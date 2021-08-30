using System;
using Chip8;
using Chip8.SDL.Helpers;
using static SDL2.SDL;

namespace Chip.SDL
{
    class Program
    {
        static void Main(string[] args)
        {
            try 
            {
                const int clockRateHz = 600;  // TODO - Make configurable
                const int refreshRateHz = 60;
                int instructionsPerCycle = (int)Math.Ceiling((double)(clockRateHz / refreshRateHz));
                const int sdlDelay = 1000 / refreshRateHz;
                var keyboardState = new bool[16];

                SDLHelpers.SDLInit();

                var emulator = new Emulator();
                emulator.Initialize();
                emulator.LoadRom();

                bool running = true;
                while (running)
                {
                    var eventResult = SDLHelpers.PollEvents(keyboardState);
                    running = eventResult.IsRunning;
                    keyboardState = eventResult.KeyboardStatus;

                    emulator.UpdateKeyboardState(keyboardState);
                    emulator.Tick(instructionsPerCycle);

                    if (emulator.IsScreenUpdated())
                    {
                        SDLHelpers.UpdateScreen(emulator.GetFramebuffer());
                        emulator.ResetDrawingFlag();
                    }

                    emulator.DecrementTimers();
                    SDLHelpers.ToggleAudio(emulator.IsSoundTimerActive());

                    SDL_Delay(sdlDelay);
                }

                SDLHelpers.SDLTearDown();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }
    }
}
