using OpenTK.Mathematics;

namespace GoonED.Rendering
{
    public class Sprite
    {
        public Vector2 position { get; set; } = new();
        public float rotation = 0.0f;
        public Texture texture;

        public Sprite(Vector2 position, float rotation, Texture tex)
        {
            this.position = position;
            this.rotation = rotation;
            this.texture = tex;
        }

        public Matrix4 GetModelMatrix()
        {
            Matrix4 modelMatrix = Matrix4.Identity;
            modelMatrix *= Matrix4.CreateRotationZ(rotation * (MathF.PI / 180.0f));
            modelMatrix *= Matrix4.CreateTranslation(position.X, position.Y, -9.0f);

            return modelMatrix;
        }
    }
}
