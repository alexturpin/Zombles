﻿using OpenTK;

using Zombles.Graphics;

namespace Zombles.Entities
{
    public class Render3D : Component
    {
        private Vector3 _lastPos;
        private Quaternion _rotation;

        private Matrix4 _transform;
        private bool _transformInvalid;

        public Quaternion Rotation
        {
            get { return _rotation; }
            set
            {
                _rotation = value;
                _transformInvalid = true;
            }
        }

        public EntityModel Model { get; set; }

        public Render3D(Entity ent)
            : base(ent)
        {
            _transform = Matrix4.Identity;
            _transformInvalid = true;

            Rotation = Quaternion.Identity;
            Model = null;
        }

        public virtual void OnRender(ModelEntityShader shader)
        {
            if (_transformInvalid || _lastPos != Position) {
                _transform = Matrix4.Mult(
                    Matrix4.CreateTranslation(Entity.Position),
                    Matrix4.CreateFromQuaternion(Rotation));

                _transformInvalid = false;
                _lastPos = Position;
            }

            if (Model != null) {
                shader.Render(Model, _transform);
            }
        }
    }
}