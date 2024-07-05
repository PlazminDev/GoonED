using OpenTK.Graphics.OpenGL;
using StbImageSharp;

namespace GoonED.Rendering
{
    public class Texture
    {
        public int TextureID { get; private set; }

        public int Width { get; private set; }
        public int Height { get; private set; }

        public Texture(string name)
        {
            ImageResult image = FileTools.GetImageResult(name);

            TextureID = GL.GenTexture();

            Width = image.Width;
            Height = image.Height;

            Bind();

            GL.TexParameterI(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, new int[] { (int)TextureMagFilter.Nearest });
            GL.TexParameterI(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, new int[] { (int)TextureMinFilter.Nearest });
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, Width, Height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, image.Data);
            GL.GenerateMipmap(GenerateMipmapTarget.Texture2D); // for some reason this line is crucial to the texture loading

            Unbind();
        }

        public Texture(int width, int height, byte[] data)
        {
            TextureID = GL.GenTexture();

            Width = width;
            Height = height;

            Bind();

            GL.TexParameterI(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, new int[] { (int)TextureMagFilter.Nearest });
            GL.TexParameterI(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, new int[] { (int)TextureMinFilter.Nearest });
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, Width, Height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, data);
            GL.GenerateMipmap(GenerateMipmapTarget.Texture2D); // for some reason this line is crucial to the texture loading

            Unbind();

            //GL.TextureStorage2D(TextureID, 0, SizedInternalFormat.Rgba8, width, height);
            //GL.CompressedTexImage2D(TextureTarget.Texture2D, 0, InternalFormat.CompressedSrgbAlphaS3tcDxt5Ext, width, height, 0, data.Length, data);
        }

        public void Bind()
        {
            GL.BindTexture(TextureTarget.Texture2D, TextureID);
        }

        public void Unbind()
        {
            GL.BindTexture(TextureTarget.Texture2D, 0);
        }

        public void Cleanup()
        {
            GL.DeleteTexture(TextureID);
        }
    }
}
