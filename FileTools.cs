using StbImageSharp;
using StbiSharp;
using System.Reflection;
using AssetRipper.TextureDecoder.Rgb;
using AssetRipper.TextureDecoder.Rgb.Formats;
using AssetRipper.TextureDecoder.Rgb.Channels;

namespace GoonED
{
    public class FileTools
    {
        private static string SignificantDrawerCompiler = Assembly.GetExecutingAssembly().GetName().Name.ToString();

        public static string ReadResource(string name)
        {
            var assembly = Assembly.GetExecutingAssembly();

            string resourcePath = name;
            // Format: "{Namespace}.{Folder}.{filename}.{Extension}"
            if (!name.StartsWith(nameof(SignificantDrawerCompiler)))
            {
                resourcePath = assembly.GetManifestResourceNames()
                    .Single(str => str.EndsWith(name));
            }

            if (resourcePath == null)
            {
                Console.WriteLine("Assembly Resource not found: " + name);
                return null;
            }

            using (Stream? stream = assembly.GetManifestResourceStream(resourcePath))
            {
                using (StreamReader reader = new StreamReader(stream))
                {
                    string data = reader.ReadToEnd();
                    stream.Dispose();
                    return data;
                }
            }
        }

        public static byte[] ReadAsBytes(string name)
        {
            var assembly = Assembly.GetExecutingAssembly();
            string resourcePath = name;
            // Format: "{Namespace}.{Folder}.{filename}.{Extension}"
            if (!name.StartsWith(nameof(SignificantDrawerCompiler)))
            {
                resourcePath = assembly.GetManifestResourceNames()
                    .Single(str => str.EndsWith(name));
            }

            if (resourcePath == null)
            {
                Console.WriteLine("Assembly Resource not found: " + name);
                return null;
            }

            using (Stream stream = assembly.GetManifestResourceStream(resourcePath))
            {
                using (MemoryStream memStream = new MemoryStream())
                {
                    stream.CopyTo(memStream);
                    byte[] data = memStream.ToArray();

                    stream.Dispose();
                    memStream.Dispose();

                    return data;
                }
            }
        }

        public static ImageResult GetImageResult(string name)
        {
            var assembly = Assembly.GetExecutingAssembly();
            string resourcePath = name;
            // Format: "{Namespace}.{Folder}.{filename}.{Extension}"
            if (!name.StartsWith(nameof(SignificantDrawerCompiler)))
            {
                resourcePath = assembly.GetManifestResourceNames()
                    .Single(str => str.EndsWith(name));
            }

            if (resourcePath == null)
            {
                Console.WriteLine("Assembly Resource not found: " + name);
                return null;
            }

            StbImage.stbi_set_flip_vertically_on_load(1);
            using (Stream stream = assembly.GetManifestResourceStream(resourcePath))
            {
                return ImageResult.FromStream(stream);
            }
        }

        ulong PackRGBA(byte r, byte g, byte b, byte a)
        {
            return (ulong)((r << 24) | (g << 16) | (b << 8) | a);
        }

        public static byte[] RGBAFromDXT1(int width, int height, byte[] data)
        {
            AssetRipper.TextureDecoder.Dxt.DxtDecoder.DecompressDXT1(data, width, height, out byte[] output);
            return output;
        }

        public static byte[] RGBAFromDXT5(int width, int height, byte[] data)
        {
            AssetRipper.TextureDecoder.Dxt.DxtDecoder.DecompressDXT5(data, width, height, out data);

            byte[] output = new byte[data.Length];
            for(int i = 0; i < data.Length; i+=4)
            {
                output[i + 0] = data[i + 2];
                output[i + 1] = data[i + 1];
                output[i + 2] = data[i + 0];
                output[i + 3] = data[i + 3];
            }

            return output;
        }
        
        public static byte[] RGBAFromARGB32(int width, int height, byte[] data)
        {
            RgbConverter.Convert<ColorARGB32, byte, ColorRGBA<byte>, byte>(data, width, height, out byte[] output);

            return output;
        }        
        
        public static byte[] RGBAFromRGB24(int width, int height, byte[] data)
        {
            RgbConverter.Convert<ColorRGB<byte>, byte, ColorRGBA<byte>, byte>(data, width, height, out byte[] output);

            return output;
        }
    }
}
