using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NFSTMXEditor
{
    class AudioEntry
    {
        public byte entrySize;
        public int index, absoluteOffset, relativeOffset, unk1, unk2, version, unk3, unk5;
        public float unk4;
        public string type;
        public byte[] audioData;
        public GIN ginData;
    }
}
