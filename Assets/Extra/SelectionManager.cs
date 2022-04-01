using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

//Thanks to : https://sharpcoderblog.com/blog/unity-3d-rts-style-unit-selection
[AddComponentMenu("Speckle/Playground/Selection Manager"), DisallowMultipleComponent]
public class SelectionManager : MonoBehaviour
{
  public Texture topLeftBorder;
  public Texture bottomLeftBorder;
  public Texture topRightBorder;
  public Texture bottomRightBorder;

  Texture2D _borderTexture;

  Texture2D borderTexture
  {
    get
    {
      if (_borderTexture == null)
      {
        _borderTexture = new Texture2D(1, 1);
        _borderTexture.SetPixel(0, 0, Color.white);
        _borderTexture.Apply();
      }

      return _borderTexture;
    }
  }

  bool selectionStarted = false;
  Vector3 mousePosition1;

  public static List<Selectable> selectables = new List<Selectable>();
  public static List<Selectable> selectedObjects = new List<Selectable>();

  // Update is called once per frame
  void Update()
  {
    if (IsPointerOverUIObject())
      return;

    // Begin selection
    if (Input.GetMouseButtonDown(0))
    {
      selectionStarted = true;
      mousePosition1 = Input.mousePosition;
    }

    // End selection
    if (Input.GetMouseButtonUp(0))
    {
      selectionStarted = false;
    }

    if (selectionStarted)
    {
      // Detect which Objects are inside selection rectangle
      Camera camera = Camera.main;
      if (camera == null)
        return;

      selectedObjects.Clear();
      Bounds viewportBounds = GetViewportBounds(camera, mousePosition1, Input.mousePosition);
      foreach (var t in selectables)
      {
        if (viewportBounds.Contains(camera.WorldToViewportPoint(t.transform.position)))
        {
          selectedObjects.Add(t);
        }
      }
    }
  }

  void OnGUI()
  {
    if (selectionStarted)
    {
      Rect rect = GetScreenRect(mousePosition1, Input.mousePosition);
      DrawScreenRectBorder(rect, 2, Color.cyan);
    }

    // Draw selection edges
    if (selectedObjects.Count > 0)
    {
      Camera camera = Camera.main;
      for (int i = 0; i < selectedObjects.Count; i++)
      {
        DrawSelectionIndicator(camera, selectedObjects[i].GetObjectBounds());
      }
    }
  }

  public static bool IsPointerOverUIObject()
  {
    if (EventSystem.current.IsPointerOverGameObject())
      return true;

    //check touch
    if (Input.touchCount > 0 && Input.touches[0].phase == TouchPhase.Began)
    {
      if (EventSystem.current.IsPointerOverGameObject(Input.touches[0].fingerId))
        return true;
    }

    return false;
  }

  void DrawScreenRectBorder(Rect rect, float thickness, Color color)
  {
    // Top
    DrawBorderRect(new Rect(rect.xMin, rect.yMin, rect.width, thickness), color);
    // Left
    DrawBorderRect(new Rect(rect.xMin, rect.yMin, thickness, rect.height), color);
    // Right
    DrawBorderRect(new Rect(rect.xMax - thickness, rect.yMin, thickness, rect.height), color);
    // Bottom
    DrawBorderRect(new Rect(rect.xMin, rect.yMax - thickness, rect.width, thickness), color);
  }

  void DrawBorderRect(Rect rect, Color color)
  {
    GUI.color = color;
    GUI.DrawTexture(rect, borderTexture);
    GUI.color = Color.white;
  }

  Rect GetScreenRect(Vector3 screenPosition1, Vector3 screenPosition2)
  {
    // Move origin from bottom left to top left
    screenPosition1.y = Screen.height - screenPosition1.y;
    screenPosition2.y = Screen.height - screenPosition2.y;
    // Calculate corners
    var topLeft = Vector3.Min(screenPosition1, screenPosition2);
    var bottomRight = Vector3.Max(screenPosition1, screenPosition2);
    // Create Rect
    return Rect.MinMaxRect(topLeft.x, topLeft.y, bottomRight.x, bottomRight.y);
  }

