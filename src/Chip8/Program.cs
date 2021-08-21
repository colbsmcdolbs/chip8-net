using System;
using Chip8.Helpers;
using static SDL2.SDL;

namespace Chip8
{
    class Program
    {
        public static void AudioFeed(IntPtr userData, IntPtr stream, int len)
        {

        }

        static void Main(string[] args)
        {
            const int videoScale = 15;
		    const int pitch = 4 * 64;
            const int clockRateHz = 600;  // TODO - Make configurable
            const int refreshRateHz = 60;
            const int instructionsPerCycle = clockRateHz / refreshRateHz;
            const int sdlDelay = 1000 / refreshRateHz;

            IntPtr nullPointer = IntPtr.Zero;
            var keyboardState = new bool[16];
            var keypadOptions = Chip8Helpers.GetKeyCodes();


            // DOING AUDIO THINGS
            
            SDL_AudioCallback callback = new SDL_AudioCallback();

            SDL_AudioSpec audioSpec = new SDL_AudioSpec
            {
                callback = AudioFeed(),
                channels = 1,
                freq = 44100,
                format = AUDIO_U8,
                samples = 4096,
                userdata = 
            };




            var emulator = new Chip8();
            emulator.Initialize();
            emulator.LoadRom();

            if (SDL_Init(SDL_INIT_EVERYTHING) < 0)
			{
				Console.WriteLine("SDL failed to init.");
				return;
			}

			var window = SDL_CreateWindow("Chip-8 Interpreter", SDL_WINDOWPOS_CENTERED, SDL_WINDOWPOS_CENTERED, 64 * videoScale, 32 * videoScale, SDL_WindowFlags.SDL_WINDOW_SHOWN);
			if (window == nullPointer)
			{
				Console.WriteLine("SDL could not create a window.");
				return;
			}

			var renderer = SDL_CreateRenderer(window, 0, SDL_RendererFlags.SDL_RENDERER_ACCELERATED);
			if (renderer == nullPointer)
			{
				Console.WriteLine("SDL could not create a valid renderer.");
				return;
			}
			
            var texture = SDL_CreateTexture(renderer, SDL_PIXELFORMAT_ABGR8888, (int)SDL_TextureAccess.SDL_TEXTUREACCESS_STREAMING, 64, 32);

            bool running = true;
            while (running)
            {
                // Cleanup - Create a SDLPollEvent method or handlerclass
                SDL_Event _Event;
                while (SDL_PollEvent(out _Event) == 1)
                {
                    int index;
                    switch(_Event.type)
                    {
                        case SDL_EventType.SDL_QUIT:
                            running = false;
                            break;
                        case SDL_EventType.SDL_KEYDOWN:
                            if (_Event.key.keysym.sym == SDL_Keycode.SDLK_ESCAPE) // Stop running if escape key pressed
                                running = false;
                            index = keypadOptions.IndexOf(_Event.key.keysym.sym);
                            if (index != -1)
                                keyboardState[index] = true;
                            break;
                        case SDL_EventType.SDL_KEYUP:
                            index = keypadOptions.IndexOf(_Event.key.keysym.sym);
                            if (index != -1)
                                keyboardState[index] = false;
                            break;
                    }
                }
                emulator.UpdateKeyboardState(keyboardState);
                for (short i = 0; i <= instructionsPerCycle; i++)
                {
                    emulator.RunNextStep();
                }

                if (emulator.IsScreenUpdated())
                {
                    unsafe // ;_;
                    {
                        fixed(uint *framebuffer = emulator.GetFramebuffer()) 
                        {
                            var framebufferRef = new IntPtr(framebuffer);
                            SDL_UpdateTexture(texture, nullPointer, framebufferRef, pitch);
                            SDL_RenderCopy(renderer, texture, nullPointer, nullPointer);
                            SDL_RenderPresent(renderer);
                        }
                    }
                    emulator.ResetDrawingFlag();
                }
                emulator.DecrementTimers();
                if (emulator.IsSoundTimerActive())
                {
                    // Play or pause audio
                }
                SDL_Delay(sdlDelay);
            }

            SDL_DestroyRenderer(renderer);
            SDL_DestroyWindow(window);
            SDL_DestroyTexture(texture);
            SDL_Quit();
        }
    }
}
