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
            //byte[] buffer = FileTools.ReadAsBytes(name);
            ImageResult image = FileTools.GetImageResult(name);

            TextureID = GL.GenTexture();

            Width = image.Width;
            Height = image.Height;

            Bind();

            GL.TexParameterI(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, new int[] { (int)TextureMagFilter.Linear });
            GL.TexParameterI(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, new int[] { (int)TextureMinFilter.Linear });
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, Width, Height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, image.Data);
            GL.GenerateMipmap(GenerateMipmapTarget.Texture2D); // for some reason this line is crucial to the texture loading

            Unbind();
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
