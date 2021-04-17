using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Tutorial : MonoBehaviour
{
  private bool _uiMode = false;
  public GameObject UI;
  public GameObject Controller;
  private CanvasGroup _canvas;
  private MonoBehaviour _cameraScript;

  void Start()
  {
    _canvas = UI.GetComponent<CanvasGroup>();
    _canvas.alpha = 0.5f;

    _cameraScript = Controller.GetComponentInChildren<FirstPersonLook>();
  }

  void Update()
  {
    if (Input.GetKeyDown(KeyCode.E))
    {
      _uiMode = !_uiMode;
      _cameraScript.enabled = !_uiMode;
      _canvas.alpha = _uiMode ? 1 : 0.5f;
      Cursor.lockState = _uiMode ? CursorLockMode.None : CursorLockMode.Locked;
    }
  }
}