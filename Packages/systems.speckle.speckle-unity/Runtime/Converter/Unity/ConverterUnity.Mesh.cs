using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Speckle.ConnectorUnity.Utils;
using Objects.Utils;
using Speckle.Core.Models;
using UnityEngine;
using UnityEngine.Rendering;
using Material = UnityEngine.Material;
using Mesh = UnityEngine.Mesh;
using SMesh = Objects.Geometry.Mesh;
using Transform = UnityEngine.Transform;

#nullable enable
namespace Objects.Converter.Unity
{
    public partial class ConverterUnity
    {
        #region ToSpeckle

        public virtual List<SMesh>? MeshToSpeckle(MeshFilter meshFilter)
        {
#if UNITY_EDITOR
            Material[]? materials = meshFilter.GetComponent<Renderer>()?.sharedMaterials;
            var nativeMesh = meshFilter.sharedMesh;
#else
            Material[]? materials = meshFilter.GetComponent<Renderer>()?.materials;
            var nativeMesh = meshFilter.mesh;
#endif
            if (nativeMesh == null)
                return null;

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
                        converted = SubMeshToSpeckle(
                            nativeMesh,
                            meshFilter.transform,
                            subMesh,
                            i,
                            3
                        );
                        convertedMeshes.Add(converted);
                        break;
                    case MeshTopology.Quads:
                        converted = SubMeshToSpeckle(
                            nativeMesh,
                            meshFilter.transform,
                            subMesh,
                            i,
                            4
                        );
                        convertedMeshes.Add(converted);
                        break;
                    default:
                        Debug.LogError(
                            $"Failed to convert submesh {i} of {typeof(GameObject)} {meshFilter.gameObject.name} to Speckle, Unsupported Mesh Topography {subMesh.topology}. Submesh will be ignored."
                        );
                        continue;
                }

                if (materials == null || materials.Length <= i)
                    continue;

                Material mat = materials[i];
                if (mat != null)
                    converted["renderMaterial"] = MaterialToSpeckle(mat);
            }

