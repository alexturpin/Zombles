﻿using Newtonsoft.Json.Linq;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using OpenTKTK.Utils;
using ResourceLibrary;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Zombles.Graphics
{
    public class EntityModel
    {
        private class Vertex
        {
            public Vector3 Position;
            public Vector2 TexCoord;

            public Vertex(JObject obj)
            {
                Position = new Vector3 {
                    X = (float) obj["x"],
                    Y = (float) obj["y"],
                    Z = (float) obj["z"]
                };

                TexCoord = new Vector2 {
                    X = (int) obj["u"],
                    Y = (int) obj["v"]
                };
            }
            
            public void GetVertices(float[] verts, ref int i)
            {
                verts[i++] = Position.X;
                verts[i++] = Position.Y;
                verts[i++] = Position.Z;
                verts[i++] = TexCoord.X;
                verts[i++] = TexCoord.Y;
            }
        }

        private class Face
        {
            public ResourceLocator Texture;
            public Vertex[] Vertices;

            public Face(JObject obj)
            {
                Texture = (String) obj["texture"];
                Vertices = ((JArray) obj["verts"])
                    .Select(x => new Vertex((JObject) x))
                    .ToArray();
            }

            public void GetVertices(float[] verts, ref int i)
            {
                foreach (var vert in Vertices) {
                    vert.GetVertices(verts, ref i);
                    verts[i++] = TextureManager.Ents.GetIndex(Texture);
                }
            }
        }

        private static VertexBuffer _sVB;
        private static bool _sVBInvalid;
        
        private static Dictionary<String, EntityModel> _sFound =
            new Dictionary<string, EntityModel>();

        public static VertexBuffer VertexBuffer
        {
            get
            {
                if (_sVB == null) {
                    _sVB = new VertexBuffer(6, BufferUsageHint.StaticDraw);
                    _sVBInvalid = true;
                }
                
                if (_sVBInvalid) {
                    float[] data = new float[_sFound.Values.Sum(x => x.Size * 6)];
                    int i = 0;
                    foreach (var mdl in _sFound.Values) {
                        mdl.GetVertices(data, ref i);
                    }
                    _sVB.SetData(data);
                    _sVBInvalid = false;
                }

                return _sVB;
            }
        }

        public static EntityModel GetModel(params String[] nameLocator)
        {
            var name = String.Join("/", nameLocator);

            if (!_sFound.ContainsKey(name)) {
                _sFound.Add(name, new EntityModel(Archive.Get<JObject>(nameLocator)));
                _sVBInvalid = true;
            }

            return _sFound[name];
        }

        private int _vertOffset;
        private Face[] _faces;

        public int Size { get; private set; }

        private EntityModel(JObject obj)
        {
            _faces = ((JArray) obj["faces"])
                .Select(x => new Face((JObject) x))
                .ToArray();

            Size = _faces.Length * 4;
        }

        private void GetVertices(float[] verts, ref int i)
        {
            _vertOffset = i / 6;

            foreach (var face in _faces) {
                face.GetVertices(verts, ref i);
            }
        }

        public void Render()
        {
            VertexBuffer.Render(_vertOffset, Size);
        }
    }
}
