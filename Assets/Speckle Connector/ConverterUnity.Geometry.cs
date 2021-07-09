using Objects.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Objects.Other;
using Objects.Primitive;
using Speckle.ConnectorUnity;
using Speckle.Core.Models;
using UnityEditor;
using UnityEngine;
using Mesh = Objects.Geometry.Mesh;
using Object = UnityEngine.Object;

namespace Objects.Converter.Unity {
    public partial class ConverterUnity {
    #region helper methods
        /// <summary>
        /// 
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="z"></param>
        /// <returns></returns>
        public Vector3 VectorByCoordinates( double x, double y, double z, string units )
            {
                // switch y and z
                return new Vector3( (float) ScaleToNative( x, units ), (float) ScaleToNative( z, units ),
                    (float) ScaleToNative( y, units ) );
            }

        public Vector3 VectorFromPoint( Point p )
            {
                // switch y and z
                return new Vector3( (float) ScaleToNative( p.x, p.units ), (float) ScaleToNative( p.z, p.units ),
                    (float) ScaleToNative( p.y, p.units ) );
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
        public Vector3[ ] ArrayToPoints( IEnumerable<double> arr, string units )
            {
                if ( arr.Count( ) % 3 != 0 ) throw new Exception( "Array malformed: length%3 != 0." );

                Vector3[ ] points = new Vector3[ arr.Count( ) / 3 ];
                var asArray = arr.ToArray( );
                for ( int i = 2, k = 0; i < arr.Count( ); i += 3 )
                    points[ k++ ] = VectorByCoordinates( asArray[ i - 2 ], asArray[ i - 1 ], asArray[ i ], units );


                return points;
            }

        public Vector3[ ] ArrayToPoints( IEnumerable<double> arr, string units, out Vector2[ ] uv )
            {
                uv = null;
                if ( arr.Count( ) % 3 != 0 ) throw new Exception( "Array malformed: length%3 != 0." );

                Vector3[ ] points = new Vector3[ arr.Count( ) / 3 ];
                uv = new Vector2[ points.Length ];

                var asArray = arr.ToArray( );
                for ( int i = 2, k = 0; i < arr.Count( ); i += 3 ) {

                    points[ k++ ] = VectorByCoordinates( asArray[ i - 2 ], asArray[ i - 1 ], asArray[ i ], units );
                }


                // get size of mesh
                for ( int i = 0; i < points.Length; i++ ) { }

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
        public Point PointToSpeckle( Vector3 p )
            {
                //switch y and z
                return new Point( p.x, p.z, p.y );
            }


        /// <summary>
        /// Converts a Speckle mesh to a GameObject with a mesh renderer
        /// </summary>
        /// <param name="speckleMesh"></param>
        /// <returns></returns>
        public Mesh MeshToSpeckle( GameObject go )
            {
                //TODO: support multiple filters?
                var filter = go.GetComponent<MeshFilter>( );
                if ( filter == null ) {
                    return null;
                }

                //convert triangle array into speckleMesh faces     
                List<int> faces = new List<int>( );
                int i = 0;
                //store them here, makes it like 1000000x faster?
                var triangles = filter.mesh.triangles;
                while (i < triangles.Length) {
                    faces.Add( 0 );

                    faces.Add( triangles[ i + 0 ] );
                    faces.Add( triangles[ i + 2 ] );
                    faces.Add( triangles[ i + 1 ] );
                    i += 3;
                }

                var mesh = new Mesh( );
                // get the speckle data from the go here
                // so that if the go comes from speckle, typed props will get overridden below
                AttachUnityProperties( mesh, go );

                mesh.units = ModelUnits;

                var vertices = filter.mesh.vertices;
                foreach ( var vertex in vertices ) {
                    var p = go.transform.TransformPoint( vertex );
                    var sp = PointToSpeckle( p );
                    mesh.vertices.Add( sp.x );
                    mesh.vertices.Add( sp.y );
                    mesh.vertices.Add( sp.z );
                }

                mesh.faces = faces;

                return mesh;
            }
    #endregion

    #region ToNative
        private GameObject NewPointBasedGameObject( Vector3[ ] points, string name )
            {
                if ( points.Length == 0 ) return null;

                float pointDiameter = 1; //TODO: figure out how best to change this?

                var go = new GameObject( );
                go.name = name;

                var lineRenderer = go.AddComponent<LineRenderer>( );

                lineRenderer.positionCount = points.Length;
                lineRenderer.SetPositions( points );
                lineRenderer.numCornerVertices = lineRenderer.numCapVertices = 8;
                lineRenderer.startWidth = lineRenderer.endWidth = pointDiameter;

                return go;
            }

        /// <summary>
        /// Converts a Speckle point to a GameObject with a line renderer
        /// </summary>
        /// <param name="point"></param>
        /// <returns></returns>
        public GameObject PointToNative( Point point )
            {
                Vector3 newPt = VectorByCoordinates( point.x, point.y, point.z, point.units );

                var go = NewPointBasedGameObject( new Vector3[ 2 ] {newPt, newPt}, point.speckle_type );
                return go;
            }


        /// <summary>
        /// Converts a Speckle line to a GameObject with a line renderer
        /// </summary>
        /// <param name="line"></param>
        /// <returns></returns>
        public GameObject LineToNative( Line line )
            {
                var points = new List<Vector3> {VectorFromPoint( line.start ), VectorFromPoint( line.end )};

                var go = NewPointBasedGameObject( points.ToArray( ), line.speckle_type );
                return go;
            }

        /// <summary>
        /// Converts a Speckle polyline to a GameObject with a line renderer
        /// </summary>
        /// <param name="polyline"></param>
        /// <returns></returns>
        public GameObject PolylineToNative( Polyline polyline )
            {
                var points = polyline.points.Select( x => VectorFromPoint( x ) );

                var go = NewPointBasedGameObject( points.ToArray( ), polyline.speckle_type );
                return go;
            }

        /// <summary>
        /// Converts a Speckle curve to a GameObject with a line renderer
        /// </summary>
        /// <param name="curve"></param>
        /// <returns></returns>
        public GameObject CurveToNative( Curve curve )
            {
                var points = ArrayToPoints( curve.points, curve.units );
                var go = NewPointBasedGameObject( points.ToArray( ), curve.speckle_type );
                return go;
            }


        public GameObject MeshToNative( Base speckleMeshObject )
            {
                if ( !( speckleMeshObject[ "displayMesh" ] is Mesh ) )
                    return null;

                return MeshToNative( speckleMeshObject[ "displayMesh" ] as Mesh,
                    speckleMeshObject[ "renderMaterial" ] as RenderMaterial, speckleMeshObject.GetMembers( ) );
            }
        /// <summary>
        /// Converts a Speckle mesh to a GameObject with a mesh renderer
        /// </summary>
        /// <param name="speckleMesh">Mesh to convert</param>
        /// <param name="renderMaterial">If provided will override the renderMaterial on the mesh itself</param>
        /// <param name="properties">If provided will override the properties on the mesh itself</param>
        /// <returns></returns>
        public GameObject MeshToNative(
            Mesh speckleMesh, RenderMaterial renderMaterial = null,
            Dictionary<string, object> properties = null
        )
            {
                if ( speckleMesh.vertices.Count == 0 || speckleMesh.faces.Count == 0 ) {
                    return null;
                }

                var recenterMeshTransforms = true; //TODO: figure out how best to change this?


                var verts = ArrayToPoints( speckleMesh.vertices, speckleMesh.units );


                //convert speckleMesh.faces into triangle array           
                List<int> tris = new List<int>( );
                int i = 0;
                // TODO: Check if this is causing issues with normals for mesh 
                while (i < speckleMesh.faces.Count) {
                    if ( speckleMesh.faces[ i ] == 0 ) {
                        //Triangles
                        tris.Add( speckleMesh.faces[ i + 1 ] );
                        tris.Add( speckleMesh.faces[ i + 3 ] );
                        tris.Add( speckleMesh.faces[ i + 2 ] );
                        i += 4;
                    } else {
                        //Quads to triangles
                        tris.Add( speckleMesh.faces[ i + 1 ] );
                        tris.Add( speckleMesh.faces[ i + 3 ] );
                        tris.Add( speckleMesh.faces[ i + 2 ] );

                        tris.Add( speckleMesh.faces[ i + 1 ] );
                        tris.Add( speckleMesh.faces[ i + 4 ] );
                        tris.Add( speckleMesh.faces[ i + 3 ] );

                        i += 5;
                    }
                }


                var go = new GameObject {name = speckleMesh.speckle_type};
                var mesh = new UnityEngine.Mesh {name = speckleMesh.speckle_type};

                if ( verts.Length >= 65535 )
                    mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;


                // center transform pivot according to the bounds of the model
                if ( recenterMeshTransforms ) {
                    Bounds meshBounds = new Bounds {
                        center = verts[ 0 ]
                    };

                    foreach ( var vert in verts ) {
                        meshBounds.Encapsulate( vert );
                    }

                    go.transform.position = meshBounds.center;

                    // offset mesh vertices
                    for ( int l = 0; l < verts.Length; l++ ) {
                        verts[ l ] -= meshBounds.center;
                    }
                }


                mesh.SetVertices( verts );
                mesh.SetTriangles( tris, 0 );

                if ( speckleMesh.bbox != null ) {
                    var uv = GenerateUV( verts, (float) speckleMesh.bbox.xSize.Length, (float) speckleMesh.bbox.ySize.Length ).ToList( );
                    mesh.SetUVs( 0, uv );

                }

                // BUG: causing some funky issues with meshes
                // mesh.RecalculateNormals( );
                mesh.Optimize( );
                // Setting mesh to filter once all mesh modifying is done
                go.SafeMeshSet( mesh, true );


                var meshRenderer = go.AddComponent<MeshRenderer>( );
                var speckleMaterial = renderMaterial ?? (RenderMaterial) speckleMesh[ "renderMaterial" ];
                meshRenderer.sharedMaterial = GetMaterial( speckleMaterial );

                //Add mesh collider
                // MeshCollider mc = go.AddComponent<MeshCollider>( );
                // mc.sharedMesh = mesh;
                //mc.convex = true;


                //attach properties on this very mesh
                //means the mesh originated in Rhino or similar
                if ( properties == null ) {
                    var meshprops = typeof( Mesh ).GetProperties( BindingFlags.Instance | BindingFlags.Public ).Select( x => x.Name )
                        .ToList( );
                    properties = speckleMesh.GetMembers( )
                        .Where( x => !meshprops.Contains( x.Key ) )
                        .ToDictionary( x => x.Key, x => x.Value );
                }

                AttachSpeckleProperties( go, properties );
                return go;
            }

        private static IEnumerable<Vector2> GenerateUV( IReadOnlyList<Vector3> verts, float xSize, float ySize )
            {
                var uv = new Vector2[ verts.Count ];
                for ( int i = 0; i < verts.Count; i++ ) {

                    var vert = verts[ i ];
                    uv[ i ] = new Vector2( vert.x / xSize, vert.y / ySize );
                }
                return uv;
            }
#endregion





        private Material GetMaterial( RenderMaterial renderMaterial )
            {
                //todo support more complex materials
                var shader = Shader.Find( "Standard" );
                Material mat = new Material( shader );

                //if a renderMaterial is passed use that, otherwise try get it from the mesh itself

                if ( renderMaterial != null ) {
                    // 1. match material by name, if any
                    var matByName = ContextObjects.FirstOrDefault( x => ( (Material) x.NativeObject ).name == renderMaterial.name );
                    if ( matByName != null ) {
                        return matByName.NativeObject as Material;
                    }

                    // 2. re-create material by setting diffuse color and transparency on standard shaders
                    if ( renderMaterial.opacity < 1 ) {
                        shader = Shader.Find( "Transparent/Diffuse" );
                        mat = new Material( shader );
                    }

                    var c = renderMaterial.diffuse.ToUnityColor( );
                    mat.color = new Color( c.r, c.g, c.b, Convert.ToSingle( renderMaterial.opacity ) );

                    return mat;
                }

                // 3. if not renderMaterial was passed, the default shader will be used 
                return mat;
            }

        private void AttachSpeckleProperties( GameObject go, Dictionary<string, object> properties )
            {
                var sd = go.AddComponent<SpeckleProperties>( );
                sd.Data = properties;
            }


        private void AttachUnityProperties( Base @base, GameObject go )
            {
                var sd = go.GetComponent<SpeckleProperties>( );
                if ( sd == null || sd.Data == null )
                    return;

                foreach ( var key in sd.Data.Keys ) {
                    @base[ key ] = sd.Data[ key ];
                }
            }
    }
}