using System;
using Objects.Geometry;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Objects.Other;
using Speckle.ConnectorUnity;
using Speckle.ConnectorUnity.NativeCache;
using Speckle.ConnectorUnity.Utils;
using Speckle.ConnectorUnity.Wrappers;
using Speckle.Core.Logging;
using Speckle.Core.Models;
using Speckle.Core.Models.GraphTraversal;
using UnityEditor;
using UnityEngine;
using Mesh = UnityEngine.Mesh;
using Object = UnityEngine.Object;
using SMesh = Objects.Geometry.Mesh;
using Transform = UnityEngine.Transform;
using STransform = Objects.Other.Transform;

#nullable enable
namespace Objects.Converter.Unity
{
    public partial class ConverterUnity
    {

        #region helper methods


        /// <summary>
        /// Converts a 3D vector from Speckle's RH Z-up to Unity's LH Y-up coordinate system
        /// </summary>
        /// <returns>Scaled Vector in Unity coordinate space</returns>
        public Vector3 VectorByCoordinates(double x, double y, double z, double scaleFactor = 1d)
        {
            // switch y and z //TODO is this correct? LH -> RH
            return new Vector3((float) (x * scaleFactor), (float) (z * scaleFactor), (float) (y * scaleFactor));
        }

        public Vector3 VectorByCoordinates(double x, double y, double z, string units)
        {
            var f = Speckle.Core.Kits.Units.GetConversionFactor(units, ModelUnits);
            return VectorByCoordinates(x, y, z, f);
        }

        public Vector3 VectorFromPoint(Point p) => VectorByCoordinates(p.x, p.y, p.z, p.units);


        /// <summary>
        /// 
        /// </summary>
        /// <param name="arr"></param>
        /// <returns></returns>
        public Vector3[] ArrayToPoints(IList<double> arr, string units)
        {
            if (arr.Count % 3 != 0) throw new Exception("Array malformed: length%3 != 0.");

            Vector3[] points = new Vector3[arr.Count / 3];
            var f = Speckle.Core.Kits.Units.GetConversionFactor(units, ModelUnits);

            for (int i = 2, k = 0; i < arr.Count; i += 3)
                points[k++] = VectorByCoordinates(arr[i - 2], arr[i - 1], arr[i], f);

            return points;
        }

        #endregion

        #region ToSpeckle

        //TODO: more of these

        /// <summary>
        /// 
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public virtual Point PointToSpeckle(Vector3 p)
        {
            //switch y and z
            return new Point(p.x, p.z, p.y);
        }
        

        #endregion


        #region ToNative

        protected GameObject? NewPointBasedGameObject(Vector3[] points, string name)
        {
            if (points.Length == 0) return null;

            float pointDiameter = 1; //TODO: figure out how best to change this?

            var go = new GameObject();
            go.name = name;

            var lineRenderer = go.AddComponent<LineRenderer>();

            lineRenderer.positionCount = points.Length;
            lineRenderer.SetPositions(points);
            lineRenderer.numCornerVertices = lineRenderer.numCapVertices = 8;
            lineRenderer.startWidth = lineRenderer.endWidth = pointDiameter;

            return go;
        }

        /// <summary>
        /// Converts a Speckle <paramref name="point"/> to a <see cref="GameObject"/> with a <see cref="LineRenderer"/>
        /// </summary>
        /// <param name="point"></param>
        /// <returns></returns>
        public GameObject? PointToNative(Point point)
        {
            Vector3 newPt = VectorByCoordinates(point.x, point.y, point.z, point.units);

            var go = NewPointBasedGameObject(new Vector3[] {newPt, newPt}, point.speckle_type);
            return go;
        }


        /// <summary>
        /// Converts a Speckle <paramref name="line"/> to a <see cref="GameObject"/> with a <see cref="LineRenderer"/>
        /// </summary>
        /// <param name="line"></param>
        /// <returns></returns>
        public GameObject? LineToNative(Line line)
        {
            var points = new List<Vector3> {VectorFromPoint(line.start), VectorFromPoint(line.end)};

            var go = NewPointBasedGameObject(points.ToArray(), line.speckle_type);
            return go;
        }

