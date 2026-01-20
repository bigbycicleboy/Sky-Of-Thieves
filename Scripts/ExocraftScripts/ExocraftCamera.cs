using UnityEngine;

public class ExocraftCamera : MonoBehaviour
{
    public Transform target;
    public Vector3 offset = new Vector3(0, 5, -10);
    public float smoothSpeed = 0.125f;

    void LateUpdate()
    {
        if (target == null) return;

        Vector3 desiredPosition = target.TransformPoint(offset);
        transform.position = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);

        Quaternion desiredRotation = Quaternion.LookRotation(target.forward, target.up);
        transform.rotation = Quaternion.Slerp(transform.rotation, desiredRotation, smoothSpeed * Time.deltaTime);
    }
}