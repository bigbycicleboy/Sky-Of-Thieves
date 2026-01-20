using UnityEngine;

public class PlayerController : GravityObject
{
    [Header("Movement")]
    public float walkSpeed = 8f;
    public float runSpeed = 14f;
    public float jumpForce = 20f;
    public float groundSmoothTime = 0.1f;
    public float airSmoothTime = 0.5f;
    public float stickToGroundForce = 8f;

    [Header("Jetpack")]
    public float jetpackForce = 10f;
    public float jetpackDuration = 2f;
    public float jetpackRefuelTime = 2f;
    public float jetpackRefuelDelay = 2f;

    [Header("Mouse")]
    public float mouseSensitivity = 10f;
    public float rotationSmoothTime = 0.1f;
    public Vector2 pitchMinMax = new Vector2(-40, 85);

    [Header("Other")]
    public float mass = 70f;
    public LayerMask walkableMask;
    public Transform feet;
    public Camera cam;

    Rigidbody rb;
    Animator animator;

    Vector3 smoothVelocity;
    Vector3 velocitySmoothRef;

    bool usingJetpack;
    float jetpackFuel = 1f;
    float lastJetpackUseTime;

    CelestialBody referenceBody;
    Vector3 gravityUp;

	float pitch;

	Quaternion facingRotation = Quaternion.identity;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.mass = mass;
		facingRotation = transform.rotation;

		Cursor.lockState = CursorLockMode.Locked;
		Cursor.visible = false;
		
        animator = GetComponentInChildren<Animator>();
    }

    void Update()
    {
        HandleMouseLook();
        HandleInput();
    }

    // ----------------------------
    // INPUT & CAMERA
    // ----------------------------
    void HandleMouseLook()
	{
		float mouseX = Input.GetAxisRaw("Mouse X") * mouseSensitivity * 5;
		float mouseY = Input.GetAxisRaw("Mouse Y") * mouseSensitivity;

		// Rotate facing direction around current up
		facingRotation =
			Quaternion.AngleAxis(mouseX, gravityUp == Vector3.zero ? Vector3.up : gravityUp)
			* facingRotation;

		pitch -= mouseY;
		pitch = Mathf.Clamp(pitch, pitchMinMax.x, pitchMinMax.y);

		cam.transform.localEulerAngles = new Vector3(pitch, 0f, 0f);
	}

    Vector3 inputDir;
    bool jumpPressed;
    bool runHeld;

    void HandleInput()
    {
        inputDir = new Vector3(
            Input.GetAxisRaw("Horizontal"),
            0f,
            Input.GetAxisRaw("Vertical")
        ).normalized;

        runHeld = Input.GetKey(KeyCode.LeftShift);
        jumpPressed = Input.GetKeyDown(KeyCode.Space);
    }

    // ----------------------------
    // PHYSICS
    // ----------------------------
    void FixedUpdate()
    {
        ApplyGravity();
        AlignToPlanet();
        HandleMovement();
    }

    void ApplyGravity()
    {
        CelestialBody[] bodies = NBodySimulation.Bodies;

        Vector3 strongestGravity = Vector3.zero;
        float nearestSurfaceDist = float.MaxValue;

        foreach (CelestialBody body in bodies)
        {
            Vector3 toBody = body.Position - rb.position;
            float sqrDst = toBody.sqrMagnitude;

            Vector3 accel =
                toBody.normalized *
                Universe.gravitationalConstant *
                body.mass / sqrDst;

            rb.AddForce(accel, ForceMode.Acceleration);

            float surfaceDst = Mathf.Sqrt(sqrDst) - body.radius;
            if (surfaceDst < nearestSurfaceDist)
            {
                nearestSurfaceDist = surfaceDst;
                strongestGravity = accel;
                referenceBody = body;
            }
        }

        gravityUp = -strongestGravity.normalized;
    }

    void AlignToPlanet()
	{
		if (gravityUp == Vector3.zero)
			return;

		// Tilt the facing rotation so its up matches gravity up
		Quaternion gravityCorrection =
			Quaternion.FromToRotation(facingRotation * Vector3.up, gravityUp);

		Quaternion targetRotation = gravityCorrection * facingRotation;

		rb.MoveRotation(
			Quaternion.Slerp(
				rb.rotation,
				targetRotation,
				15f * Time.fixedDeltaTime
			)
		);

		// Keep facingRotation in sync so it doesn't drift
		facingRotation = rb.rotation;
	}

    void HandleMovement()
    {
        bool grounded = IsGrounded();

        Vector3 moveDir =
            Vector3.ProjectOnPlane(transform.forward, gravityUp) * inputDir.z +
            Vector3.ProjectOnPlane(transform.right, gravityUp) * inputDir.x;

        float speed = runHeld ? runSpeed : walkSpeed;
        Vector3 targetVelocity = moveDir * speed;

        smoothVelocity = Vector3.SmoothDamp(
            smoothVelocity,
            targetVelocity,
            ref velocitySmoothRef,
            grounded ? groundSmoothTime : airSmoothTime
        );

        if (grounded)
        {
            if (jumpPressed)
            {
                rb.AddForce(transform.up * jumpForce, ForceMode.VelocityChange);
            }
            else
            {
                rb.AddForce(-transform.up * stickToGroundForce, ForceMode.Acceleration);
            }
        }
        else if (jumpPressed && jetpackFuel > 0f)
        {
            usingJetpack = true;
        }

		if (grounded)
		{
			Vector3 lateralVelocity =
				Vector3.ProjectOnPlane(rb.linearVelocity, gravityUp);

			Vector3 desiredVelocity =
				Vector3.ProjectOnPlane(smoothVelocity, gravityUp);

			Vector3 velocityChange = desiredVelocity - lateralVelocity;

			rb.AddForce(velocityChange, ForceMode.VelocityChange);
		}

		if (grounded && inputDir.sqrMagnitude < 0.01f)
		{
			Vector3 lateralVelocity =
				Vector3.ProjectOnPlane(rb.linearVelocity, gravityUp);

			rb.AddForce(-lateralVelocity, ForceMode.VelocityChange);
		}

        if (usingJetpack && Input.GetKey(KeyCode.Space) && jetpackFuel > 0f)
        {
            lastJetpackUseTime = Time.time;
            jetpackFuel -= Time.fixedDeltaTime / jetpackDuration;
            rb.AddForce(transform.up * jetpackForce, ForceMode.Acceleration);
        }
        else
        {
            usingJetpack = false;
        }

        if (Time.time - lastJetpackUseTime > jetpackRefuelDelay)
        {
            jetpackFuel = Mathf.Clamp01(
                jetpackFuel + Time.fixedDeltaTime / jetpackRefuelTime
            );
        }

        animator.SetBool("Grounded", grounded);
        animator.SetFloat("Speed", smoothVelocity.magnitude / runSpeed);
    }

    // ----------------------------
    // GROUND CHECK
    // ----------------------------
    bool IsGrounded()
    {
        if (!referenceBody) return false;

        Vector3 offsetToFeet = feet.position - transform.position;
        Vector3 rayOrigin = rb.position + offsetToFeet + transform.up * 0.2f;

        return Physics.SphereCast(
            rayOrigin,
            0.3f,
            -transform.up,
            out _,
            0.4f,
            walkableMask
        );
    }
}