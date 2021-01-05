using UnityEngine;

public class PlayerMovement3D : MonoBehaviour
{
  public float speed = 20;
  private Vector3 motion;
  private Rigidbody rb;

  void Start()
  {
    rb = GetComponent<Rigidbody>();
  }

  void Update()
  {
    motion = new Vector3(Input.GetAxisRaw("Horizontal"), 0, Input.GetAxisRaw("Vertical"));
    rb.velocity = motion * speed;
  }
}