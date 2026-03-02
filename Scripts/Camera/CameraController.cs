using UnityEngine;

/// <summary>
/// Top-down camera controller.
/// WASD — pan on the XZ plane regardless of camera angle.
/// Q/E  — rotate around the Y axis.
/// </summary>
public class CameraController : MonoBehaviour {

    [Header("Pan")]
    public float panSpeed = 10f;

    [Header("Rotation")]
    public float rotateSpeed = 90f;   // degrees per second

    void Update() {
        HandlePan();
        HandleRotation();
    }

    void HandlePan() {
        // Build a flat (XZ) forward and right from current Y rotation
        float yRot = transform.eulerAngles.y;
        Vector3 forward = new Vector3(Mathf.Sin(yRot * Mathf.Deg2Rad), 0f, Mathf.Cos(yRot * Mathf.Deg2Rad));
        Vector3 right   = new Vector3(forward.z, 0f, -forward.x);

        Vector3 move = Vector3.zero;

        if (Input.GetKey(KeyCode.W)) move += forward;
        if (Input.GetKey(KeyCode.S)) move -= forward;
        if (Input.GetKey(KeyCode.D)) move += right;
        if (Input.GetKey(KeyCode.A)) move -= right;

        transform.position += move.normalized * panSpeed * Time.deltaTime;
    }

    void HandleRotation() {
        if (Input.GetKey(KeyCode.Q))
            transform.Rotate(0f, -rotateSpeed * Time.deltaTime, 0f, Space.World);
        if (Input.GetKey(KeyCode.E))
            transform.Rotate(0f,  rotateSpeed * Time.deltaTime, 0f, Space.World);
    }
}
