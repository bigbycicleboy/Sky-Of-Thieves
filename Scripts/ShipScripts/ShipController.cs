using UnityEngine;

public class ShipController : MonoBehaviour
{
    [Header("Flight Settings")]
    public float moveSpeed = 150f;         // forward/backward speed
    public float boostSpeed = 250f;        // boost speed when holding shift
    public float pulseSpeed = 750f;       // speed when pulse drive is active
    public float pitchSpeed = 60f;        // nose up/down speed
    public float yawSpeed = 60f;          // turning left/right speed
    public float rollSpeed = 60f;         // roll speed (A/D keys)
    public float maxPitchAngle = 45f;     // max pitch from mouse
    public float maxYawAngle = 45f;       // max yaw from mouse
    public float smoothness = 5f;         // rotation & movement smoothing
    public float deadZone = 0.1f;         // mouse dead zone
    public Texture2D shipCursor;     
    public GameObject[] particles;

    private float pitchInput;
    private float yawInput;
    private float rollInput;
    private float thrustInput;
    private float smoothedPitch;
    private float smoothedYaw;
    private float smoothedRoll;
    private float finalSpeed;
    private bool pulse = false;

    private Vector3 currentVelocity = Vector3.zero;

    void Start()
    {
        Cursor.SetCursor(shipCursor, Vector2.zero, CursorMode.Auto);
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.Confined;
    }

    void Update()
    {
        HandleMouseInput();
        HandleKeyboardInput();
        HandleThrustInput();
        ApplyRotation();
        ApplyMovement();

        if(thrustInput >= 0.1f)
        {
            foreach(GameObject p in particles)
            {
                var particleSystem = p.GetComponent<ParticleSystem>();
                particleSystem.startLifetime = Mathf.Lerp(particleSystem.startLifetime, 2.5f, 2);
            }
        }
        else
        {
            foreach(GameObject p in particles)
            {
                var particleSystem = p.GetComponent<ParticleSystem>();
                particleSystem.startLifetime = Mathf.Lerp(particleSystem.startLifetime, 0, 2);
            }
        }
    }

    void HandleMouseInput()
    {
        float mouseX = (Input.mousePosition.x - Screen.width / 2f) / (Screen.width / 2f);
        float mouseY = (Input.mousePosition.y - Screen.height / 2f) / (Screen.height / 2f);
        bool boosting = Input.GetKey(KeyCode.LeftShift);
        bool pulseDrive = Input.GetKey(KeyCode.Space);

        if(boosting)
            finalSpeed = boostSpeed;
        else
            finalSpeed = moveSpeed;

        if(pulseDrive)
        {
            finalSpeed = pulseSpeed;
            pulse = true;
        }
        else
        {
            finalSpeed = moveSpeed;
            pulse = false;
        }
            

        if (Mathf.Abs(mouseX) < deadZone) mouseX = 0f;
        if (Mathf.Abs(mouseY) < deadZone) mouseY = 0f;

        mouseX = Mathf.Clamp(mouseX, -1f, 1f);
        mouseY = Mathf.Clamp(mouseY, -1f, 1f);

        pitchInput = -mouseY * maxPitchAngle;
        yawInput = mouseX * maxYawAngle;
    }

    void HandleKeyboardInput()
    {
        if (Input.GetKey(KeyCode.A))
            rollInput = maxYawAngle;
        else if (Input.GetKey(KeyCode.D))
            rollInput = -maxYawAngle;
        else
            rollInput = 0f;
    }

    void HandleThrustInput()
    {
        if (Input.GetKey(KeyCode.W))
            thrustInput = 1f;
        else if (Input.GetKey(KeyCode.S))
            thrustInput = -1f;
        else
            thrustInput = 0f;
    }

    void ApplyRotation()
    {
        smoothedPitch = Mathf.Lerp(smoothedPitch, pitchInput, smoothness * Time.deltaTime);
        smoothedYaw   = Mathf.Lerp(smoothedYaw,   yawInput,   smoothness * Time.deltaTime);
        smoothedRoll  = Mathf.Lerp(smoothedRoll,  rollInput,  smoothness * Time.deltaTime);

        Quaternion targetRotation = Quaternion.Euler(
            smoothedPitch * pitchSpeed * Time.deltaTime,
            smoothedYaw   * yawSpeed   * Time.deltaTime,
            smoothedRoll  * rollSpeed  * Time.deltaTime
        );

        transform.rotation = transform.rotation * targetRotation;
    }

    void ApplyMovement()
    {
        Vector3 targetVelocity = transform.forward * (thrustInput * finalSpeed);

        currentVelocity = Vector3.Lerp(currentVelocity, targetVelocity, smoothness * Time.deltaTime);

        transform.position += currentVelocity * Time.deltaTime;
    }

    void OnTriggerEnter(Collider other)
    {
        if(other.CompareTag("Atmosphere") && pulse)
        {
            pulse = false;
            finalSpeed = moveSpeed;
        }
    }
}