            return convertedMeshes;
        }

        protected virtual SMesh SubMeshToSpeckle(
            Mesh nativeMesh,
            Transform instanceTransform,
            SubMeshDescriptor subMesh,
            int subMeshIndex,
            int faceN
        )
        {
            var nFaces = nativeMesh.GetIndices(subMeshIndex, true);
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

            var nColors = nativeMesh.colors.Skip(indexOffset).Take(vertexTake).ToArray();

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

        #endregion


        #region ToNative

        /// <summary>
        /// Converts multiple <paramref name="meshes"/> (e.g. with different materials) into one native mesh
        /// </summary>
        /// <param name="element">The <see cref="Base"/> object being converted</param>
        /// <param name="meshes">Collection of <see cref="Objects.Geometry.Mesh"/>es that shall be converted</param>
        /// <returns>A <see cref="GameObject"/> with the converted <see cref="UnityEngine.Mesh"/>, <see cref="MeshFilter"/>, and <see cref="MeshRenderer"/></returns>
        public GameObject MeshesToNative(Base element, IReadOnlyCollection<SMesh> meshes)
        {
            if (!meshes.Any())
                throw new ArgumentException("Expected at least one Mesh", nameof(meshes));

            Material[] nativeMaterials = RenderMaterialsToNative(meshes);

            if (!TryGetMeshFromCache(element, meshes, out Mesh? nativeMesh, out Vector3 center))
            {
                //Convert a new one
                MeshToNativeMesh(meshes, out nativeMesh, out center);
                string name = CoreUtils.GenerateObjectName(element);
                nativeMesh.name = name;
                LoadedAssets.TrySaveObject(element, nativeMesh);
            }

            var go = new GameObject();
            go.transform.position = center;
            go.SafeMeshSet(nativeMesh, nativeMaterials);

            return go;
        }

        /// <summary>
        /// Converts <paramref name="speckleMesh"/> to a <see cref="GameObject"/> with a <see cref="MeshRenderer"/>
        /// </summary>
        /// <param name="speckleMesh">Mesh to convert</param>
        /// <returns></returns>
        public GameObject MeshToNative(SMesh speckleMesh)
        {
            if (speckleMesh.vertices.Count == 0 || speckleMesh.faces.Count == 0)
                throw new ArgumentException("mesh data was empty", nameof(speckleMesh));

            GameObject converted = MeshesToNative(speckleMesh, new[] { speckleMesh });

            // Raw meshes shouldn't have dynamic props to attach
            //if (converted != null) AttachSpeckleProperties(converted,speckleMesh.GetType(), GetProperties(speckleMesh, typeof(Mesh)));

            return converted;
        }

        protected bool TryGetMeshFromCache(
            Base element,
            IReadOnlyCollection<SMesh> meshes,
            [NotNullWhen(true)] out Mesh? nativeMesh,
            out Vector3 center
        )
        {
            if (LoadedAssets.TryGetObject(element, out Mesh? existing))
            {
                nativeMesh = existing;
                //todo This is pretty inefficient, having to convert the mesh data anyway just to get the center... eek
                MeshDataToNative(meshes, out List<Vector3> verts, out _, out _, out _);
                center = CalculateBounds(verts).center;
                return true;
            }

            nativeMesh = default;
            center = default;
            return false;
        }

        /// <summary>
        /// Converts Speckle <see cref="SMesh"/>s as a native <see cref="Mesh"/> object
        /// </summary>
        /// <param name="meshes">meshes to be converted as SubMeshes</param>
        /// <param name="nativeMesh">The converted native mesh</param>
        public void MeshToNativeMesh(IReadOnlyCollection<SMesh> meshes, out Mesh nativeMesh) =>
            MeshToNativeMesh(meshes, out nativeMesh, out _, false);

        /// <inheritdoc cref="MeshToNativeMesh(IReadOnlyCollection{Objects.Geometry.Mesh},out Mesh)"/>
        /// <param name="recenterVerts">when true, will recenter vertices</param>
        /// <param name="center">Center position for the mesh</param>
        public void MeshToNativeMesh(
            IReadOnlyCollection<SMesh> meshes,
            out Mesh nativeMesh,
            out Vector3 center,
            bool recenterVerts = true
        )
        {
            MeshDataToNative(
                meshes,
                out List<Vector3> verts,
                out List<Vector2> uvs,
                out List<Color> vertexColors,
                out List<List<int>> subMeshes
            );

            Debug.Assert(verts.Count >= 0);

            center = recenterVerts ? RecenterVertices(verts) : Vector3.zero;

            nativeMesh = new Mesh();
            nativeMesh.subMeshCount = subMeshes.Count;
            nativeMesh.SetVertices(verts);
            nativeMesh.SetUVs(0, uvs);
            nativeMesh.SetColors(vertexColors);

            int j = 0;
            foreach (var subMeshTriangles in subMeshes)
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

        public void MeshDataToNative(
            IReadOnlyCollection<SMesh> meshes,
            out List<Vector3> verts,
            out List<Vector2> uvs,
            out List<Color> vertexColors,
            out List<List<int>> subMeshes
        )
        {
            verts = new List<Vector3>();
            uvs = new List<Vector2>();
            vertexColors = new List<Color>();
            subMeshes = new List<List<int>>(meshes.Count);

            foreach (SMesh m in meshes)
            {
                if (m.vertices.Count == 0 || m.faces.Count == 0)
                    continue;

                List<int> tris = new List<int>();
                SubmeshToNative(m, verts, tris, uvs, vertexColors);
                subMeshes.Add(tris);
            }
        }

        protected void SubmeshToNative(
            SMesh speckleMesh,
            List<Vector3> verts,
            List<int> tris,
            List<Vector2> texCoords,
            List<Color> vertexColors
        )
        {
            speckleMesh.AlignVerticesWithTexCoordsByIndex();
            speckleMesh.TriangulateMesh();

            int indexOffset = verts.Count;

            // Convert Vertices
            verts.AddRange(ArrayToPoints(speckleMesh.vertices, speckleMesh.units));

            // Convert texture coordinates
            bool hasValidUVs = speckleMesh.TextureCoordinatesCount == speckleMesh.VerticesCount;
            if (speckleMesh.textureCoordinates.Count > 0 && !hasValidUVs)
                Debug.LogWarning(
                    $"Expected number of UV coordinates to equal vertices. Got {speckleMesh.TextureCoordinatesCount} expected {speckleMesh.VerticesCount}. \nID = {speckleMesh.id}"
                );

            if (hasValidUVs)
            {
                texCoords.Capacity += speckleMesh.TextureCoordinatesCount;
                for (int j = 0; j < speckleMesh.TextureCoordinatesCount; j++)
                {
                    var (u, v) = speckleMesh.GetTextureCoordinate(j);
                    texCoords.Add(new Vector2((float)u, (float)v));
                }
            }
            else if (speckleMesh.bbox != null)
            {
                // Attempt to generate some crude UV coordinates using bbox
                texCoords.AddRange(
                    GenerateUV(
                        indexOffset,
                        verts,
                        (float)speckleMesh.bbox.xSize.Length,
                        (float)speckleMesh.bbox.ySize.Length
                    )
                );
            }
            else
            {
                texCoords.AddRange(Enumerable.Repeat(Vector2.zero, verts.Count - indexOffset));
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
                    Debug.LogWarning(
                        $"{typeof(SMesh)} {speckleMesh.id} has invalid number of vertex {nameof(SMesh.colors)}. Expected 0 or {speckleMesh.VerticesCount}, got {speckleMesh.colors.Count}"
                    );
                }
            }

            // Convert faces
            tris.Capacity += (int)(speckleMesh.faces.Count / 4f) * 3;

            for (int i = 0; i < speckleMesh.faces.Count; i += 4)
            {
                // We can safely assume all faces are triangles since we called TriangulateMesh
                tris.Add(speckleMesh.faces[i + 1] + indexOffset);
                tris.Add(speckleMesh.faces[i + 3] + indexOffset);
                tris.Add(speckleMesh.faces[i + 2] + indexOffset);
            }
        }

        protected static IEnumerable<Vector2> GenerateUV(
            int indexOffset,
            IReadOnlyList<Vector3> verts,
            float xSize,
            float ySize
        )
        {
            var uv = new Vector2[verts.Count - indexOffset];
            for (int i = 0; i < verts.Count - indexOffset; i++)
            {
                var vert = verts[i];
                uv[i] = new Vector2(vert.x / xSize, vert.y / ySize);
            }

            return uv;
        }

        protected static Vector3 RecenterVertices(IList<Vector3> vertices)
        {
            if (!vertices.Any())
                return Vector3.zero;

            Bounds meshBounds = CalculateBounds(vertices);

            for (int i = 0; i < vertices.Count; i++)
                vertices[i] -= meshBounds.center;

            return meshBounds.center;
        }

        protected static Bounds CalculateBounds(IList<Vector3> points)
        {
            Bounds b = new Bounds { center = points[0] };

            foreach (var p in points)
                b.Encapsulate(p);

            return b;
        }

        #endregion
    }
}
