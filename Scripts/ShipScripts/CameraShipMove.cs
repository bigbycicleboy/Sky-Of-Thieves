using UnityEngine;

public class CameraShipMove : MonoBehaviour
{
    [Header("Target")]
    public Transform ship;

    [Header("Camera Settings")]
    public Vector3 thirdPersonOffset = new Vector3(0f, 2f, -8f);
    public Vector3 firstPersonOffset = new Vector3(0f, 1f, 0.5f);
    public float positionSmooth = 5f;
    public float rotationSmooth = 5f;

    [Header("Mode")]
    public bool isFirstPerson = false;
    public KeyCode toggleKey = KeyCode.V;

    void Update()
    {
        if (Input.GetKeyDown(toggleKey))
        {
            isFirstPerson = !isFirstPerson;
        }
    }

    void LateUpdate()
    {
        if (ship == null) return;

        if (isFirstPerson)
        {
            transform.position = ship.TransformPoint(firstPersonOffset);
            transform.rotation = ship.rotation;
        }
        else
        {
            Vector3 desiredPosition = ship.TransformPoint(thirdPersonOffset);
            transform.position = Vector3.Lerp(transform.position, desiredPosition, positionSmooth * Time.deltaTime);

            Quaternion desiredRotation = Quaternion.LookRotation(ship.forward, ship.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, desiredRotation, rotationSmooth * Time.deltaTime);
        }
    }
}