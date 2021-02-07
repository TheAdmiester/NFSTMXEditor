using Syroot.BinaryData;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace NFSTMXEditor
{
    class TMX
    {
        public int numSounds = 0, ginHeaderOffset = 0, audioDataOffset = 0;
        public List<AudioEntry> audioEntries;
        public string fileName;
        public byte[] tmxHeader1, tmxHeader2;
        
        public TMX ReadTMX(string path)
        {
            var bytes = File.ReadAllBytes(path);

            fileName = path;

            using (var stream = new BinaryStream(new MemoryStream(bytes)))
            {
                audioEntries = new List<AudioEntry>();

                if (stream.ReadUInt32() != 2292214557)
                {
                    MessageBox.Show("Not a valid TMX file. Please open a valid TMX file and try again.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);

                    return null;
                }
                else
                {
                    bool searching = true;
                    int offset = -1;
                    List<int> ginOffsets = new List<int>(), snrOffsets = new List<int>();
                    byte[] ginHeader = new byte[] { 0x20, 0x4E, 0x49, 0x47 };
                    byte[] snrHeader = new byte[] { 0x20, 0x52, 0x4E, 0x53 };

                    // Search for GIN headers
                    while (searching)
                    {
                        offset = FindSequenceMultiple(bytes, ginHeader, ginOffsets);
                        if (offset != -1)
                        {
                            ginOffsets.Add(offset);
                        }
                        else
                        {
                            searching = false;
                        }
                    }

                    searching = true;

                    // Search for SNR headers
                    while (searching)
                    {
                        offset = FindSequenceMultiple(bytes, snrHeader, snrOffsets);
                        if (offset != -1)
                        {
                            snrOffsets.Add(offset);
                        }
                        else
                        {
                            searching = false;
                        }
                    }

                    numSounds = ginOffsets.Count + snrOffsets.Count;

                    stream.Position = 0;

                    tmxHeader1 = stream.ReadBytes(88);
                    ginHeaderOffset = stream.ReadInt32();
                    audioDataOffset = stream.ReadInt32();
                    tmxHeader2 = stream.ReadBytes(ginHeaderOffset - 96);

                    for (int i = 0; i < numSounds; i++)
                    {
                        AudioEntry entry = new AudioEntry();

                        entry.entrySize = (byte)stream.ReadByte();

                        entry.type = new string(stream.ReadString(3).Reverse().ToArray());

                        entry.index = stream.ReadInt32();
                        entry.relativeOffset = stream.ReadInt32();
                        entry.absoluteOffset = entry.relativeOffset + audioDataOffset;
                        entry.unk1 = stream.ReadInt32();
                        entry.unk2 = stream.ReadInt32();
                        entry.version = stream.ReadInt32();
                        entry.unk3 = stream.ReadInt32();
                        entry.unk4 = BitConverter.ToSingle(stream.ReadBytes(4), 0);
                        entry.unk5 = stream.ReadInt32();

                        audioEntries.Add(entry);
                    }

                    for (int i = 0; i < audioEntries.Count; i++)
                    {
                        int readTo = 0;

                        stream.Position = audioEntries[i].absoluteOffset;

                        // If last sound file, read to end, otherwise to next header
                        if (i == audioEntries.Count - 1)
                        {
                            readTo = bytes.Length;
                        }
                        else
                        {
                            readTo = audioEntries[i + 1].absoluteOffset;
                        }

                        if (audioEntries[i].type == "GIN")
                        {
                            audioEntries[i].ginData = GIN.ReadGIN(stream.ReadBytes((int)(readTo - stream.Position)));
                        }
                        else
                        {
                            audioEntries[i].audioData = stream.ReadBytes((int)(readTo - stream.Position));
                        }
                    }
                }
            }

            return this;
        }

        public void WriteTMX(string path)
        {
            using (var file = new FileStream(path, FileMode.Create))
            //using (var stream = new BinaryStream(file, ByteConverter.Little))
            {
                int cumulativeOffset = 0;

                //stream.Position = 0;

                file.WriteBytes(tmxHeader1);
                file.WriteInt32(ginHeaderOffset);
                file.WriteInt32(audioDataOffset);
                file.WriteBytes(tmxHeader2);

                foreach (AudioEntry entry in audioEntries)
                {
                    if (audioEntries.IndexOf(entry) > 0)
                    {
                        if (audioEntries[audioEntries.IndexOf(entry) - 1].type == "GIN")
                        {
                            cumulativeOffset += GIN.WriteGINToMemory(new MemoryStream(), audioEntries[audioEntries.IndexOf(entry) - 1].ginData);
                        }
                        else
                        {
                            cumulativeOffset += audioEntries[audioEntries.IndexOf(entry) - 1].audioData.Length;
                        }
                    }

                    file.WriteByte(entry.entrySize);
                    file.WriteString(new string(entry.type.Reverse().ToArray()), StringCoding.Raw);
                    file.WriteInt32(entry.index);
                    file.WriteInt32(cumulativeOffset);
                    file.WriteInt32(entry.unk1);
                    file.WriteInt32(entry.unk2);
                    file.WriteInt32(entry.version);
                    file.WriteInt32(entry.unk3);
                    file.WriteBytes(BitConverter.GetBytes(entry.unk4));
                    file.WriteInt32(entry.unk5);
                }

                foreach (AudioEntry entry in audioEntries)
                {
                    if (entry.type == "GIN")
                    {
                        GIN.WriteGIN(file, entry.ginData);
                    }
                    else
                    {
                        file.WriteBytes(entry.audioData);
                    }
                }
            }
        }

        static int FindSequenceMultiple(byte[] source, byte[] seq, List<int> existingOffsets = null)
        {
            //bool alreadyInList = false;
            int start = -1;
            int searchStart = 0;

            if (existingOffsets != null && existingOffsets.Count > 0)
            {
                searchStart = existingOffsets.Last() + 4; // If already in sequence, jump to right after the last "GIN" or "SNR" we already know about
            }

            for (int i = searchStart; i < source.Length - seq.Length + 1 && start == -1; i++)
            {
                var j = 0;
                for (; j < seq.Length && source[i + j] == seq[j]; j++) { }
                if (j == seq.Length) start = i;
            }

            return start;
        }
    }
}
