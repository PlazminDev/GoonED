using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoonED.Rendering
{
    public class ATexture : Texture
    {
        public string package { get; private set; }

        public ATexture(int width, int height, byte[] data, string package) : base(width, height, data) 
        {
            this.package = package;
        }
    }
}
