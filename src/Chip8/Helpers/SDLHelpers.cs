using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using static SDL2.SDL;

namespace Chip8.Helpers
{
    public static class SDLHelpers
    {
        private static IntPtr window;
        private static IntPtr renderer;
        private static IntPtr texture;
        private static SDL_AudioSpec audioSpec;
        private static SDL_Event _Event;
        private static List<SDL_Keycode> keypadOptions;
        private static uint audioDevice;
        const int pitch = 4 * 64;
        const int videoScale = 15;

        public static void SDLInit()
        {
            SDL_Init(SDL_INIT_AUDIO | SDL_INIT_VIDEO);
            GraphicsInit();
            AudioInit();
            keypadOptions = Chip8Helpers.GetKeyCodes();
        }

        private static void GraphicsInit()
        {
            window = SDL_CreateWindow("Chip-8 Interpreter", SDL_WINDOWPOS_CENTERED, SDL_WINDOWPOS_CENTERED, 64 * videoScale, 32 * videoScale, SDL_WindowFlags.SDL_WINDOW_SHOWN);
            renderer = SDL_CreateRenderer(window, 0, SDL_RendererFlags.SDL_RENDERER_ACCELERATED);
            texture = SDL_CreateTexture(renderer, SDL_PIXELFORMAT_ABGR8888, (int)SDL_TextureAccess.SDL_TEXTUREACCESS_STREAMING, 64, 32);
        }

        private static void AudioInit()
        {
            audioSpec = new SDL_AudioSpec
            {
                channels = 1,
                freq = 44100,
                samples = 256,
                format = AUDIO_S8,
                callback = new SDL_AudioCallback((userdata, stream, length) =>
                {
                    int sample = 0;
                    int beepSamples = 0;
                    sbyte[] waveData = new sbyte[length];

                    for (int i = 0; i < waveData.Length; i++, beepSamples++)
                    {
                        waveData[i] = (sbyte)(127 * Math.Sin(sample * Math.PI * 2 * 604.1 / 44100));
                        sample++;
                    }

                    byte[] byteData = (byte[])(Array)waveData;
                    Marshal.Copy(byteData, 0, stream, byteData.Length);
                })
            };

            audioDevice = SDL_OpenAudioDevice(null, 0, ref audioSpec, out _, (int)SDL_AUDIO_ALLOW_FORMAT_CHANGE);
        }

        public static EventResult PollEvents()
        {
            var keyboardState = new bool[16];
            var running = true;
            while (SDL_PollEvent(out _Event) == 1)
            {
                switch(_Event.type)
                {
                    case SDL_EventType.SDL_QUIT:
                        running = false;
                        break;
                    case SDL_EventType.SDL_KEYDOWN:
                        if (_Event.key.keysym.sym == SDL_Keycode.SDLK_ESCAPE) // Stop running if escape key pressed
                            running = false;
                        int index = keypadOptions.IndexOf(_Event.key.keysym.sym);
                        if (index != -1)
                            keyboardState[index] = true;
                        break;
                }
            }
            return new EventResult { IsRunning = running, KeyboardStatus = keyboardState };
        }

        public unsafe static void UpdateScreen(uint[] framebufferArray)
        {
            fixed(uint *framebuffer = framebufferArray) 
            {
                var framebufferRef = new IntPtr(framebuffer);
                SDL_UpdateTexture(texture, IntPtr.Zero, framebufferRef, pitch);
                SDL_RenderCopy(renderer, texture, IntPtr.Zero, IntPtr.Zero);
                SDL_RenderPresent(renderer);
            }
        }

        public static void ToggleAudio(bool isActive)
        {
            if (isActive)
                SDL_PauseAudioDevice(audioDevice, 0);
            else
                SDL_PauseAudioDevice(audioDevice, 1);
        }

        public static void SDLTearDown()
        {
            SDL_CloseAudioDevice(audioDevice);
            SDL_DestroyTexture(texture);
            SDL_DestroyRenderer(renderer);
            SDL_DestroyWindow(window);
            SDL_Quit();
        }
    }

    public class EventResult
    {
        public bool IsRunning { get; set; } = false;
        public bool[] KeyboardStatus { get; set; }
    }
}
