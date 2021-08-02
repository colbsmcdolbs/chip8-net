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
        private Stack<ushort> _callStack;
        private uint[] _framebuffer;
        private ushort _programCounter;
        private ushort _indexRegister;
        private byte _delayTimer;
        private byte _soundTimer;
        private bool _isScreenUpdated;

        public Emulator()
        {
            _mainMemory = new byte[4096];
            _variableRegisters = new byte[16];
            _framebuffer = new uint[64 * 32];
            _callStack = new Stack<ushort>();
            _programCounter = 0x200;
            _indexRegister = 0;
            _delayTimer = 0;
            _soundTimer = 0;
            _isScreenUpdated = false;
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
            var fileBytes = File.ReadAllBytes($"{Environment.CurrentDirectory}\\Assets\\test_opcode.ch8");
            var startIndex = 0x200;
            foreach (var data in fileBytes)
            {
                _mainMemory[startIndex++] = data;
            }
        }

        public uint[] GetFramebuffer()
        {
            return _framebuffer;
        }

        public void ResetDrawingFlag()
        {
            _isScreenUpdated = false;
        }

        public bool IsScreenUpdated()
        {
            return _isScreenUpdated;
        }

        public void RunNextStep()
        {
            var instruction = Fetch();
            var decoded = new DecodedInstruction(instruction);
            Execute(decoded);
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
                case (0xD, _, _, _):
                    DisplayDXYN(decoded.X, decoded.Y, decoded.N);
                    break;
                default:
                    Console.WriteLine("Instruction not implemented yet!");
                    break;
            }
        }

        #region OpCode Functions

        private void ClearScreen00E0()
        {
            _framebuffer.Initialize();
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
            var currentProgramCounter = _programCounter;
            _callStack.Push(currentProgramCounter);
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
            switch(instruction)
            {
                case 0x0:
                    _variableRegisters[index1] = _variableRegisters[index2];
                    break;
                case 0x1:
                    _variableRegisters[index1] = (byte)(_variableRegisters[index1] | _variableRegisters[index2]);
                    break;
                case 0x2:
                    _variableRegisters[index1] = (byte)(_variableRegisters[index1] & _variableRegisters[index2]);
                    break;
                case 0x3:
                    _variableRegisters[index1] = (byte)(_variableRegisters[index1] ^ _variableRegisters[index2]);
                    break;
                case 0x4:
                    // TODO
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
                        if (_framebuffer[index] == 0xffffffff)
                        {
                            _variableRegisters[0xF] = 1;
                        }
                        _framebuffer[index] ^= 0xffffffff;
                    }
                }
            }
            _isScreenUpdated = true;
        }

        #endregion
    }
}
