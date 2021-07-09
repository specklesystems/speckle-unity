using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Speckle.Core.Api;
using Speckle.Core.Models;
using UnityEngine;

namespace Speckle.ConnectorUnity
{


  [Serializable]
  public class SpeckleData
  {
    public SpeckleData( string key, string value )
      {
        this.key = key;
        this.value = value;
      }

    public string key;
    public string value;
  }


  /// <summary>
  /// This class gets attached to GOs and is used to store Speckle's metadata when sending / receiving
  /// </summary>
  public class SpeckleProperties : MonoBehaviour
  {

    [Tooltip( "Collection of formatted object data from speckle Base" )]
    [SerializeField] private List<string> serializedData;

    // Basically the same thing as SerializeData field just put in a wrapper class 
    [Tooltip( "Collection of Speckle data in a wrapper thing" )]
    [SerializeField] private List<SpeckleData> speckleData;

    private Dictionary<string, object> _data;

    /// <summary>
    /// Access to data stored in object
    /// </summary>
    public IEnumerable<Base> SerializedData
    {
      get
      {
        if ( speckleData == null ) {
          Debug.Log( $"No Data in this {name} to pull" );
          return null;
        }

        return ( from d in speckleData select Operations.Deserialize( d.value ) ).ToList( );
      }
    }

    public Dictionary<string, object> Data
    {
      get => _data;
      set
      {
        Debug.Log( $"Setting valid data? {value != null}" );
        if ( value != null ) {

          _data = value;
          serializedData = new List<string>( );
          speckleData = new List<SpeckleData>( );

          foreach ( var v in _data ) {
            if ( v.Value is Base @base ) {
              var sb = Operations.Serialize( @base );
              // set to primitive collection
              serializedData.Add( sb );
              //set to wrapper collection
              speckleData.Add( new SpeckleData( v.Key, sb ) );
            }
          }
        }
      }
    }


  }
}