  Bounds GetViewportBounds(Camera camera, Vector3 screenPosition1, Vector3 screenPosition2)
  {
    Vector3 v1 = camera.ScreenToViewportPoint(screenPosition1);
    Vector3 v2 = camera.ScreenToViewportPoint(screenPosition2);
    Vector3 min = Vector3.Min(v1, v2);
    Vector3 max = Vector3.Max(v1, v2);
    min.z = camera.nearClipPlane;
    max.z = camera.farClipPlane;

    Bounds bounds = new Bounds();
    bounds.SetMinMax(min, max);
    return bounds;
  }

  void DrawSelectionIndicator(Camera camera, Bounds bounds)
  {
    Vector3 boundPoint1 = bounds.min;
    Vector3 boundPoint2 = bounds.max;
    Vector3 boundPoint3 = new Vector3(boundPoint1.x, boundPoint1.y, boundPoint2.z);
    Vector3 boundPoint4 = new Vector3(boundPoint1.x, boundPoint2.y, boundPoint1.z);
    Vector3 boundPoint5 = new Vector3(boundPoint2.x, boundPoint1.y, boundPoint1.z);
    Vector3 boundPoint6 = new Vector3(boundPoint1.x, boundPoint2.y, boundPoint2.z);
    Vector3 boundPoint7 = new Vector3(boundPoint2.x, boundPoint1.y, boundPoint2.z);
    Vector3 boundPoint8 = new Vector3(boundPoint2.x, boundPoint2.y, boundPoint1.z);

    Vector2[] screenPoints = new Vector2[8];
    screenPoints[0] = camera.WorldToScreenPoint(boundPoint1);
    screenPoints[1] = camera.WorldToScreenPoint(boundPoint2);
    screenPoints[2] = camera.WorldToScreenPoint(boundPoint3);
    screenPoints[3] = camera.WorldToScreenPoint(boundPoint4);
    screenPoints[4] = camera.WorldToScreenPoint(boundPoint5);
    screenPoints[5] = camera.WorldToScreenPoint(boundPoint6);
    screenPoints[6] = camera.WorldToScreenPoint(boundPoint7);
    screenPoints[7] = camera.WorldToScreenPoint(boundPoint8);

    Vector2 topLeftPosition = Vector2.zero;
    Vector2 topRightPosition = Vector2.zero;
    Vector2 bottomLeftPosition = Vector2.zero;
    Vector2 bottomRightPosition = Vector2.zero;

    for (int a = 0; a < screenPoints.Length; a++)
    {
      //Top Left
      if (topLeftPosition.x == 0 || topLeftPosition.x > screenPoints[a].x)
      {
        topLeftPosition.x = screenPoints[a].x;
      }

      if (topLeftPosition.y == 0 || topLeftPosition.y > Screen.height - screenPoints[a].y)
      {
        topLeftPosition.y = Screen.height - screenPoints[a].y;
      }

      //Top Right
      if (topRightPosition.x == 0 || topRightPosition.x < screenPoints[a].x)
      {
        topRightPosition.x = screenPoints[a].x;
      }

      if (topRightPosition.y == 0 || topRightPosition.y > Screen.height - screenPoints[a].y)
      {
        topRightPosition.y = Screen.height - screenPoints[a].y;
      }

      //Bottom Left
      if (bottomLeftPosition.x == 0 || bottomLeftPosition.x > screenPoints[a].x)
      {
        bottomLeftPosition.x = screenPoints[a].x;
      }

      if (bottomLeftPosition.y == 0 || bottomLeftPosition.y < Screen.height - screenPoints[a].y)
      {
        bottomLeftPosition.y = Screen.height - screenPoints[a].y;
      }

      //Bottom Right
      if (bottomRightPosition.x == 0 || bottomRightPosition.x < screenPoints[a].x)
      {
        bottomRightPosition.x = screenPoints[a].x;
      }

      if (bottomRightPosition.y == 0 || bottomRightPosition.y < Screen.height - screenPoints[a].y)
      {
        bottomRightPosition.y = Screen.height - screenPoints[a].y;
      }
    }

    GUI.DrawTexture(new Rect(topLeftPosition.x - 16, topLeftPosition.y - 16, 16, 16), topLeftBorder);
    GUI.DrawTexture(new Rect(topRightPosition.x, topRightPosition.y - 16, 16, 16), topRightBorder);
    GUI.DrawTexture(new Rect(bottomLeftPosition.x - 16, bottomLeftPosition.y, 16, 16), bottomLeftBorder);
    GUI.DrawTexture(new Rect(bottomRightPosition.x, bottomRightPosition.y, 16, 16), bottomRightBorder);
  }
}