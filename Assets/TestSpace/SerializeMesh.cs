using UnityEngine;

namespace Speckle.ConnectorUnity {

    [ExecuteInEditMode]
    [RequireComponent( typeof( MeshFilter ) )]
    public class SerializeMesh : MonoBehaviour {


        [HideInInspector] [SerializeField] private Vector2[ ] uv;
        [HideInInspector] [SerializeField] private Vector3[ ] vertices;
        [HideInInspector] [SerializeField] private int[ ] triangles;
        [HideInInspector] [SerializeField] private bool serialized = false;



        private void Awake( )
            {
                if ( serialized ) {
                    gameObject.SafeMeshSet( Rebuild( ) );
                }
            }

        private void Start( )
            {
                if ( serialized ) return;

                Serialize( );
            }

        public void Serialize( )
            {

                var mf = GetComponent<MeshFilter>( );
                if ( mf == null ) return;


                var mesh = mf.SafeMeshGet( );

                uv = mesh.uv;
                vertices = mesh.vertices;
                triangles = mesh.triangles;

                serialized = true;
            }

        public Mesh Rebuild( )
            {
                Mesh mesh = new Mesh {
                    vertices = vertices,
                    triangles = triangles,
                    uv = uv
                };

                mesh.RecalculateNormals( );
                mesh.RecalculateBounds( );

                return mesh;
            }
    }
}