using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoonED
{
    public class IndexBufferUtil
    {
        public static int GetTriangleFanIndexCount(int vertexCount)
        {
            if (vertexCount < 0) throw new IndexOutOfRangeException();

            if (vertexCount < 3) return 0;

            return (vertexCount - 2) * 3;
        }

        public static uint BuildTriangleFan(uint vertexStart, uint vertexCount, ref uint[] indices)
        {
            uint count = vertexCount - 2;

            if(vertexCount < 0) throw new IndexOutOfRangeException();
            if (vertexCount < 3) { return 0; }
            if (indices.Length < count * 3) throw new IndexOutOfRangeException();

            for(uint i = 0; i < count; i++)
            {
                indices[3 * i + 0] = vertexStart;
                indices[3 * i + 2] = vertexStart + i + 1;
                indices[3 * i + 1] = vertexStart + i + 2;
            }

            return count * 3;
        }
    }
}
