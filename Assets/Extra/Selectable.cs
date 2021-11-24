using System.Collections;
using System.Collections.Generic;
using UnityEngine;


//Thanks to : https://sharpcoderblog.com/blog/unity-3d-rts-style-unit-selection
public class Selectable : MonoBehaviour
{

  public Bounds GetObjectBounds()
  {
    Bounds totalBounds = new Bounds();

    var renderers = this.gameObject.GetComponents<MeshRenderer>();

    foreach (var r in renderers)
    {
      if (totalBounds.center == Vector3.zero)
      {
        totalBounds = r.bounds;
      }
      else
      {
        totalBounds.Encapsulate(r.bounds);
      }
    }

    return totalBounds;
  }

  void OnEnable()
  {
    //Add this Object to global list
    if (!SelectionManager.selectables.Contains(this))
    {
      SelectionManager.selectables.Add(this);
    }
  }

  void OnDisable()
  {
    //Remove this Object from global list
    if (SelectionManager.selectables.Contains(this))
    {
      SelectionManager.selectables.Remove(this);
    }
  }
}