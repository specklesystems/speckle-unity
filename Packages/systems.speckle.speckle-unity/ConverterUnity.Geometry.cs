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
using Material = UnityEngine.Material;
using Mesh = UnityEngine.Mesh;
using Object = UnityEngine.Object;
using SMesh = Objects.Geometry.Mesh;
using SColor = System.Drawing.Color;
using Transform = UnityEngine.Transform;
using STransform = Objects.Other.Transform;

#nullable enable
namespace Objects.Converter.Unity
{
    public partial class ConverterUnity
    {

        protected static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");
        protected static readonly int Metallic = Shader.PropertyToID("_Metallic");
        protected static readonly int Glossiness = Shader.PropertyToID("_Glossiness");
    
        #region helper methods
    

        
        public Vector3 VectorByCoordinates(double x, double y, double z, double scaleFactor)
        {
            // switch y and z //TODO is this correct? LH -> RH
            return new Vector3((float)(x * scaleFactor), (float)(z * scaleFactor), (float)(y * scaleFactor));
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

        
        
        
        
        public virtual List<SMesh>? MeshToSpeckle(MeshFilter meshFilter)
        {
            Material[]? materials = meshFilter.GetComponent<Renderer>()?.materials; 
#if UNITY_EDITOR
            var nativeMesh = meshFilter.sharedMesh;
#else
            var nativeMesh = meshFilter.mesh;
#endif
            if (nativeMesh == null) return null;
            
            List<SMesh> convertedMeshes = new List<SMesh>(nativeMesh.subMeshCount);
            for (int i = 0; i < nativeMesh.subMeshCount; i++)
            {
                var subMesh = nativeMesh.GetSubMesh(i);
                SMesh converted;
                switch (subMesh.topology)
                {
                    // case MeshTopology.Points:
                    //     //TODO convert as pointcloud
                    //     continue;
                    case MeshTopology.Triangles:
                        converted = SubMeshToSpeckle(nativeMesh, meshFilter.transform, subMesh, i, 3);
                        convertedMeshes.Add(converted);
                        break;
                    case MeshTopology.Quads:
                        converted = SubMeshToSpeckle(nativeMesh, meshFilter.transform, subMesh, i, 4);
                        convertedMeshes.Add(converted);
                        break;
                    default:
                        Debug.LogError($"Failed to convert submesh {i} of {typeof(GameObject)} {meshFilter.gameObject.name} to Speckle, Unsupported Mesh Topography {subMesh.topology}. Submesh will be ignored.");
                        continue;
                }

                if (materials == null || materials.Length <= i) continue;
                
                Material mat = materials[i]; 
                if(mat != null) converted["renderMaterial"] = MaterialToSpeckle(mat);
            }
            
            return convertedMeshes;
        }


        protected virtual SMesh SubMeshToSpeckle(Mesh nativeMesh, Transform instanceTransform, SubMeshDescriptor subMesh, int subMeshIndex, int faceN)
        {
            var nFaces = nativeMesh.GetIndices( subMeshIndex, true);
            int numFaces = nFaces.Length / faceN;
            List<int> sFaces = new List<int>(numFaces * (faceN + 1));

            int indexOffset = subMesh.firstVertex;
            
            // int i = 0;
            // int j = 0;
            // while (i < nFaces.Length)
            // {
            //     if (j == 0)
            //     {
            //         sFaces.Add(faceN);
            //         j = faceN;
            //     }
            //     sFaces.Add(nFaces[i] - indexOffset);
            //     j--;
            //     i++;
            // }
            
            int i = nFaces.Length - 1;
            int j = 0;
            while (i >= 0) //Traverse backwards to ensure CCW face orientation
            {
                if (j == 0)
                {
                    //Add face cardinality indicator ever
                    sFaces.Add(faceN);
                    j = faceN;
                }
                sFaces.Add(nFaces[i] - indexOffset);
                j--;
                i--;
            }
            
            int vertexTake = subMesh.vertexCount;
            var nVertices = nativeMesh.vertices.Skip(indexOffset).Take(vertexTake);
            List<double> sVertices = new List<double>(subMesh.vertexCount * 3); 
            foreach (var vertex in nVertices) 
            {
                var p = instanceTransform.TransformPoint(vertex); 
                sVertices.Add(p.x);
                sVertices.Add(p.z); //z and y swapped //TODO is this correct? LH -> RH
                sVertices.Add(p.y);
            }
            
            var nColors = nativeMesh.colors.Skip(indexOffset).Take(vertexTake).ToArray();;
            List<int> sColors = new List<int>(nColors.Length);
            sColors.AddRange(nColors.Select(c => c.ToIntColor()));
            
            var nTexCoords = nativeMesh.uv.Skip(indexOffset).Take(vertexTake).ToArray();
            List<double> sTexCoords = new List<double>(nTexCoords.Length * 2);
            foreach (var uv in nTexCoords)
            {
                sTexCoords.Add(uv.x);
                sTexCoords.Add(uv.y);
            }
            
            var convertedMesh = new SMesh
            {
                vertices = sVertices,
                faces = sFaces,
                colors = sColors,
                textureCoordinates = sTexCoords,
                units = ModelUnits
            };
            
            return convertedMesh;
        }

        protected static HashSet<string> SupportedShadersToSpeckle = new()
        {
            "Legacy Shaders/Transparent/Diffuse", "Standard"
        };
        public virtual RenderMaterial MaterialToSpeckle(Material nativeMaterial)
        {
            //Warning message for unknown shaders
            if(!SupportedShadersToSpeckle.Contains(nativeMaterial.shader.name)) Debug.LogWarning($"Material Shader \"{nativeMaterial.shader.name}\" is not explicitly supported, the resulting material may be incorrect");
            
            var color = nativeMaterial.color;
            var opacity = 1f;
            if (nativeMaterial.shader.name.ToLower().Contains("transparent"))
            {
                opacity = color.a;
                color.a = 255;
            }

            var emissive = nativeMaterial.IsKeywordEnabled("_EMISSION")
                ? nativeMaterial.GetColor(EmissionColor).ToIntColor()
                : SColor.Black.ToArgb();
            
            var materialName = !string.IsNullOrWhiteSpace(nativeMaterial.name)
                ? nativeMaterial.name.Replace("(Instance)", string.Empty).TrimEnd()
                : $"material-{Guid.NewGuid().ToString().Substring(0, 8)}";

            var metalness = nativeMaterial.HasProperty(Metallic)
                ? nativeMaterial.GetFloat(Metallic)
                : 0;
            
            var roughness = nativeMaterial.HasProperty(Glossiness)
                ? 1 - nativeMaterial.GetFloat(Glossiness)
                : 1;

            return new RenderMaterial
            {
                name = materialName,
                diffuse = color.ToIntColor(),
                opacity = opacity,
                metalness = metalness,
                roughness = roughness,
                emissive = emissive,
            };
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
        public GameObject? MeshesToNative(IReadOnlyCollection<SMesh> meshes)
        {
            if (!meshes.Any()) return null;
            
            var go = new GameObject();
            MeshDataToNative(meshes, out var nativeMesh, out var nativeMaterials, out var center);
            
            go.transform.position = center;
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
            excludeProps.Add("name");
            //excludeProps.Add("tag");
            excludeProps.Add("physicsLayer");
      
            return o.GetMembers()
                .Where(x => !(excludeProps.Contains(x.Key) || excludeProps.Contains(x.Key.TrimStart('@'))))
                .ToDictionary(x => x.Key, x => (object?)x.Value);
        }
    
        /// <summary>
        /// Converts <paramref name="speckleMesh"/> to a <see cref="GameObject"/> with a <see cref="MeshRenderer"/>
        /// </summary>
        /// <param name="speckleMesh">Mesh to convert</param>
        /// <returns></returns>
        public GameObject? MeshToNative(SMesh speckleMesh)
        {
            if (speckleMesh.vertices.Count == 0 || speckleMesh.faces.Count == 0)
            {
                Debug.Log($"Skipping mesh {speckleMesh.id}, mesh data was empty");
                return null;
            }

            GameObject? converted = MeshesToNative(new[] {speckleMesh});
            //if(converted != null) AttachSpeckleProperties(converted,speckleMesh.GetType(), GetProperties(speckleMesh, typeof(Mesh)));
            return converted;
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="meshes">meshes to be converted as SubMeshes</param>
        /// <param name="nativeMesh">The converted native mesh</param>
        /// <param name="nativeMaterials">The converted materials (one per converted sub-mesh)</param>
        /// <param name="center">Center position for the mesh</param>
        public void MeshDataToNative(IReadOnlyCollection<SMesh> meshes, out Mesh nativeMesh, out Material[] nativeMaterials, out Vector3 center)
        {
             var verts = new List<Vector3>();

             var uvs = new List<Vector2>();
             var vertexColors = new List<Color>();
             
            var materials = new List<Material>(meshes.Count);
            var subMeshes = new List<List<int>>(meshes.Count);

            foreach (SMesh m in meshes)
            {
                if(m.vertices.Count == 0 || m.faces.Count == 0 ) continue;
                List<int> tris = new List<int>();
                SubmeshToNative(m, verts, tris, uvs, vertexColors, materials);
                subMeshes.Add(tris);
            }
            nativeMaterials = materials.ToArray();

            Debug.Assert(verts.Count >= 0);
            Debug.Assert(verts.Count >= 0);
            nativeMesh = new Mesh();
      
            RecenterVertices(verts, out center);

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
    
    
        protected void SubmeshToNative(SMesh speckleMesh, List<Vector3> verts, List<int> tris, List<Vector2> texCoords, List<Color> vertexColors, List<Material> materials)
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
                    Debug.LogWarning($"{typeof(SMesh)} {speckleMesh.id} has invalid number of vertex {nameof(SMesh.colors)}. Expected 0 or {speckleMesh.VerticesCount}, got {speckleMesh.colors.Count}");
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
            materials.Add(RenderMaterialToNative(speckleMesh["renderMaterial"] as RenderMaterial));
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

        public static void RecenterVertices(List<Vector3> vertices, out Vector3 center)
        {
          center = Vector3.zero;

          if (vertices == null || !vertices.Any()) return;

          Bounds meshBounds = new Bounds { center = vertices[0] };

          foreach (var vert in vertices)
            meshBounds.Encapsulate(vert);

          center = meshBounds.center;

          for (int i = 0; i < vertices.Count; i++)
            vertices[i] -= meshBounds.center;
        }

        private Material RenderMaterialToNative(RenderMaterial? renderMaterial)
        {
            //todo support more complex materials
            var shader = Shader.Find("Standard");
            Material mat = new Material(shader);

            //if a renderMaterial is passed use that, otherwise try get it from the mesh itself
            if (renderMaterial == null) return mat;
            
            // 1. match material by name, if any
            string materialName = string.IsNullOrWhiteSpace(renderMaterial.name)
                ? $"material-{renderMaterial.id}"
                : renderMaterial.name.Replace('/', '-');

            if (LoadedAssets.TryGetValue(materialName, out Object asset)
                && asset is Material loadedMaterial) return loadedMaterial;
            
            // 2. re-create material by setting diffuse color and transparency on standard shaders
            if (renderMaterial.opacity < 1)
            {
                shader = Shader.Find("Transparent/Diffuse");
                mat = new Material(shader);
            }

            var c = renderMaterial.diffuse.ToUnityColor();
            mat.color = new Color(c.r, c.g, c.b, (float)renderMaterial.opacity);
            mat.name = materialName;
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
          
                if (AssetDatabase.LoadAllAssetsAtPath($"Assets/Resources/Materials/Speckle Generated/" + name + ".mat").Length == 0) AssetDatabase.CreateAsset(mat, "Assets/Resources/Materials/Speckle Generated/" + name + ".mat");
          
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