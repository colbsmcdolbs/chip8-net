using System.Collections.Generic;
using System;
using System.IO;
using Chip8.Helpers;

namespace Chip8
{
    public class Emulator
    {
        private byte[] _mainMemory;
        private byte[] _variableRegisters;
        private bool[] _keyboard;
        private Stack<ushort> _callStack;
        private uint[] _framebuffer;
        private ushort _programCounter;
        private ushort _indexRegister;
        private byte _delayTimer;
        private byte _soundTimer;
        private bool _isScreenUpdated;
        private Random _randomNumberGenerator;
        private uint _mainColor;

        public Emulator()
        {
            _mainMemory = new byte[4096];
            _variableRegisters = new byte[16];
            _keyboard = new bool[16];
            _framebuffer = new uint[64 * 32];
            _callStack = new Stack<ushort>();
            _programCounter = 0x200;
            _indexRegister = 0;
            _delayTimer = 0;
            _soundTimer = 0;
            _isScreenUpdated = false;
            _randomNumberGenerator = new Random();
            _mainColor = 0xFFFFFFFF;

            ClearScreen00E0(); // Initialize screen
        }

        public void Initialize()
        {
            var fonts = Chip8Helpers.GetFonts();
            int index = 0x050;
            foreach (var fontByte in fonts)
            {
                _mainMemory[index++] = fontByte;
            }
        }

        public void LoadRom()
        {
            var fileBytes = File.ReadAllBytes($"{Environment.CurrentDirectory}\\Assets\\PONG2");
            var startIndex = 0x200;
            foreach (var data in fileBytes)
            {
                _mainMemory[startIndex++] = data;
            }
        }

        public void Tick(int instructionsPerCycle)
        {
            for (int i = 0; i < instructionsPerCycle; i++)
            {
                var instruction = Fetch();
                var decoded = new DecodedInstruction(instruction);
                Execute(decoded);
            }
        }

        public void UpdateKeyboardState(bool[] keyboard) => _keyboard = keyboard;

        public uint[] GetFramebuffer() => _framebuffer;

        public void ResetDrawingFlag() => _isScreenUpdated = false;

        public bool IsScreenUpdated() => _isScreenUpdated;

        public bool IsSoundTimerActive() => _soundTimer > 0;

        public void DecrementTimers()
        {
            if (_delayTimer > 0) _delayTimer--;
            if (_soundTimer > 0) _soundTimer--;
        }

        private ushort Fetch()
        {
            byte firstHalf = _mainMemory[_programCounter++];
            byte secondHalf = _mainMemory[_programCounter++];
            ushort instruction = (ushort)((firstHalf << 8) + secondHalf);
            return instruction;
        }

        private void Execute(DecodedInstruction decoded)
        {
            switch(decoded.GetInstructionTuple())
            {
                case (0x0, 0x0, 0xE, 0x0):
                    ClearScreen00E0();
                    break;
                case (0x0, 0x0, 0xE, 0xE):
                    FinishSubroutine00EE();
                    break;
                case (0x0, _, _, _):
                    // Skip
                    break;
                case (0x1, _, _, _):
                    Jump1NNN(decoded.NNN);
                    break;
                case (0x2, _, _, _):
                    FinishSubroutine2NNN(decoded.NNN);
                    break;
                case (0x3, _, _, _):
                    Skip3XNN(decoded.X, decoded.NN);
                    break;
                case (0x4, _, _, _):
                    Skip4XNN(decoded.X, decoded.NN);
                    break;
                case (0x5, _, _, _):
                    Skip5XY0(decoded.X, decoded.Y);
                    break;
                case (0x6, _, _, _):
                    Set6XNN(decoded.X, decoded.NN);
                    break;
                case (0x7, _, _, _):
                    Add7XNN(decoded.X, decoded.NN);
                    break;
                case (0x8, _, _, _):
                    Logical8XYN(decoded.X, decoded.Y, decoded.N);
                    break;
                case (0x9, _, _, _):
                    Skip9XY0(decoded.X, decoded.Y);
                    break;
                case (0xA, _, _, _):
                    SetIndexANNN(decoded.NNN);
                    break;
                case (0xB, _, _, _):
                    JumpWithOffsetBNNN(decoded.NNN);
                    break;
                case (0xC, _, _, _):
                    RandomCXNN(decoded.X, decoded.NN);
                    break;
                case (0xD, _, _, _):
                    DisplayDXYN(decoded.X, decoded.Y, decoded.N);
                    break;
                case (0xE, _, _, _):
                    SkipIfKeyEXYN(decoded.X, decoded.Y);
                    break;
                case (0xF, _, 0x0, 0x7):
                    LoadDelayIntoRegisterFX07(decoded.X);
                    break;
                case (0xF, _, 0x1, 0x5):
                    LoadRegisterIntoDelayFX15(decoded.X);
                    break;
                case (0xF, _, 0x1, 0x8):
                    LoadRegisterIntoSoundFX18(decoded.X);
                    break;
                case (0xF, _, 0x1, 0xE):
                    AddToIndexFX1E(decoded.X);
                    break;
                case (0xF, _, 0x0, 0xA):
                    GetKeyFX0A(decoded.X);
                    break;
                case (0xF, _, 0x2, 0x9):
                    FontCharacterFX29(decoded.X);
                    break;
                case (0xF, _, 0x3, 0x3):
                    DecimalConversionFX33(decoded.X);
                    break;
                case (0xF, _, 0x5, 0x5):
                    StoreMemoryFX55(decoded.X);
                    break;
                case (0xF, _, 0x6, 0x5):
                    LoadFromMemoryFX65(decoded.X);
                    break;
                default:
                    Console.WriteLine("Instruction not implemented yet!");
                    break;
            }
        }

