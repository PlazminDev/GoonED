using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;
using System;
using System.Reflection;
using GLShaderType = OpenTK.Graphics.OpenGL.ShaderType;

namespace GoonED.Shaders
{
    public class Shader
    {
        public int ProgramID { get; private set; }

        private ShaderProgramSrc _shaderProgramSrc { get; }

        public bool Compiled { get; private set; }

        private Dictionary<string, int> uniforms = new();

        public Shader(ShaderProgramSrc shaderProgramSource, bool compile = false)
        {
            _shaderProgramSrc = shaderProgramSource;
            if (compile)
            {
                CompileShader();
            }
        }

        public bool CompileShader()
        {
            if (_shaderProgramSrc == null)
            {
                Console.Error.WriteLine("Shader.cs: Shader Program Source is Null!");
                return false;
            }
            if (Compiled)
            {
                Console.Error.WriteLine("Shader.cs: Shader is already compiled!");
                return false;
            }

            int vertexShaderID = GL.CreateShader(GLShaderType.VertexShader);
            GL.ShaderSource(vertexShaderID, _shaderProgramSrc.VertexShaderSrc);
            GL.CompileShader(vertexShaderID);
            GL.GetShader(vertexShaderID, ShaderParameter.CompileStatus, out var vertexShaderCompilationCode);
            if (vertexShaderCompilationCode != (int)All.True)
            {
                Console.Error.WriteLine(GL.GetShaderInfoLog(vertexShaderID));
                return false;
            }

            int fragmentShaderID = GL.CreateShader(GLShaderType.FragmentShader);
            GL.ShaderSource(fragmentShaderID, _shaderProgramSrc.FragmentShaderSrc);
            GL.CompileShader(fragmentShaderID);
            GL.GetShader(fragmentShaderID, ShaderParameter.CompileStatus, out var fragmentShaderCompilationCode);
            if (fragmentShaderCompilationCode != (int)All.True)
            {
                Console.Error.WriteLine(GL.GetShaderInfoLog(fragmentShaderID));
                return false;
            }

            ProgramID = GL.CreateProgram();
            GL.AttachShader(ProgramID, vertexShaderID);
            GL.AttachShader(ProgramID, fragmentShaderID);
            GL.LinkProgram(ProgramID);

            GL.DetachShader(ProgramID, vertexShaderID);
            GL.DetachShader(ProgramID, fragmentShaderID);

            GL.DeleteShader(vertexShaderID);
            GL.DeleteShader(fragmentShaderID);

            // Load uniforms
            GL.GetProgram(ProgramID, GetProgramParameterName.ActiveUniforms, out int count);
            //Console.WriteLine("Uniforms Count: " + count);

            for (int i = 0; i < count; i++)
            {
                GL.GetActiveUniform(ProgramID, i, 32, out int length, out int size, out ActiveUniformType type, out string name);
            }

            Compiled = true;
            return true;
        }

        private void CreateUniform(string name)
        {
            int uniformLocation = GL.GetUniformLocation(ProgramID, name);
            if (uniformLocation < 0)
            {
                // this should never happen
                Console.Error.WriteLine("Shader.cs: Uniform of name " + name + " not found.");
                return;
            }
            uniforms.Add(name, uniformLocation);
        }

        public void SetInt(string uniformName, int value)
        {
            GL.Uniform1(GL.GetUniformLocation(ProgramID, uniformName), value);
        }

        public void SetFloat(string uniformName, float value)
        {
            GL.Uniform1(GL.GetUniformLocation(ProgramID, uniformName), value);
        }

        public void SetVector3(string uniformName, Vector3 value)
        {
            GL.Uniform3(GL.GetUniformLocation(ProgramID, uniformName), value);
        }

        public void SetVector4(string uniformName, Vector4 value)
        {
            GL.Uniform4(GL.GetUniformLocation(ProgramID, uniformName), value);
        }

        public void SetMatrix(string uniformName, Matrix4 value)
        {
            GL.UniformMatrix4(GL.GetUniformLocation(ProgramID, uniformName), false, ref value);
        }

        public void Bind()
        {
            if (Compiled)
            {
                GL.UseProgram(ProgramID);
            }
            else
            {
                //Console.WriteLine("Shader.cs: Shader has not been compiled!");
            }
        }
        public void Unbind()
        {
            if (Compiled)
            {
                GL.UseProgram(0);
            }
            else
            {
                //Console.WriteLine("Shader.cs: Shader has not been compiled!");
            }
        }

        public static ShaderProgramSrc ParseShader(string filePath)
        {

            string[] shaderSource = new string[2];
            ShaderType shaderType = ShaderType.NONE;
            var allLines = FileTools.ReadResource(filePath).Split("\n");
            for (int i = 0; i < allLines.Length; i++)
            {
                string current = allLines[i];
                if (current.ToLower().Contains("#shader"))
                {
                    if (current.ToLower().Contains("vertex"))
                    {
                        shaderType = ShaderType.VERTEX;
                    }
                    else if (current.ToLower().Contains("fragment"))
                    {
                        shaderType = ShaderType.FRAGMENT;
                    }
                    else
                    {
                        Console.Error.WriteLine("Shader.cs: No shader type identified in " + filePath + " at line " + i + "!");
                    }
                }
                else
                {
                    shaderSource[(int)shaderType] += current + Environment.NewLine;
                }
            }

            //Console.Write(shaderSource[0] + "\n");
            //Console.Write(shaderSource[1] + "\n");

            return new ShaderProgramSrc(shaderSource[(int)ShaderType.VERTEX], shaderSource[(int)ShaderType.FRAGMENT]);
        }

        public void Cleanup()
        {
            Unbind();
            GL.DeleteProgram(ProgramID);
        }
    }
}
