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
            const int MAX_SAMPLES_PER_UPDATE = 4096;
            const int MAX_SAMPLES = 512;

            // Buffer for the single cycle waveform we are synthesizing
            short[] data = new short[MAX_SAMPLES];

            // Frame buffer, describing the waveform when repeated over the course of a frame
            short[] writeBuf = new short[MAX_SAMPLES_PER_UPDATE];

            var emulator = new Chip8();
            emulator.Initialize();
            emulator.LoadRom();

            InitWindow(64 * videoScale, 32 * videoScale, "Chip-8 Interpreter");
            InitAudioDevice();
            SetAudioStreamBufferSizeDefault(4096);
            AudioStream stream = InitAudioStream(44100, 16, 1);
            PlayAudioStream(stream);

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


                // Refill audio stream if required
                // if (IsAudioStreamProcessed(stream))
                // {
                //     // Synthesize a buffer that is exactly the requested size
                //     int writeCursor = 0;
                //     int readCursor = 0;

                //     while (writeCursor < MAX_SAMPLES_PER_UPDATE)
                //     {
                //         // Start by trying to write the whole chunk at once
                //         int writeLength = MAX_SAMPLES_PER_UPDATE-writeCursor;

                //         // Limit to the maximum readable size
                //         int readLength = 1-readCursor;

                //         if (writeLength > readLength) writeLength = readLength;

                //         // Write the slice
                //         memcpy(writeBuf + writeCursor, data + readCursor, writeLength*sizeof(short));

                //         // Update cursors and loop audio
                //         readCursor = (readCursor + writeLength) % 1;

                //         writeCursor += writeLength;
                //     }

                //     // Copy finished frame to audio stream
                //     UpdateAudioStream(stream, writeBuf, MAX_SAMPLES_PER_UPDATE);
                // }



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
                    PlayAudioStream(stream);
                else
                    PauseAudioStream(stream);
                //----------------------------------------------------------------------------------
            }

            CloseAudioStream(stream);
            CloseAudioDevice();
            CloseWindow();
        }
    }
}
