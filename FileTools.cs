using StbImageSharp;
using StbiSharp;
using System.Reflection;

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
    }
}