        #region OpCode Functions

        private void ClearScreen00E0()
        {
            uint offValue = ~_mainColor;
            Array.Fill<uint>(_framebuffer, offValue);
            _isScreenUpdated = true;
        }

        private void FinishSubroutine00EE()
        {
            _programCounter = _callStack.Pop();
        }

        private void Jump1NNN(ushort updatedProgramCounter)
        {
            _programCounter = updatedProgramCounter;
        }

        private void FinishSubroutine2NNN(ushort subroutineLocation)
        {
            _callStack.Push(_programCounter);
            _programCounter = subroutineLocation;
        }

        private void Skip3XNN(byte registerIndex, byte value)
        {
            if (_variableRegisters[registerIndex] == value)
                _programCounter = (ushort)(_programCounter + 2);
        }

        private void Skip4XNN(byte registerIndex, byte value)
        {
            if (_variableRegisters[registerIndex] != value)
                _programCounter = (ushort)(_programCounter + 2);
        }

        private void Skip5XY0(byte registerIndex1, byte registerIndex2)
        {
            if (_variableRegisters[registerIndex1] == _variableRegisters[registerIndex2])
                _programCounter = (ushort)(_programCounter + 2);
        }

        private void Set6XNN(byte registerIndex, byte newValue)
        {
            _variableRegisters[registerIndex] = newValue;
        }

        private void Add7XNN(byte registerIndex, byte valueToAdd)
        {
            var initial = _variableRegisters[registerIndex];
            _variableRegisters[registerIndex] = (byte)(initial + valueToAdd);
        }

        private void Logical8XYN(byte index1, byte index2, byte instruction)
        {
            byte value;
            switch(instruction)
            {
                case 0x0: // SET
                    _variableRegisters[index1] = _variableRegisters[index2];
                    break;
                case 0x1:// OR
                    _variableRegisters[index1] = (byte)(_variableRegisters[index1] | _variableRegisters[index2]);
                    break;
                case 0x2: // AND
                    _variableRegisters[index1] = (byte)(_variableRegisters[index1] & _variableRegisters[index2]);
                    break;
                case 0x3: // XOR
                    _variableRegisters[index1] = (byte)(_variableRegisters[index1] ^ _variableRegisters[index2]);
                    break;
                case 0x4: // Add
                    var sum = _variableRegisters[index1] + _variableRegisters[index2];
                    if (sum > 255)
                        _variableRegisters[0xF] = 1;
                    else
                        _variableRegisters[0xF] = 0;
                    _variableRegisters[index1] = (byte) sum;
                    break;
                case 0x5: //Subtract X - Y
                    if (_variableRegisters[index1] > _variableRegisters[index2])
                        _variableRegisters[0xF] = 1;
                    else
                        _variableRegisters[0xF] = 0;
                    _variableRegisters[index1] = (byte)(_variableRegisters[index1] - _variableRegisters[index2]);
                    break;
                case 0x6: // Shift >>
                    // TODO - Make this configurable for Super Chip-8 support
                    value = _variableRegisters[index1];
                    _variableRegisters[0xF] = (byte)(value & 0x1);
                    _variableRegisters[index1] = (byte)(value >> 1);
                    break;
                case 0x7: //Subtract Y - X
                    if (_variableRegisters[index2] > _variableRegisters[index1])
                        _variableRegisters[0xF] = 1;
                    else
                        _variableRegisters[0xF] = 0;
                    _variableRegisters[index1] = (byte)(_variableRegisters[index2] - _variableRegisters[index1]);
                    break;
                case 0xE: // Shift <<
                    // TODO - Make this configurable for Super Chip-8 support
                    value = _variableRegisters[index1];
                    _variableRegisters[0xF] = (byte)((value & 0x80) >> 7);
                    _variableRegisters[index1] = (byte)(value << 1);
                    break;
            }
        }

