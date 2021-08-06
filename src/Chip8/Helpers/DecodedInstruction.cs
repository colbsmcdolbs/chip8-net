namespace Chip8.Helpers
{
    public class DecodedInstruction
    {
        private byte _firstInstruction { get; set; }
        private byte _secondInstruction { get; set; }
        private byte _thirdInstruction { get; set; }
        private byte _fourthInstruction { get; set; }


        public DecodedInstruction(ushort instruction)
        {
            _firstInstruction  = (byte)((instruction & 0xF000) >> 12);
            _secondInstruction = (byte)((instruction & 0x0F00) >> 8);
            _thirdInstruction  = (byte)((instruction & 0x00F0) >> 4);
            _fourthInstruction = (byte) (instruction & 0x000F);
        }

        public (byte, byte, byte, byte) GetInstructionTuple()
        {
            return (_firstInstruction, _secondInstruction, _thirdInstruction, _fourthInstruction);
        }

        public byte X => _secondInstruction;

        public byte Y => _thirdInstruction;

        public byte N => _fourthInstruction;

        public byte NN => (byte)((_thirdInstruction << 4) + _fourthInstruction);

        public ushort NNN => (ushort)((_secondInstruction << 8) + NN);
    }
}
