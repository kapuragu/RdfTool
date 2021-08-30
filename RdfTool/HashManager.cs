using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace RdfTool
{
    public delegate void HashIdentifiedDelegate(uint hashValue, string stringLiteral);

    public class HashManager
    {
        public Dictionary<uint, string> StrCode32LookupTable = new Dictionary<uint, string>();
        public Dictionary<uint, string> Fnv1LookupTable = new Dictionary<uint, string>();
        public Dictionary<uint, string> UsedHashes = new Dictionary<uint, string>();

        public static uint StrCode32(string text)
        {
            if (text == null) throw new ArgumentNullException("text");
            const ulong seed0 = 0x9ae16a3b2f90404f;
            ulong seed1 = text.Length > 0 ? (uint)((text[0]) << 16) + (uint)text.Length : 0;
            return (uint)(CityHash.CityHash.CityHash64WithSeeds(text + "\0", seed0, seed1) & 0xFFFFFFFFFFFF);
        }
        /// <summary>
        /// Whenever a hash is identified, keep track of it so we can output a list of all matching hashes.
        /// </summary>
        /// <param name="hashValue">Hash value that was matched.</param>
        /// <param name="stringLiteral">String literal the hashValue matches.</param>
        public void OnHashIdentified(uint hashValue, string stringLiteral)
        {
            if (!UsedHashes.ContainsKey(hashValue))
            {
                UsedHashes.Add(hashValue, stringLiteral);
            }
        }
        public abstract class FNVHash : HashAlgorithm
        {
            protected const uint FNV32_PRIME = 16777619;
            protected const uint FNV32_OFFSETBASIS = 2166136261;

            public FNVHash(int hashSize)
            {
                this.HashSizeValue = hashSize;
                this.Initialize();
            }
        }

        public class FNV1Hash32 : FNVHash
        {
            private uint _hash;

            public FNV1Hash32()
                : base(32) { }

            public override void Initialize()
            {
                _hash = FNV32_OFFSETBASIS;
            }

            protected override void HashCore(byte[] array, int ibStart, int cbSize)
            {
                for (int i = 0; i < cbSize; i++)
                    _hash = (_hash * FNV32_PRIME) ^ array[ibStart + i];
            }

            protected override byte[] HashFinal()
            {
                return BitConverter.GetBytes(_hash);
            }
        }

        //tex fnvhash from https://gist.github.com/RobThree/25d764ea6d4849fdd0c79d15cda27d61 check.cs
        public static uint FNV1Hash32Str(string text)
        {
            var fnvHash = new FNV1Hash32();
            var value = fnvHash.ComputeHash(Encoding.UTF8.GetBytes(text));//DEBUGNOW encoding? -v-
            var hash = BitConverter.ToUInt32(value, 0);
            return hash;
        }//FNV1Hash32Str
    }
}