        /// <summary>
        /// Converts a Speckle <paramref name="polyline"/> to a <see cref="GameObject"/> with a <see cref="LineRenderer"/>
        /// </summary>
        /// <param name="polyline"></param>
        /// <returns></returns>
        public GameObject? PolylineToNative(Polyline polyline)
        {
            var points = polyline.GetPoints().Select(VectorFromPoint);

            var go = NewPointBasedGameObject(points.ToArray(), polyline.speckle_type);
            return go;
        }

        /// <summary>
        /// Converts a Speckle <paramref name="curve"/> to a <see cref="GameObject"/> with a <see cref="LineRenderer"/>
        /// </summary>
        /// <param name="curve"></param>
        /// <returns></returns>
        public GameObject? CurveToNative(Curve curve)
        {
            var points = ArrayToPoints(curve.points, curve.units);
            var go = NewPointBasedGameObject(points, curve.speckle_type);
            return go;
        }
        

        public Dictionary<string, object?> GetProperties(Base o) => GetProperties(o, typeof(Base));

        public Dictionary<string, object?> GetProperties(Base o, Type excludeType)
        {
            var excludeProps = new HashSet<string>(excludeType
                .GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .Select(x => x.Name));

            foreach (string alias in DisplayValuePropertyAliases)
            {
                excludeProps.Add(alias);
            }

            excludeProps.Add("renderMaterial");
            excludeProps.Add("elements");
            excludeProps.Add("name");
            //excludeProps.Add("tag");
            excludeProps.Add("physicsLayer");

            return o.GetMembers()
                .Where(x => !(excludeProps.Contains(x.Key) || excludeProps.Contains(x.Key.TrimStart('@'))))
                .ToDictionary(x => x.Key, x => (object?) x.Value);
        }
        
        

        #endregion
        
        

        private Base CreateSpeckleObjectFromProperties(GameObject go)
        {
            var sd = go.GetComponent<SpeckleProperties>();
            if (sd == null || sd.Data == null)
                return new Base();

            Base sobject = (Base) Activator.CreateInstance(sd.SpeckleType);

            foreach (var key in sd.Data.Keys)
            {
                try
                {
                    sobject[key] = sd.Data[key];
                }
                catch (SpeckleException)
                {
                    // Ignore SpeckleExceptions that may be caused by get only properties
                }
            }

            return sobject;
        }

        public GameObject InstanceToNative(Instance instance)
        {
            if (instance.definition == null) throw new ArgumentException("Definition was null", nameof(instance));

            var defName = CoreUtils.GenerateObjectName(instance.definition);
            // Check for existing converted object
            if(LoadedAssets.TryGetObject(instance.definition, out GameObject? existingGo))
            {
                var go = InstantiateCopy(existingGo);
                go.name = defName;
                TransformToNativeTransform(go.transform, instance.transform);
                return go;
            }

            // Convert the block definition
            GameObject native = new(defName);
            
            List<SMesh> meshes = new();
            List<Base> others = new();

            var geometry = instance.definition is BlockDefinition b
                ? b.geometry
                : GraphTraversal.TraverseMember(new[]
                {
                    instance.definition["elements"] ?? instance.definition["@elements"],
                    instance.definition["displayValue"] ?? instance.definition["@displayValue"],
                });

            foreach (Base geo in geometry)
            {
                if (geo is SMesh m) meshes.Add(m);
                else if (geo is IDisplayValue<List<SMesh>> s) meshes.AddRange(s.displayValue);
                else others.Add(geo);
            }

            if (meshes.Any())
            {
                if(!TryGetMeshFromCache(instance.definition, meshes, out Mesh? nativeMesh, out _))
                {
                    MeshToNativeMesh(meshes, out nativeMesh);
                    string name = CoreUtils.GenerateObjectName(instance.definition);
                    nativeMesh.name = name;
                    LoadedAssets.TrySaveObject(instance.definition, nativeMesh);
                }
                var nativeMaterials = RenderMaterialsToNative(meshes);
                native.SafeMeshSet(nativeMesh, nativeMaterials);
            }

            foreach (Base child in others)
            {
                GameObject? c = ConvertToNativeGameObject(child);
                if (c == null) continue;
                c.transform.SetParent(native.transform, false);
            }
            
            LoadedAssets.TrySaveObject(instance.definition, native);
            
            
            TransformToNativeTransform(native.transform, instance.transform);
            
            var instanceName = CoreUtils.GetFriendlyObjectName(instance) != null
                ? CoreUtils.GenerateObjectName(instance)
                : defName;
            
            native.name = instanceName;
            return native;
        }


