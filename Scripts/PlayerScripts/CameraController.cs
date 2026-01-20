using UnityEngine;

public class CameraController : MonoBehaviour
{
    public Transform player;
    public Vector3 offset;

    [Header("Settings")]
    public float sensitivity = 100f;
    public float minYAngle = -90f;
    public float maxYAngle = 90f;
    public bool lockCursor = true;
    public bool followPlayer = true;

    Vector2 rotation = Vector2.zero;

    void Start()
    {
        if (lockCursor)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    void Update() 
    {
		rotation.y += Input.GetAxis ("Mouse X");
		rotation.x += -Input.GetAxis ("Mouse Y");

        rotation.x = Mathf.Clamp (rotation.x, minYAngle / sensitivity, maxYAngle / sensitivity);

		transform.eulerAngles = rotation * sensitivity;

        transform.position = followPlayer ? player.position + offset : transform.position;
        player.eulerAngles = new Vector3(0, transform.eulerAngles.y, 0);
	}
}