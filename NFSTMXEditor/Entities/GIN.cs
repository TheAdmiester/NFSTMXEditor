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
    class GIN
    {
        public string header = "Gnsu20";
        public byte unk1, unk2, unk3, unk4, unk5, unk6;
        public byte[] audioData;
        public float minRpm, maxRpm;
        public int numGrains, numSamples, sampleRate;
        public List<int> grainTable1, grainTable2;

        public GIN()
        {
            grainTable1 = new List<int>();
            grainTable2 = new List<int>();
        }

        public static GIN ReadGIN(byte[] data)
        {
            GIN gin = new GIN();

            using (var stream = new BinaryStream(new MemoryStream(data)))
            {
                if (stream.ReadString(4) != "Gnsu")
                {
                    MessageBox.Show("Type detected as GIN but data is not valid GIN data. Please check the file and try again.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                else
                {
                    stream.Position = 0x8;
                    gin.minRpm = BitConverter.ToSingle(stream.ReadBytes(4), 0);
                    gin.maxRpm = BitConverter.ToSingle(stream.ReadBytes(4), 0);
                    gin.unk1 = stream.Read1Byte();
                    gin.unk2 = stream.Read1Byte();
                    gin.unk3 = stream.Read1Byte();
                    gin.unk4 = stream.Read1Byte();
                    gin.numGrains = stream.ReadInt32();
                    gin.numSamples = stream.ReadInt32();
                    gin.sampleRate = stream.ReadInt32();

                    for (int i = 0; i < 51; i++)
                    {
                        gin.grainTable1.Add(stream.ReadInt32());
                    }

                    stream.Position += 0x4;

                    for (int i = 0; i < gin.numGrains; i++)
                    {
                        gin.grainTable2.Add(stream.ReadInt32());
                    }

                    gin.audioData = stream.ReadBytes(data.Length - (int)stream.Position);
                }
            }

            return gin;
        }

        public static void WriteGIN(FileStream file, GIN gin)
        {
            file.WriteString("Gnsu20", StringCoding.Raw);
            file.WriteBytes(new byte[] { 0x0, 0x0 });
            file.WriteBytes(BitConverter.GetBytes(gin.minRpm));
            file.WriteBytes(BitConverter.GetBytes(gin.maxRpm));
            file.WriteByte(gin.unk1);
            file.WriteByte(gin.unk2);
            file.WriteByte(gin.unk3);
            file.WriteByte(gin.unk4);
            file.WriteInt32(gin.numGrains);
            file.WriteInt32(gin.numSamples);
            file.WriteInt32(gin.sampleRate);

            foreach (int grain in gin.grainTable1)
            {
                file.WriteInt32(grain);
            }

            file.WriteInt32(0);

            foreach (int grain in gin.grainTable2)
            {
                file.WriteInt32(grain);
            }

            file.WriteBytes(gin.audioData);
        }

        public static int GetGINLength(MemoryStream file, GIN gin)
        {
            file.WriteString("Gnsu20", StringCoding.Raw);
            file.WriteBytes(new byte[] { 0x0, 0x0 });
            file.WriteBytes(BitConverter.GetBytes(gin.minRpm));
            file.WriteBytes(BitConverter.GetBytes(gin.maxRpm));
            file.WriteByte(gin.unk1);
            file.WriteByte(gin.unk2);
            file.WriteByte(gin.unk3);
            file.WriteByte(gin.unk4);
            file.WriteInt32(gin.numGrains);
            file.WriteInt32(gin.numSamples);
            file.WriteInt32(gin.sampleRate);

            foreach (int grain in gin.grainTable1)
            {
                file.WriteInt32(grain);
            }

            file.WriteInt32(0);

            foreach (int grain in gin.grainTable2)
            {
                file.WriteInt32(grain);
            }

            file.WriteBytes(gin.audioData);

            return (int)file.Length;
        }
    }
}
