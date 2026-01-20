using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlanetExocraft : MonoBehaviour
{
    [Header("References")]
    public Transform planetTransform;
    private Rigidbody rb;
    public Transform[] wheels;

    [Header("Movement")]
    public float acceleration = 80f;
    public float maxSpeed = 35f;
    public float steeringStrength = 8f;
    public float brakingForce = 60f;

    [Header("Gravity")]
    public float gravityStrength = 30f;
    public float alignToSurfaceSpeed = 10f;

    [Header("Grounding")]
    public float groundRayLength = 2.5f;
    public float stickToGroundForce = 50f;
    public LayerMask groundLayer;

    bool isGrounded;
    Vector3 groundNormal;
    Vector3 groundPoint;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.centerOfMass = new Vector3(0, -0.6f, 0);
    }

    void FixedUpdate()
    {
        ApplyPlanetGravity();
        CheckGrounded();
        AlignToSurface();
        CancelSlopeSlide();
        HandleMovement();
    }

    void ApplyPlanetGravity()
    {
        Vector3 gravityDir = (planetTransform.position - transform.position).normalized;
        rb.AddForce(gravityDir * gravityStrength, ForceMode.Acceleration);
    }

    
    void CheckGrounded()
    {
        isGrounded = false;

        if (Physics.Raycast(transform.position, -transform.up, out RaycastHit hit, groundRayLength, groundLayer))
        {
            isGrounded = true;
            gravityStrength = 20f;
            groundNormal = hit.normal;
            groundPoint = hit.point;
        }
        else
        {
            gravityStrength = 150f;
        }
    }

    void AlignToSurface()
    {
        if (!isGrounded) return;

        Quaternion targetRotation =
            Quaternion.FromToRotation(transform.up, groundNormal) * rb.rotation;

        Quaternion newRotation = Quaternion.Slerp(
            rb.rotation,
            targetRotation,
            alignToSurfaceSpeed * Time.fixedDeltaTime
        );

        Vector3 pivotOffset = rb.position - groundPoint;
        Vector3 rotatedOffset = newRotation * Quaternion.Inverse(rb.rotation) * pivotOffset;

        rb.MovePosition(groundPoint + rotatedOffset);
        rb.MoveRotation(newRotation);
    }

    void CancelSlopeSlide()
    {
        if (!isGrounded) return;

        rb.linearVelocity = Vector3.Project(rb.linearVelocity, groundNormal) + Vector3.ProjectOnPlane(rb.linearVelocity, groundNormal) * 0.995f;
        rb.angularVelocity *= 0.8f;
    }

    void HandleMovement()
    {
        float forward = Input.GetAxis("Vertical");
        float steer = Input.GetAxis("Horizontal");

        Vector3 surfaceForward = Vector3.ProjectOnPlane(transform.forward, groundNormal).normalized;
    
        // Acceleration
        if (isGrounded)
        {
            if (rb.linearVelocity.magnitude < maxSpeed)
            {
                rb.AddForce(surfaceForward * forward * acceleration, ForceMode.Acceleration);
            }

            // Braking
            if (Mathf.Abs(forward) < 0.1f)
            {
                rb.AddForce(-rb.linearVelocity * brakingForce * Time.fixedDeltaTime, ForceMode.Acceleration);
            }

            // Steering
            float steerAmount = steer * steeringStrength * rb.linearVelocity.magnitude * Time.fixedDeltaTime;
            rb.AddTorque(groundNormal * steerAmount, ForceMode.VelocityChange);
        }

        if(rb.linearVelocity.magnitude > 0.1f)
        {
            foreach(Transform wheel in wheels)
            {
                wheel.Rotate(Vector3.right, rb.linearVelocity.magnitude * 20f * Time.fixedDeltaTime);
            }
        }
        else if(rb.linearVelocity.magnitude < 0.1f)
        {
            foreach(Transform wheel in wheels)
            {
                wheel.Rotate(Vector3.left, rb.linearVelocity.magnitude * 20f * Time.fixedDeltaTime);
            }
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawLine(transform.position, transform.position - transform.up * groundRayLength);
    }
}