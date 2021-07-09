using System;
using UnityEngine;

namespace Speckle.ConnectorUnity.TestSpace {

    [ExecuteAlways]
    [RequireComponent( typeof( MeshFilter ), typeof( MeshRenderer ) )]
    public class MeshGen : MonoBehaviour {


        [SerializeField] private int xSize = 10, ySize = 10;

        [SerializeField] private bool generate;

        private Mesh mesh;
        private Vector3[ ] vertices;


        private void Update( )
            {
                if ( generate ) {
                    Generate( );

                }
            }


        public void Generate( )
            {

                var mf = GetComponent<MeshFilter>( );

                mf.mesh = mesh = new Mesh( );

                mesh.name = "Procedural Grid";


                vertices = new Vector3[ ( xSize + 1 ) * ( ySize + 1 ) ];
                for ( int i = 0, y = 0; y <= ySize; y++ ) {
                    for ( int x = 0; x <= xSize; x++, i++ ) {
                        vertices[ i ] = new Vector3( x, y );
                    }
                }
                mesh.vertices = vertices;

                int[ ] triangles = new int[ xSize * ySize * 6 ];
                for ( int ti = 0, vi = 0, y = 0; y < ySize; y++, vi++ ) {
                    for ( int x = 0; x < xSize; x++, ti += 6, vi++ ) {
                        triangles[ ti ] = vi;
                        triangles[ ti + 3 ] = triangles[ ti + 2 ] = vi + 1;
                        triangles[ ti + 4 ] = triangles[ ti + 1 ] = vi + xSize + 1;
                        triangles[ ti + 5 ] = vi + xSize + 2;
                    }
                }
                mesh.triangles = triangles;
                generate = false;

            }


    }
}