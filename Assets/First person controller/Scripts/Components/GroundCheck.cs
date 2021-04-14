using UnityEngine;

public class GroundCheck : MonoBehaviour
{
    public float maxGroundDistance = .3f;
    public bool isGrounded = true;
    public event System.Action Grounded;


    void Reset() => transform.localPosition = Vector3.up * .01f;

    void LateUpdate()
    {
        bool isGroundedNow = Physics.Raycast(transform.position, Vector3.down, maxGroundDistance);
        if (isGroundedNow && !isGrounded)
            Grounded?.Invoke();
        isGrounded = isGroundedNow;
    }

    void OnDrawGizmosSelected()
    {
        RaycastHit hit;
        if (Physics.Raycast(transform.position, Vector3.down, out hit, maxGroundDistance))
            Debug.DrawLine(transform.position, hit.point, Color.white);
        else
            Debug.DrawLine(transform.position, transform.position + Vector3.down * maxGroundDistance, Color.red);
    }


    public static GroundCheck Create(Transform parent)
    {
        GameObject newGroundCheck = new GameObject("Ground check");
#if UNITY_EDITOR
        UnityEditor.Undo.RegisterCreatedObjectUndo(newGroundCheck, "Created ground check");
#endif
        newGroundCheck.transform.parent = parent;
        newGroundCheck.transform.localPosition = Vector3.up * .01f;
        return newGroundCheck.AddComponent<GroundCheck>();
    }
}
