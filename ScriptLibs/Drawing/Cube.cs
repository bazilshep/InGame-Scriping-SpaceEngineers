using VRageMath;

namespace IngameScript.Drawing
{
    public class Cube
    {

        public static readonly int triangles = 12;
        public static readonly Vector3[] Verts = new Vector3[]{

            new Vector3(1,-1,1), new Vector3(1,-1,-1), new Vector3(1,1,-1),
            new Vector3(1,-1,1), new Vector3(1,1,-1), new Vector3(1,1,1), // +x
            new Vector3(-1,-1,-1), new Vector3(-1,-1,1), new Vector3(-1,1,1),
            new Vector3(-1,-1,-1), new Vector3(-1,1,1), new Vector3(-1,1,-1), //-x

            new Vector3(-1,1,1), new Vector3(1,1,1), new Vector3(1,1,-1),
            new Vector3(-1,1,1), new Vector3(1,1,-1), new Vector3(-1,1,-1), //+y
            new Vector3(1,-1,1), new Vector3(-1,-1,-1), new Vector3(1,-1,-1),
            new Vector3(1,-1,1), new Vector3(-1,-1, 1), new Vector3(-1,-1,-1), //-y

            new Vector3(-1,-1,1), new Vector3(1,-1,1), new Vector3(1,1,1),
            new Vector3(-1,-1,1), new Vector3(1,1,1), new Vector3(-1,1,1), //+z
            new Vector3(1,-1,-1), new Vector3(-1,-1,-1), new Vector3(-1,1,-1),
            new Vector3(1,-1,-1), new Vector3(-1,1,-1), new Vector3(1,1,-1), //-z
        };

        public static readonly Vector3[] Normals = new Vector3[]
        {
            new Vector3(1,0,0), new Vector3(-1,0,0),
            new Vector3(0,1,0), new Vector3(0,-1,0),
            new Vector3(0,0,1), new Vector3(0,0,-1),
        };

        public Matrix scale = Matrix.Identity;
        public Matrix trf = Matrix.Identity;
        public Color color;
        public Cube()
        {

        }

        public void AddTo(Frame3D s, ref Matrix model)
        {
            var trf_model_view = trf * model * s.view;
            Vector3 light = Vector3.TransformNormal(s.LightDirection, Matrix.Transpose(trf_model_view));
            light.Normalize();
            var combined_projection = scale * trf_model_view * s.projection;

            Triangle t;
            t.c = color;

            for (var i = 0; i < triangles; i++)
            {
                t.v1 = Verts[i * 3];
                t.v2 = Verts[i * 3 + 1];
                t.v3 = Verts[i * 3 + 2];
                t.n = Normals[i/2];
                s.AddTriangle(ref t, ref combined_projection, ref light);
            }
        }
        

    }
}