namespace GoonED.Shaders
{
    public class ShaderProgramSrc
    {
        public string VertexShaderSrc;
        public string FragmentShaderSrc;

        public ShaderProgramSrc(string vertexShaderSrc, string fragmentShaderSrc)
        {
            VertexShaderSrc = vertexShaderSrc;
            FragmentShaderSrc = fragmentShaderSrc;
        }
    }
}