        private void Skip9XY0(byte registerIndex1, byte registerIndex2)
        {
            if (_variableRegisters[registerIndex1] != _variableRegisters[registerIndex2])
                _programCounter = (ushort)(_programCounter + 2);
        }
        
        private void SetIndexANNN(ushort updatedIR)
        {
            _indexRegister = updatedIR;
        }

        private void JumpWithOffsetBNNN(ushort updatedAddress)
        {
            // TODO MAKE THIS CONFIGURABLE
            _programCounter = (ushort)(updatedAddress + _variableRegisters[0x0]);
        }

        private void RandomCXNN(byte registerIndex, byte andValue)
        {
            byte random = (byte)_randomNumberGenerator.Next(0, 255);
            _variableRegisters[registerIndex] = (byte)(andValue & random);
        }

        private void DisplayDXYN(byte x, byte y, byte n)
        {
            var xCoor = _variableRegisters[x] % 64;
            var yCoor = _variableRegisters[y] % 32;
            _variableRegisters[0xF] = 0;

            for (int yCol = 0; yCol < n; yCol++)
            {
                byte indexByte = _mainMemory[_indexRegister + yCol];
                for (int xCol = 0; xCol < 8; xCol++)
                {
                    if ((indexByte & (0x80 >> xCol)) != 0)
                    {
                        var index = xCoor + xCol + ((yCoor + yCol) * 64);
                        if (index < 2048)
                        {
                            if (_framebuffer[index] == _mainColor)
                            {
                                _variableRegisters[0xF] = 1;
                            }
                            _framebuffer[index] = ~_framebuffer[index];
                        }
                    }
                }
            }
            _isScreenUpdated = true;
        }

        private void SkipIfKeyEXYN(byte registerIndex, byte type)
        {
            bool isSuccess = false;
            byte key = _variableRegisters[registerIndex];
            switch(type)
            {
                case 0x9:
                    isSuccess = _keyboard[key];
                    break;
                case 0xA:
                    isSuccess = !_keyboard[key];
                    break;
            }

            if (isSuccess)
                _programCounter = (ushort)(_programCounter + 2);
        }

        private void LoadDelayIntoRegisterFX07(byte registerIndex)
        {
            _variableRegisters[registerIndex] = _delayTimer;
        }

        private void LoadRegisterIntoDelayFX15(byte registerIndex)
        {
            _delayTimer = _variableRegisters[registerIndex];
        }

        private void LoadRegisterIntoSoundFX18(byte registerIndex)
        {
            _soundTimer = _variableRegisters[registerIndex];
        }

        private void AddToIndexFX1E(byte registerIndex)
        {
            _indexRegister = (ushort)(_indexRegister + _variableRegisters[registerIndex]);
        }

        private void GetKeyFX0A(byte registerIndex)
        {
            bool isFound = false;
            for (byte i = 0; i < _keyboard.Length; i++)
            {
                if (_keyboard[i])
                {
                    _variableRegisters[registerIndex] = i;
                    isFound = true;
                }
            }
            if (!isFound)
                _programCounter = (ushort)(_programCounter - 2);
        }

        private void FontCharacterFX29(byte registerIndex)
        {
            byte offset = (byte)(_variableRegisters[registerIndex] & 0xF);
            _indexRegister = (ushort)(0x050 + (offset * 5));
        }

        private void DecimalConversionFX33(byte registerIndex)
        {
           byte initialValue = _variableRegisters[registerIndex];
           _mainMemory[_indexRegister] = (byte)(initialValue / 100);
           var tens = initialValue % 100;
           _mainMemory[_indexRegister + 1] = (byte)(tens / 10);
           var ones = tens % 10; 
           _mainMemory[_indexRegister + 2] = (byte)ones;
        }

        private void StoreMemoryFX55(byte registerIndex)
        {
           ushort memoryLocation = _indexRegister;
           for (byte i = 0; i <= registerIndex; i++)
           {
               _mainMemory[memoryLocation++] = _variableRegisters[i];
           }
        }

        private void LoadFromMemoryFX65(byte registerIndex)
        {
           ushort memoryLocation = _indexRegister;
           for (byte i = 0; i <= registerIndex; i++)
           {
               _variableRegisters[i] = _mainMemory[memoryLocation++];
           }
        }

        #endregion
    }
}
