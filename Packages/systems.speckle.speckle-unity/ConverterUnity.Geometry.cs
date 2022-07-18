using System;
using Objects.Geometry;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Objects.Other;
using Objects.Utils;
using Speckle.ConnectorUnity;
using Speckle.Core.Logging;
using Speckle.Core.Models;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Mesh = Objects.Geometry.Mesh;
using SColor = System.Drawing.Color;

#nullable enable
namespace Objects.Converter.Unity
{
    public partial class ConverterUnity
    {

        private static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");
        private static readonly int Metallic = Shader.PropertyToID("_Metallic");
        private static readonly int Glossiness = Shader.PropertyToID("_Glossiness");
    
        #region helper methods
    
        /// <summary>
        /// 
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="z"></param>
        /// <returns></returns>
        public Vector3 VectorByCoordinates(double x, double y, double z, string units)
        {
            // switch y and z
            return new Vector3((float)ScaleToNative(x, units), (float)ScaleToNative(z, units),
                (float)ScaleToNative(y, units));
        }

        public Vector3 VectorFromPoint(Point p)
        {
            // switch y and z
            return new Vector3((float)ScaleToNative(p.x, p.units), (float)ScaleToNative(p.z, p.units),
                (float)ScaleToNative(p.y, p.units));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="ptValues"></param>
        /// <returns></returns>
        // public Vector3 ArrayToPoint(double[] ptValues, string units)
        // {
        //   double x = ptValues[0];
        //   double y = ptValues[1];
        //   double z = ptValues[2];
        //
        //   return PointByCoordinates(x, y, z, units);
        // }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="arr"></param>
        /// <returns></returns>
        public Vector3[] ArrayToPoints(IList<double> arr, string units)
        {
            if (arr.Count % 3 != 0) throw new Exception("Array malformed: length%3 != 0.");

            Vector3[] points = new Vector3[arr.Count / 3];

            for (int i = 2, k = 0; i < arr.Count; i += 3)
                points[k++] = VectorByCoordinates(arr[i - 2], arr[i - 1], arr[i], units);


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
        public Point PointToSpeckle(Vector3 p)
        {
            //switch y and z
            return new Point(p.x, p.z, p.y);
        }


        public List<Mesh>? MeshToSpeckle(MeshFilter? meshFilter)
        {
            if (meshFilter == null) return null;

            var nativeMesh = meshFilter.mesh;
      
            var nTriangles = nativeMesh.triangles;
            List<int> sFaces = new List<int>(nTriangles.Length * 4);
            for (int i = 2; i < nTriangles.Length; i += 3)
            {
                sFaces.Add(0); //Triangle cardinality indicator

                sFaces.Add(nTriangles[i]);
                sFaces.Add(nTriangles[i - 1]);
                sFaces.Add(nTriangles[i - 2]);
            }

            var nVertices = nativeMesh.vertices;
            List<double> sVertices = new List<double>(nVertices.Length * 3);
            foreach (var vertex in nVertices)
            {
                var p = meshFilter.gameObject.transform.TransformPoint(vertex);
                sVertices.Add(p.x);
                sVertices.Add(p.z); //z and y swapped //TODO is this correct? LH -> RH
                sVertices.Add(p.y);
            }
      
            var nColors = nativeMesh.colors;
            List<int> sColors = new List<int>(nColors.Length);
            sColors.AddRange(nColors.Select(c => c.ToIntColor()));

            var nTexCoords = nativeMesh.uv;
            List<double> sTexCoords = new List<double>(nTexCoords.Length * 2);
            foreach (var uv in nTexCoords)
            {
                sTexCoords.Add(uv.x);
                sTexCoords.Add(uv.y);
            }

            var mesh = new Mesh();
      
            mesh.vertices = sVertices;
            mesh.faces = sFaces;
            mesh.colors = sColors;
            mesh.textureCoordinates = sTexCoords;
            mesh.units = ModelUnits;

            return new List<Mesh>() {mesh}; //TODO split mesh by render material
        }
        #endregion

        #region ToNative
        private GameObject? NewPointBasedGameObject(Vector3[] points, string name)
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

            var go = NewPointBasedGameObject(new Vector3[] { newPt, newPt }, point.speckle_type);
            return go;
        }


        /// <summary>
        /// Converts a Speckle <paramref name="line"/> to a <see cref="GameObject"/> with a <see cref="LineRenderer"/>
        /// </summary>
        /// <param name="line"></param>
        /// <returns></returns>
        public GameObject? LineToNative(Line line)
        {
            var points = new List<Vector3> { VectorFromPoint(line.start), VectorFromPoint(line.end) };

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

    
        /// <summary>
        /// Converts multiple <paramref name="meshes"/> (e.g. with different materials) into one native mesh
        /// </summary>
        /// <param name="meshes">Collection of <see cref="Objects.Geometry.Mesh"/>es that shall be converted</param>
        /// <returns>A <see cref="GameObject"/> with the converted <see cref="UnityEngine.Mesh"/>, <see cref="MeshFilter"/>, and <see cref="MeshRenderer"/></returns>
        public GameObject? MeshesToNative(IReadOnlyCollection<Mesh> meshes)
        {
            if (!meshes.Any()) return null;
            
            var go = new GameObject();
            MeshDataToNative(meshes, out var nativeMesh, out var nativeMaterials);

            go.SafeMeshSet(nativeMesh, true);
      
            var meshRenderer = go.AddComponent<MeshRenderer>();

            meshRenderer.sharedMaterials = nativeMaterials;

            return go;
        }


        public Dictionary<string, object?> GetProperties(Base o) => GetProperties(o, typeof(Base));
        public Dictionary<string, object?> GetProperties(Base o, Type excludeType)
        {
            var excludeProps = new HashSet<string>(excludeType.GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .Select(x => x.Name));

            foreach (string alias in DisplayValuePropertyAliases)
            {
                excludeProps.Add(alias);
            }
            excludeProps.Add("renderMaterial");
            excludeProps.Add("elements");
      
            return o.GetMembers()
                .Where(x => !excludeProps.Contains(x.Key))
                .ToDictionary(x => x.Key, x => (object?)x.Value);
        }
    
        /// <summary>
        /// Converts <paramref name="speckleMesh"/> to a <see cref="GameObject"/> with a <see cref="MeshRenderer"/>
        /// </summary>
        /// <param name="speckleMesh">Mesh to convert</param>
        /// <returns></returns>
        public GameObject? MeshToNative(Mesh speckleMesh)
        {
            if (speckleMesh.vertices.Count == 0 || speckleMesh.faces.Count == 0)
            {
                Debug.Log($"Skipping mesh {speckleMesh.id}, mesh data was empty");
                return null;
            }

            GameObject? converted = MeshesToNative(new[] {speckleMesh});
            if(converted != null) AttachSpeckleProperties(converted,speckleMesh.GetType(), GetProperties(speckleMesh, typeof(Mesh)));
            return converted;
        }

        /// <summary> 
        ///
        /// </summary>
        /// <param name="meshes">meshes to be converted as SubMeshes</param>
        /// <param name="nativeMesh">The converted native mesh</param>W
        /// <param name="nativeMaterials">The converted materials (one per converted sub-mesh)</param>
        public void MeshDataToNative(IReadOnlyCollection<Mesh> meshes, out UnityEngine.Mesh nativeMesh, out Material[] nativeMaterials)
        {
            var verts = new List<Vector3>();
      
            var uvs = new List<Vector2>();
            var vertexColors = new List<Color>();
      
            var materials = new List<Material>(meshes.Count);
            var subMeshes = new List<List<int>>(meshes.Count);

            foreach (Mesh m in meshes)
            {
                if(m.vertices.Count == 0 || m.faces.Count == 0 ) continue;
                List<int> tris = new List<int>();
                SubmeshToNative(m, verts, tris, uvs, vertexColors, materials);
                subMeshes.Add(tris);
            }
            nativeMaterials = materials.ToArray();

            Debug.Assert(verts.Count >= 0);
            Debug.Assert(verts.Count >= 0);
            nativeMesh = new UnityEngine.Mesh();
      
      
            //  center transform pivot according to the bounds of the model
            // Bounds meshBounds = new Bounds
            // {
            //  center = verts[0]
            // };
            //
            // foreach (var vert in verts)
            // {
            //  meshBounds.Encapsulate(vert);
            // }
            //
            // // offset mesh vertices
            // for (int l = 0; l < verts.Count; l++)
            // {
            //  verts[l] -= meshBounds.center;
            // }
      
      
            nativeMesh.subMeshCount = subMeshes.Count;
      
            nativeMesh.SetVertices(verts);
            nativeMesh.SetUVs(0, uvs);
            nativeMesh.SetColors(vertexColors);
      
      
            int j = 0;
            foreach(var subMeshTriangles in subMeshes)
            {
                nativeMesh.SetTriangles(subMeshTriangles, j);
                j++;
            }
      
            if (nativeMesh.vertices.Length >= UInt16.MaxValue)
                nativeMesh.indexFormat = IndexFormat.UInt32;
      
            nativeMesh.Optimize();
            nativeMesh.RecalculateBounds();
            nativeMesh.RecalculateNormals();
            nativeMesh.RecalculateTangents();
        }
    
    
        protected void SubmeshToNative(Mesh speckleMesh, List<Vector3> verts, List<int> tris, List<Vector2> texCoords, List<Color> vertexColors, List<Material> materials)
        {
            speckleMesh.AlignVerticesWithTexCoordsByIndex();
            speckleMesh.TriangulateMesh();
      
            int indexOffset = verts.Count;
      
            // Convert Vertices
            verts.AddRange(ArrayToPoints(speckleMesh.vertices, speckleMesh.units));

            // Convert texture coordinates
            bool hasValidUVs = speckleMesh.TextureCoordinatesCount == speckleMesh.VerticesCount;
            if(speckleMesh.textureCoordinates.Count > 0 && !hasValidUVs) Debug.LogWarning($"Expected number of UV coordinates to equal vertices. Got {speckleMesh.TextureCoordinatesCount} expected {speckleMesh.VerticesCount}. \nID = {speckleMesh.id}");
      
            if (hasValidUVs)
            {
                texCoords.Capacity += speckleMesh.TextureCoordinatesCount;
                for (int j = 0; j < speckleMesh.TextureCoordinatesCount; j++)
                {
                    var (u, v) = speckleMesh.GetTextureCoordinate(j);
                    texCoords.Add(new Vector2((float)u,(float)v));
                }
            }
            else if (speckleMesh.bbox != null)
            {
                //Attempt to generate some crude UV coordinates using bbox //TODO this will be broken for submeshes
                texCoords.AddRange(GenerateUV(verts, (float)speckleMesh.bbox.xSize.Length, (float)speckleMesh.bbox.ySize.Length));
            }

            // Convert vertex colors
            if (speckleMesh.colors != null)
            {
                if (speckleMesh.colors.Count == speckleMesh.VerticesCount)
                {
                    vertexColors.AddRange(speckleMesh.colors.Select(c => c.ToUnityColor()));
                }
                else if (speckleMesh.colors.Count != 0)
                {
                    //TODO what if only some submeshes have colors?
                    Debug.LogWarning($"{typeof(Mesh)} {speckleMesh.id} has invalid number of vertex {nameof(Mesh.colors)}. Expected 0 or {speckleMesh.VerticesCount}, got {speckleMesh.colors.Count}");
                }
            }
      
            // Convert faces
            tris.Capacity += (int) (speckleMesh.faces.Count / 4f) * 3;
      
            for (int i = 0; i < speckleMesh.faces.Count; i += 4)
            {
                //We can safely assume all faces are triangles since we called TriangulateMesh
                tris.Add(speckleMesh.faces[i + 1] + indexOffset);
                tris.Add(speckleMesh.faces[i + 3] + indexOffset);
                tris.Add(speckleMesh.faces[i + 2] + indexOffset);
            }
      
            // Convert RenderMaterial
            materials.Add(GetMaterial(speckleMesh["renderMaterial"] as RenderMaterial));
        }
    
    

        private static IEnumerable<Vector2> GenerateUV(IReadOnlyList<Vector3> verts, float xSize, float ySize)
        {
            var uv = new Vector2[verts.Count];
            for (int i = 0; i < verts.Count; i++)
            {

                var vert = verts[i];
                uv[i] = new Vector2(vert.x / xSize, vert.y / ySize);
            }
            return uv;
        }
    
        private static Matrix4x4 UnflattenMatrix(IList<double> flatMatrix)
        {
            Matrix4x4 matrix = new Matrix4x4();
            for(int row = 0; row < 4; row++)
            for(int col = 0; col < 4; col++)
            {
                matrix[row,col] = (float)flatMatrix[row * 4 + col];
            }

            return matrix.transpose;
        }
        #endregion





        private Material GetMaterial(RenderMaterial? renderMaterial)
        {
            //todo support more complex materials
            var shader = Shader.Find("Standard");
            Material mat = new Material(shader);

            //if a renderMaterial is passed use that, otherwise try get it from the mesh itself

            if (renderMaterial == null) return mat;
            
            // 1. match material by name, if any
            Material? matByName = null;
        
            foreach (var _mat in ContextObjects)
            {
                if (((Material)_mat.NativeObject).name == renderMaterial.name)
                {
                    if (matByName == null) matByName = (Material)_mat.NativeObject;
                    else Debug.LogWarning("There is more than one Material with the name \'" + renderMaterial.name + "\'!", (Material)_mat.NativeObject);
                }
            }
            if (matByName != null) return matByName;

            // 2. re-create material by setting diffuse color and transparency on standard shaders
            if (renderMaterial.opacity < 1)
            {
                shader = Shader.Find("Transparent/Diffuse");
                mat = new Material(shader);
            }

            var c = renderMaterial.diffuse.ToUnityColor();
            mat.color = new Color(c.r, c.g, c.b, (float)renderMaterial.opacity);
            mat.name = renderMaterial.name ?? "material-"+ Guid.NewGuid().ToString().Substring(0,8);
        
            mat.SetFloat(Metallic, (float)renderMaterial.metalness);
            mat.SetFloat(Glossiness,  1 - (float)renderMaterial.roughness);

            if (renderMaterial.emissive != SColor.Black.ToArgb()) mat.EnableKeyword ("_EMISSION");
            mat.SetColor(EmissionColor, renderMaterial.emissive.ToUnityColor());
        
        
#if UNITY_EDITOR
            if (StreamManager.GenerateMaterials)
            {
                string name = mat.name.Trim(Path.GetInvalidFileNameChars());
                if (!AssetDatabase.IsValidFolder("Assets/Resources")) AssetDatabase.CreateFolder("Assets", "Resources");
                if (!AssetDatabase.IsValidFolder("Assets/Resources/Materials")) AssetDatabase.CreateFolder("Assets/Resources", "Materials");
                if (!AssetDatabase.IsValidFolder("Assets/Resources/Materials/Speckle Generated")) AssetDatabase.CreateFolder("Assets/Resources/Materials", "Speckle Generated");
          
                if (AssetDatabase.LoadAllAssetsAtPath("Assets/Resources/Materials/Speckle Generated/" + name + ".mat").Length == 0) AssetDatabase.CreateAsset(mat, "Assets/Resources/Materials/Speckle Generated/" + name + ".mat");
          
            }
#endif
        
            return mat;
            // 3. if not renderMaterial was passed, the default shader will be used 
        }

        private SpeckleProperties AttachSpeckleProperties(GameObject go, Type speckleType, IDictionary<string, object?> properties)
        {
            var sd = go.AddComponent<SpeckleProperties>();
            sd.Data = properties;
            sd.SpeckleType = speckleType;
            return sd;
        }


        private Base CreateSpeckleObjectFromProperties(GameObject go)
        {
            var sd = go.GetComponent<SpeckleProperties>();
            if (sd == null || sd.Data == null)
                return new Base();
            
            Base sobject = (Base)Activator.CreateInstance(sd.SpeckleType);
            
            foreach (var key in sd.Data.Keys)
            {
                try
                {
                    sobject[key] = sd.Data[key];
                }
                catch(SpeckleException)
                {
                    // Ignore SpeckleExceptions that may be caused by get only properties
                }
            }

            return sobject;
        }
    }
}