        private static GameObject InstantiateCopy(GameObject existingGo)
        {
#if UNITY_EDITOR
            GameObject? prefabInstance = null;
            bool isPrefab = PrefabUtility.GetPrefabAssetType(existingGo) != PrefabAssetType.NotAPrefab;
            if (isPrefab)
            {
                GameObject? prefabAsset = PrefabUtility.GetCorrespondingObjectFromSource(existingGo);
                if (prefabAsset == null) prefabAsset = existingGo;
                prefabInstance = (GameObject) PrefabUtility.InstantiatePrefab(prefabAsset);
            }

            if (prefabInstance != null)
                return prefabInstance;
#endif
            
            return Object.Instantiate(existingGo);
        }


        /// <summary>
        /// Converts a 4x4 transformation matrix from Speckle's <see cref="Other.Transform"/> format,
        /// to a Unity <see cref="Matrix4x4"/>. Applying Z -> Y up conversion, and applying units to the translation 
        /// </summary>
        /// <param name="speckleTransform"></param>
        /// <returns>Transformation matrix in Unity's coordinate system</returns>
        public Matrix4x4 TransformToNativeMatrix(STransform speckleTransform)
        {
            var sf = Speckle.Core.Kits.Units.GetConversionFactor(speckleTransform.units, ModelUnits);
            var smatrix = speckleTransform.matrix;
            
            return new Matrix4x4
            {
                // Left (X -> X)
                [0, 0] = smatrix.M11,
                [2, 0] = smatrix.M21,
                [1, 0] = smatrix.M31,
                [3, 0] = smatrix.M41,

                //Up (Z -> Y)
                [0, 2] = smatrix.M12,
                [2, 2] = smatrix.M22,
                [1, 2] = smatrix.M32,
                [3, 2] = smatrix.M42,

                //Forwards (Y -> Z)
                [0, 1] = smatrix.M13,
                [2, 1] = smatrix.M23,
                [1, 1] = smatrix.M33,
                [3, 1] = smatrix.M43,

                //Translation
                [0, 3] = (float) (smatrix.M14 * sf),
                [2, 3] = (float) (smatrix.M24 * sf),
                [1, 3] = (float) (smatrix.M34 * sf),
                [3, 3] = smatrix.M44,
            };
        }

        public void TransformToNativeTransform(Transform transform, STransform speckleTransform)
        {
            Matrix4x4 matrix = TransformToNativeMatrix(speckleTransform);
            ApplyMatrixToTransform(transform, matrix);
        }

        protected static void ApplyMatrixToTransform(Transform transform, Matrix4x4 m)
        {
            transform.localScale =
                m.lossyScale; //doesn't work for non TRS, maybe we could fallback to squareSum approach (see TransformVectorized::SetFromMatrix in UE src)

            //We can't use m.rotation, as it gives us incorrect results (perhaps because of RH -> LH? or maybe our MatrixToNative is broken?)
            transform.localRotation = Quaternion.LookRotation(
                m.GetColumn(2),
                m.GetColumn(1)
            );
            transform.localPosition = m.GetPosition();
        }
    }

}
