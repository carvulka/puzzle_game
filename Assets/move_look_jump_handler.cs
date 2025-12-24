using UnityEngine;
using UnityEngine.InputSystem;

public class move_look_jump_handler : MonoBehaviour
{
    //configuration
    [SerializeField] float sensitivity = 8f;
    [SerializeField] float speed = 800f;
    [SerializeField] float airborne_speed = 196f;
    [SerializeField] float max_horizontal_velocity_magnitude = 4f;
    [SerializeField] float counter_strength = 0.125f;
    [SerializeField] float jump_strength = 336f;
    [SerializeField] float jump_cooldown = 0.25f;
    [SerializeField] float max_slope = 45f;
    InputAction move_action;
    InputAction look_action;
    InputAction jump_action;
    new Camera camera;
    Rigidbody body;
    //state
    Vector2 move_input;
    Vector2 look_input;
    bool jump_is_pressed;
    //bool jump_was_pressed;
    //bool jump_was_released;
    bool is_grounded = false;
    bool is_ready_to_jump = true;
    Vector3 ground_normal_vector = Vector3.up;
    bool is_cancelling_grounded = true;
    //stored to prevent accumulation of quaternion float errors
    float pitch;

    void Awake()
    {
        camera = Camera.main;
        body = GetComponent<Rigidbody>();
    }

    void Start()
    {
        move_action = InputSystem.actions.FindAction("move");
        look_action = InputSystem.actions.FindAction("look");
        jump_action = InputSystem.actions.FindAction("jump");

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        body.freezeRotation = true;
    }

    void FixedUpdate()
    {
        move();

        if (is_grounded && !jump_is_pressed)
        {
            counter_move(counter_velocity());
        }

        if (is_grounded && jump_is_pressed && is_ready_to_jump)
        {
            jump();
        }

        limit_horizontal_speed();
    }

    void Update()
    {
        move_input = move_action.ReadValue<Vector2>();
        look_input = look_action.ReadValue<Vector2>();
        jump_is_pressed = jump_action.IsPressed();

        camera.transform.position = transform.position;
        look();
    }

    void look()
    {
        float pitch_delta = look_input.y * Time.deltaTime * sensitivity;
        float yaw_delta = look_input.x * Time.deltaTime * sensitivity;

        float yaw = camera.transform.localEulerAngles.y + yaw_delta;
        pitch = pitch - pitch_delta;
        pitch = Mathf.Clamp(pitch, -90f, 90f);

        camera.transform.localRotation = Quaternion.Euler(pitch, yaw, 0);
        transform.localRotation = Quaternion.Euler(0, yaw, 0);
    }

    void move()
    {
        float z = move_input.y * Time.fixedDeltaTime;
        float x = move_input.x * Time.fixedDeltaTime;

        Vector3 force = transform.forward * z + transform.right * x;
        if (!is_grounded)
        {
            body.AddForce(force * airborne_speed);
        }
        else
        {
            body.AddForce(force * speed);
        }
    }

    void limit_horizontal_speed()
    {
        Vector3 horizontal_velocity = new Vector3(body.linearVelocity.x, 0f, body.linearVelocity.z);
        if (horizontal_velocity.magnitude > max_horizontal_velocity_magnitude)
        {
            Vector3 limited_velocity = horizontal_velocity.normalized * max_horizontal_velocity_magnitude;
            body.linearVelocity = new Vector3(limited_velocity.x, body.linearVelocity.y, limited_velocity.z);
        }
    }

    void counter_move(Vector2 counter_velocity)
    {
        float z_delta = move_input.y * Time.fixedDeltaTime;
        float x_delta = move_input.x * Time.fixedDeltaTime;

        if (Mathf.Abs(counter_velocity.x) > 0.5f && Mathf.Abs(x_delta * speed) < 0.05f
            || counter_velocity.x < -0.5f && x_delta * speed > 0
            || counter_velocity.x > 0.5f && x_delta * speed < 0)
        {
            body.AddForce(transform.right * -counter_velocity.x * counter_strength * speed * Time.deltaTime);
        }
        if (Mathf.Abs(counter_velocity.y) > 0.5f && Mathf.Abs(z_delta * speed) < 0.05f
            || counter_velocity.y < -0.5f && z_delta * speed > 0
            || counter_velocity.y > 0.5f && z_delta * speed < 0)
        {
            body.AddForce(transform.forward * -counter_velocity.y * counter_strength * speed * Time.deltaTime);
        }
    }

    Vector2 counter_velocity()
    {
        float yaw = transform.eulerAngles.y;
        float move_direction = Mathf.Atan2(body.linearVelocity.x, body.linearVelocity.z) * Mathf.Rad2Deg;

        float u = Mathf.DeltaAngle(yaw, move_direction);
        float v = 90f - u;

        return new Vector2(Mathf.Cos(v * Mathf.Deg2Rad), Mathf.Cos(u * Mathf.Deg2Rad)) * body.linearVelocity.magnitude;
    }

    void jump()
    {
        is_ready_to_jump = false;

        Vector3 vertical_component = Vector3.up * jump_strength * 0.75f;
        Vector3 ground_normal_component = ground_normal_vector * jump_strength * 0.25f;
        body.AddForce(vertical_component + ground_normal_component);

        if (body.linearVelocity.y < 0.5f)
        {
            body.linearVelocity = new Vector3(body.linearVelocity.x, 0f, body.linearVelocity.z);
        }
        else if (body.linearVelocity.y > 0f)
        {
            body.linearVelocity = new Vector3(body.linearVelocity.x, body.linearVelocity.y / 2f, body.linearVelocity.z);
        }

        Invoke(nameof(reset_jump), jump_cooldown);
    }

    void reset_jump()
    {
        is_ready_to_jump = true;
    }



    void OnCollisionStay(Collision other)
    {
        for (int i = 0; i < other.contactCount; i = i + 1)
        {
            ground_normal_vector = other.contacts[i].normal;
            if (Vector3.Angle(Vector3.up, ground_normal_vector) < max_slope)
            {
                is_grounded = true;
                is_cancelling_grounded = false;
                CancelInvoke(nameof(reset_grounded));
            }
        }

        if (!is_cancelling_grounded)
        {
            is_cancelling_grounded = true;
            Invoke(nameof(reset_grounded), Time.deltaTime * 3f);
        }
    }

    void reset_grounded()
    {
        is_grounded = false;
    }
}
