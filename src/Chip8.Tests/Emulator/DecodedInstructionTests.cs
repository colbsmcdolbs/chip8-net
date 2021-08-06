using NUnit.Framework;
using Chip8.Helpers;
using System;

namespace Chip8.Tests.Emulator
{
    public class DecodedInstructionTests
    {
        private DecodedInstruction _decoded;
        private readonly ushort _instruction = 0xABCD;

        [SetUp]
        public void Setup()
        {
            _decoded = new DecodedInstruction(_instruction);
        }

        [Test]
        public void GetXTest()
        {
            var result = _decoded.X;
            Assert.AreEqual(result, 0xB);
        }

        [Test]
        public void GetYTest()
        {
            var result = _decoded.Y;
            Assert.AreEqual(result, 0xC);
        }

        [Test]
        public void GetNTest()
        {
            var result = _decoded.N;
            Assert.AreEqual(result, 0xD);
        }

        [Test]
        public void GetNNTest()
        {
            var result = _decoded.NN;
            Assert.AreEqual(result, 0xCD);
        }

        [Test]
        public void GetNNNTest()
        {
            var result = _decoded.NNN;
            Assert.AreEqual(result, 0xBCD);
        }
    }
}
