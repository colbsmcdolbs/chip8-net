using System;
using Chip8.Helpers;
using static SDL2.SDL;

namespace Chip8
{
    class Program
    {
        static void Main(string[] args)
        {
            const int clockRateHz = 600;  // TODO - Make configurable
            const int refreshRateHz = 60;
            int instructionsPerCycle = (int)Math.Ceiling((double)(clockRateHz / refreshRateHz));
            const int sdlDelay = 1000 / refreshRateHz;

            SDLHelpers.SDLInit();

            var emulator = new Chip8();
            emulator.Initialize();
            emulator.LoadRom();

            bool running = true;
            while (running)
            {
                var events = SDLHelpers.PollEvents();
                running = events.IsRunning;

                emulator.UpdateKeyboardState(events.KeyboardStatus);
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
    }
}
