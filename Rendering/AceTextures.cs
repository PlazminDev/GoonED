using StbImageSharp;
using StbiSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoonED.Rendering
{
    // thanks zombie :)
    public class AceTextures
    {
        private class Header
        {
            char[] identifier = new char[4]; // "FATP"
            int version;
            long directory;
        }

        public enum TextureFormat : int
        {
            Alpha8 = 1,
            ARGB4444,
            RGB24,
            RGBA32,
            ARGB32,
            RGB565 = 7,
            R16 = 9,
            DXT1,
            DXT5 = 12,
            RGBA4444,
            BGRA32,
            RHalf,
            RGHalf,
            RGBAHalf,
            RFloat,
            RGFloat,
            RGBAFloat,
            YUY2,
            RGB9e5Float,
            BC6H = 24,
            BC7,
            BC4,
            BC5,
            DXT1Crunched,
            DXT5Crunched,
            PVRTC_RGB2,
            PVRTC_RGBA2,
            PVRTC_RGB4,
            PVRTC_RGBA4,
            ETC_RGB4,
            EAC_R = 41,
            EAC_R_SIGNED,
            EAC_RG,
            EAC_RG_SIGNED,
            ETC2_RGB,
            ETC2_RGBA1,
            ETC2_RGBA8,
            ASTC_4x4,
            ASTC_5x5,
            ASTC_6x6,
            ASTC_8x8,
            ASTC_10x10,
            ASTC_12x12,
            RG16 = 62,
            R8,
            ETC_RGB4Crunched,
            ETC2_RGBA8Crunched,
            ASTC_HDR_4x4,
            ASTC_HDR_5x5,
            ASTC_HDR_6x6,
            ASTC_HDR_8x8,
            ASTC_HDR_10x10,
            ASTC_HDR_12x12,
            RG32,
            RGB48,
            RGBA64,
            R8_SIGNED,
            RG16_SIGNED,
            RGB24_SIGNED,
            RGBA32_SIGNED,
            R16_SIGNED,
            RG32_SIGNED,
            RGB48_SIGNED,
            RGBA64_SIGNED,
        }

        public enum CompressionFormat : int
        {
            None
        };

        public struct TextureEntry
        {
            public string name;
            public int width;
            public int height;
            public int mip_count;
            public TextureFormat format;
            public CompressionFormat compression;
            public long offset;
            public int length;

            public TextureEntry(string name,
                int width,
                int height,
                int mip_count,
                TextureFormat format,
                CompressionFormat compression,
                long offset,
                int length)
            {
                this.name = name;
                this.width = width;
                this.height = height;
                this.mip_count = mip_count;
                this.format = format;
                this.compression = compression;
                this.offset = offset;
                this.length = length;
            }
        }

        Texture[] textures;
        TextureEntry[] entries;

        bool init = false;

        public void Load(string path)
        {
            using (FileStream textureStream = File.Open(path + "/Texture Packages/textures1.bin", FileMode.Open))
            {
                using (BinaryReader reader = new BinaryReader(textureStream))
                {
                    Console.WriteLine(Encoding.Default.GetString(reader.ReadBytes(4)));
                    int version = reader.ReadInt32();
                    Console.WriteLine("Version: " + version);
                    long directory = reader.ReadInt64();
                    Console.WriteLine("Directory: " + directory);

                    reader.BaseStream.Seek(directory, SeekOrigin.Begin);
                    int count = reader.ReadInt32();
                    Console.WriteLine("Count: " + count);

                    textures = new Texture[count];
                    entries = new TextureEntry[count];

                    // Get headers
                    for (int i = 0; i < count; i++)
                    {
                        int stringLength = reader.ReadInt32();
                        string name = Encoding.Default.GetString(reader.ReadBytes(stringLength));

                        int width = reader.ReadInt32();
                        int height = reader.ReadInt32();
                        int mip_count = reader.ReadInt32();
                        TextureFormat textureFormat = (TextureFormat)reader.ReadInt32();
                        CompressionFormat compressionFormat = (CompressionFormat)reader.ReadInt32();
                        long offset = reader.ReadInt64();
                        int length = reader.ReadInt32();

                        entries[i] = new TextureEntry(name, width, height, mip_count, textureFormat, compressionFormat, offset, length);

                        //Console.WriteLine(name + ", " + width + ", " + height + ", " + mip_count + ", " +
                            //textureFormat + ", " + compressionFormat + ", " + offset + ", " + length);

                        //reader.BaseStream.Seek(offset, SeekOrigin.Begin);
                        //reader.ReadBytes(length);
                        //textures[i] = new Texture(width, height, reader.ReadBytes(length));
                    }

                    Console.WriteLine("Assembling textures");

                    // Assemble textures
                    for(int i = 0; i < textures.Length; i++)
                    {
                        long offset = entries[i].offset;
                        int length = entries[i].length;

                        int width = entries[i].width;
                        int height = entries[i].height;

                        int mip_count = entries[i].mip_count;
                        TextureFormat format = entries[i].format;

                        reader.BaseStream.Seek(offset, SeekOrigin.Begin);
                        byte[] data = reader.ReadBytes(length);

                        //Console.WriteLine(format);

                        //Console.WriteLine(data);

                        /*
                         * empty 8x8 red texture for testing
                         */

                        //byte[] tempData = new byte[64 * 4];

                        /*
                        for (int j = 0; j < data.Length; j+=4)
                        {
                            data[j + 0] = reader.ReadByte();
                            data[j + 1] = (byte)0;
                            data[j + 2] = (byte)0;
                            data[j + 3] = (byte)255;
                        }
                        */

                        if (format == TextureFormat.DXT1)
                        {
                            data = FileTools.RGBAFromDXT1(width, height, data);
                        }
                        else if (format == TextureFormat.DXT5)
                        {
                            data = FileTools.RGBAFromDXT5(width, height, data);

                        }
                        else if(format == TextureFormat.ARGB32)
                        {
                            data = FileTools.RGBAFromARGB32(width, height, data);
                        }
                        else if (format == TextureFormat.RGB24)
                        {
                            data = FileTools.RGBAFromRGB24(width, height, data);
                        }
                        else
                        {
                            return;
                        }

                        textures[i] = new Texture(width, height, data);
                    }

                    init = true;
                }
            }
        }

        public int Length => textures.Length;

        public void Cleanup()
        {
            if(init)

            for (int i = 0; i < textures.Length; i++) { textures[i].Cleanup(); }
        }

        public Texture GetTexture(int i)
        {
            return textures[i];
        }

        public TextureEntry GetEntry(int i)
        {
            return entries[i];
        }
    }
}
