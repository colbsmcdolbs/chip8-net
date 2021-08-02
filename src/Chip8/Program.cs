using System;
using static SDL2.SDL;

namespace Chip8
{
    class Program
    {
        static void Main(string[] args)
        {
            const int videoScale = 15;
			const int pitch = 4 * 64;
            IntPtr nullPointer = IntPtr.Zero;

            var emulator = new Emulator();
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
                SDL_Event _Event;
                SDL_PollEvent(out _Event);
                switch(_Event.type) {
                    case SDL_EventType.SDL_QUIT:
                        running = false;
                        break;
                }

                emulator.RunNextStep();

                if (emulator.IsScreenUpdated())
                {
                    unsafe // ;_;
                    {
                        fixed(uint *ptr = emulator.GetFramebuffer()) 
                        {
                            var pointer = new IntPtr(ptr);
                            SDL_UpdateTexture(texture, nullPointer, pointer, pitch);
                            SDL_RenderCopy(renderer, texture, nullPointer, nullPointer);
                            SDL_RenderPresent(renderer);
                        }
                    }
                    emulator.ResetDrawingFlag();
                }

                SDL_Delay(1); // on windows this isn't really needed, but on unix systems for some god forsaken reason, not doing this will crash your DE
            }

            SDL_DestroyRenderer(renderer);
            SDL_DestroyWindow(window);
            SDL_DestroyTexture(texture);
            SDL_Quit();
        }
    }
}
