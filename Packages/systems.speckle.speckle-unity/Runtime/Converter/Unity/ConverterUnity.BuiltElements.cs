using Objects.BuiltElements;
using UnityEngine;

namespace Objects.Converter.Unity
{
    public partial class ConverterUnity
    {
        [Tooltip("Enable/Disable the converting of Speckle View objects to Unity Cameras")]
        public bool shouldConvertViews;

        public GameObject View3DToNative(View3D speckleView)
        {
            var go = new GameObject(speckleView.name);
            go.AddComponent<Camera>();

            var matrix = View3DToMatrix(speckleView).transpose;
            ApplyMatrixToTransform(go.transform, matrix);

            AttachSpeckleProperties(go, speckleView.GetType(), () => speckleView.GetMembers());
            return go;
        }

        protected Matrix4x4 View3DToMatrix(View3D view)
        {
            var sf = GetConversionFactor(view.units);
            var tx = (float)(view.origin.x * sf);
            var ty = (float)(view.origin.z * sf); //Y up -> Z up coordinate transformation
            var tz = (float)(view.origin.y * sf);

            var forward = new Vector3(
                (float)view.forwardDirection.x,
                (float)view.forwardDirection.z,
                (float)view.forwardDirection.y
            );
            var up = new Vector3(
                (float)view.upDirection.x,
                (float)view.upDirection.z,
                (float)view.upDirection.y
            );
            var right = Vector3.Cross(forward, up).normalized;

            return new Matrix4x4(
                new Vector4(right.x, up.x, forward.x, tx),
                new Vector4(right.y, up.y, forward.y, ty),
                new Vector4(right.z, up.z, forward.z, tz),
                new Vector4(0, 0, 0, 1)
            );
        }
    }
}
