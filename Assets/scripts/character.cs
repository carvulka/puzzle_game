using UnityEngine;
using UnityEngine.InputSystem;

public class CHARACTER : MonoBehaviour
{
	//constants
	const int default_layer = 0;
	const int outline_layer = 6;
	const int preview_layer = 7;

	[Header("configuration")]
	[SerializeField] float sensitivity = 8f;
	[SerializeField] Vector3 camera_offset = new Vector3(0f, 0.25f, 0f);
	[SerializeField] float reach = 4f;
    [SerializeField] float speed = 49152f;
    [SerializeField] float airborne_speed = 3136f;
    [SerializeField] float max_horizontal_velocity_magnitude = 4f;
    [SerializeField] float counter_strength = 0.125f;
    [SerializeField] float max_slope = 45f;
	[SerializeField] float grounded_timeout = 0.125f;
	[SerializeField] Material preview_material;

	[Header("components/children")]
	[SerializeField] Rigidbody body;
	[SerializeField] ConfigurableJoint drag_joint;
	[SerializeField] LineRenderer drag_line;
	[SerializeField] AudioSource score_sound_source;

	InputAction move_action;
    InputAction look_action;
	InputAction drag_action;
	InputAction collect_action;
	InputAction place_action;
	InputAction inventory_action;

	//state
	Vector2 move_input = Vector2.zero;
    Vector2 look_input = Vector2.zero;
	bool drag_is_pressed = false;
    bool drag_was_pressed = false;
    bool drag_was_released = false;
	bool collect_was_pressed = false;
	bool place_was_pressed = false;
	bool inventory_was_pressed = false;
    float pitch = 0f;
	float grounded_timer = 0f;
	bool is_grounded = true;
	float drag_depth = 0f;
	GameObject outlined_object = null;
	GameObject preview_object = null;
	GameObject preview_prefab = null;



    void Awake()
    {
		this.move_action = InputSystem.actions.FindAction("move");
        this.look_action = InputSystem.actions.FindAction("look");
		this.drag_action = InputSystem.actions.FindAction("drag");
		this.collect_action = InputSystem.actions.FindAction("collect");
		this.place_action = InputSystem.actions.FindAction("place");
        this.inventory_action = InputSystem.actions.FindAction("inventory");

		this.body.freezeRotation = true;
		this.drag_joint.connectedBody = null;
		this.drag_line.enabled = false;
    }

	void Start()
    {
		HOTBAR.instance.gameObject.SetActive(true);
		Cursor.lockState = CursorLockMode.Locked;
		INVENTORY.instance.gameObject.SetActive(false);
    }

	void Update()
	{
		this.set_input();

        if (this.inventory_was_pressed)
        {
			this.toggle_inventory();
        }
        if (INVENTORY.instance.gameObject.activeSelf)
        {
            this.reset_input();
        }

        this.look();

		Ray ray = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
		int mask = ~(1 << preview_layer);
		if (Physics.Raycast(ray, out RaycastHit hit, this.reach, mask))
		{
			(ITEM item, COLLECTIBLE.UNIQUE unique) = HOTBAR.instance.selected_slot().try_get();
			if (item != null)
			{
				if (hit.transform.TryGetComponent(out COLLECTIBLE collectible))
				{
					this.set_outline(hit);
					this.set_preview(hit, item.prefab);
					this.handle_collect_was_pressed(hit, collectible);
					this.handle_place_was_pressed(hit, item, unique);
				}
				else if (hit.transform.TryGetComponent(out TARGETABLE targetable) && targetable.id == unique.targetable_id)
				{
					this.reset_preview();
					this.set_outline(hit);
					this.handle_special_place_was_pressed(hit, item, unique);
				}
				else
				{
					this.reset_outline();
					this.set_preview(hit, item.prefab);
					this.handle_place_was_pressed(hit, item, unique);
				}
			}
			else
            {
				this.reset_preview();

				if (hit.transform.CompareTag("draggable"))
				{
					this.reset_outline();
					this.handle_drag_was_pressed(hit);
				}
				else if (hit.transform.TryGetComponent(out COLLECTIBLE collectible))
				{
					this.set_outline(hit);
					this.handle_drag_was_pressed(hit);
					this.handle_collect_was_pressed(hit, collectible);
				}
				else if (hit.transform.TryGetComponent(out TARGETABLE _))
				{
					this.reset_outline();
					this.handle_drag_was_pressed(hit);
				}
				else
				{
					this.reset_outline();
				}
            }
		}
		else
		{
			this.reset_outline();
			this.reset_preview();
		}

		handle_drag_is_pressed();
		handle_drag_was_released();
	}

	void handle_drag_is_pressed()
	{
		if (this.drag_is_pressed && this.drag_joint.connectedBody != null)
		{
			this.drag();
		}
	}

	void handle_drag_was_released()
	{
		if (this.drag_was_released && this.drag_joint.connectedBody != null)
		{
			this.detach();
		}
	}

	void handle_drag_was_pressed(RaycastHit hit)
	{
		if (this.drag_was_pressed)
		{
			this.attach(hit);
		}
	}

	void handle_collect_was_pressed(RaycastHit hit, COLLECTIBLE collectible)
	{
		if (this.collect_was_pressed)
		{
			if (HOTBAR.instance.GetComponent<HOTBAR>().try_merge(collectible.item, collectible.unique)
				|| INVENTORY.instance.GetComponent<INVENTORY>().try_merge(collectible.item, collectible.unique)
				|| HOTBAR.instance.GetComponent<HOTBAR>().try_allot(collectible.item, collectible.unique)
				|| INVENTORY.instance.GetComponent<INVENTORY>().try_allot(collectible.item, collectible.unique))
			{
				Destroy(hit.transform.gameObject);
				if (this.drag_is_pressed && this.drag_joint.connectedBody == hit.rigidbody)
				{
					this.detach();
				}
			}
		}
	}

	void handle_place_was_pressed(RaycastHit hit, ITEM item, COLLECTIBLE.UNIQUE unique)
	{
		if (this.place_was_pressed)
		{
			_ = HOTBAR.instance.selected_slot().try_remove();
			GameObject new_object = Instantiate(item.prefab, hit.point, chud(hit));
			COLLECTIBLE collectible = new_object.GetComponent<COLLECTIBLE>();
			collectible.item = item;
			collectible.unique = unique;
		}
	}

	void handle_special_place_was_pressed(RaycastHit hit, ITEM item, COLLECTIBLE.UNIQUE unique)
	{
		if (this.place_was_pressed)
		{
			_ = HOTBAR.instance.selected_slot().try_remove();
			//attach or destroy it
			HOTBAR.instance.increment_score();
			this.score_sound_source.Play();
		}
	}

	Quaternion chud(RaycastHit hit)
	{
		Vector3 camera_direction = Camera.main.transform.position - hit.point;
		camera_direction.y = 0;
		Vector3 projection = Vector3.ProjectOnPlane(camera_direction, hit.normal);
		return projection != Vector3.zero ?
				Quaternion.LookRotation(projection, hit.normal) : Quaternion.FromToRotation(Vector3.up, hit.normal);
	}

	void set_input()
	{
		this.move_input = this.move_action.ReadValue<Vector2>();
        this.look_input = this.look_action.ReadValue<Vector2>();
		this.drag_is_pressed = this.drag_action.IsPressed();
		this.drag_was_pressed = this.drag_action.WasPressedThisFrame();
		this.drag_was_released = this.drag_action.WasReleasedThisFrame();
		this.collect_was_pressed = this.collect_action.WasPressedThisFrame();
		this.place_was_pressed = this.place_action.WasPressedThisFrame();
        this.inventory_was_pressed = this.inventory_action.WasPressedThisFrame();
	}

	void reset_input()
    {
        this.move_input = Vector2.zero;
        this.look_input = Vector2.zero;
		this.drag_is_pressed = false;
		this.drag_was_pressed = false;
		this.drag_was_released = false;
		this.collect_was_pressed = false;
		this.place_was_pressed = false;
        this.inventory_was_pressed = false;
    }

	void toggle_inventory()
	{
		Cursor.lockState = !INVENTORY.instance.gameObject.activeSelf ? CursorLockMode.None : CursorLockMode.Locked;
		INVENTORY.instance.gameObject.SetActive(!INVENTORY.instance.gameObject.activeSelf);
	}

	void set_preview(RaycastHit hit, GameObject prefab)
    {
		Quaternion rotation = chud(hit);
        if (this.preview_object == null || this.preview_prefab != prefab)
        {
            this.reset_preview();
			this.preview_object = Instantiate(prefab, hit.point, rotation);
			this.preview_prefab = prefab;

            foreach (var collider in this.preview_object.GetComponentsInChildren<Collider>())
			{
				collider.isTrigger = true;
			}
            foreach (var body in this.preview_object.GetComponentsInChildren<Rigidbody>())
			{
				body.isKinematic = true;
			}
            foreach (var script in this.preview_object.GetComponentsInChildren<MonoBehaviour>()) 
            {
                if (script != this)
				{
					Destroy(script);
				};
            }

            foreach (var rend in this.preview_object.GetComponentsInChildren<Renderer>())
            {
				//does not work because we loooooooove c# and side effects
				/*
    			for (int n = 0; n < rend.materials.Length; n = n + 1)
    			{
        			rend.materials[n] = preview_material;
   	 			}
				*/
				Material[] preview_materials = new Material[rend.sharedMaterials.Length];
    			for (int n = 0; n < preview_materials.Length; n = n + 1)
    			{
    			    preview_materials[n] = preview_material;
    			}
				rend.materials = preview_materials;
            	rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
			this.set_layer_recursively(this.preview_object, preview_layer);
        }
		this.preview_object.transform.position = hit.point;
		this.preview_object.transform.rotation = rotation;
    }

    void reset_preview()
    {
        if (this.preview_object == null) { return; }
        Destroy(this.preview_object);
        this.preview_object = null;
		this.preview_prefab = null;
    }

	void set_outline(RaycastHit hit)
	{
		GameObject game_object = hit.transform.root.gameObject;
		if (game_object != this.outlined_object)
		{
			this.reset_outline();
			this.set_layer_recursively(game_object, outline_layer);
			this.outlined_object = game_object;
		}
	}

	void set_layer_recursively(GameObject game_object, int layer)
	{
    	game_object.layer = layer;
    	foreach (Transform child in game_object.transform)
    	{
        	this.set_layer_recursively(child.gameObject, layer);
    	}
	}

	void reset_outline()
	{
		if (this.outlined_object == null) { return; }
		this.set_layer_recursively(this.outlined_object, default_layer);
        this.outlined_object = null;
	}

	void attach(RaycastHit hit)
	{
		this.drag_depth = Camera.main.transform.InverseTransformPoint(hit.point).z;
		this.drag_joint.transform.position = hit.point;
		this.drag_joint.anchor = Vector3.zero;
		this.drag_joint.connectedBody = hit.rigidbody;
		this.drag_joint.connectedAnchor = hit.transform.InverseTransformPoint(hit.point);
		this.drag_line.enabled = true;
	}

	void drag()
	{
		this.drag_joint.transform.position = Camera.main.ViewportToWorldPoint(new Vector3(0.5f, 0.5f, this.drag_depth));
		this.drag_line.SetPosition(0, this.drag_joint.transform.TransformPoint(this.drag_joint.anchor));
		this.drag_line.SetPosition(1, this.drag_joint.connectedBody.transform.TransformPoint(this.drag_joint.connectedAnchor));
	}

	void detach()
	{
		this.drag_joint.connectedBody = null;
		this.drag_line.enabled = false;
	}

	void look()
    {
        float pitch_delta = this.look_input.y * Time.deltaTime * this.sensitivity;
        float yaw_delta = this.look_input.x * Time.deltaTime * this.sensitivity;

        float yaw = Camera.main.transform.localEulerAngles.y + yaw_delta;
        this.pitch = this.pitch - pitch_delta;
        this.pitch = Mathf.Clamp(this.pitch, -90f, 90f);

		Camera.main.transform.position = this.transform.position + this.camera_offset;
        Camera.main.transform.localRotation = Quaternion.Euler(this.pitch, yaw, 0);
        this.transform.localRotation = Quaternion.Euler(0, yaw, 0);
    }

	void FixedUpdate()
    {
		if (this.grounded_timer > 0f)
    	{
        	this.grounded_timer -= Time.fixedDeltaTime;
    	}
    	else
    	{
        	this.is_grounded = false;
    	}

        this.move();
        if (this.is_grounded)
        {
            this.counter_move();
        }
        this.limit_horizontal_velocity();
    }

    void move()
    {
        float z = this.move_input.y;
        float x = this.move_input.x;
        this.body.AddForce((this.transform.forward * z + this.transform.right * x) * (this.is_grounded ? this.speed : this.airborne_speed));
    }

    void counter_move()
    {
		float yaw = this.transform.eulerAngles.y;
        float velocity_yaw = Mathf.Atan2(this.body.linearVelocity.x, this.body.linearVelocity.z) * Mathf.Rad2Deg;
        float u = Mathf.DeltaAngle(yaw, velocity_yaw);
        float v = 90f - u;
        Vector2 counter_velocity = new Vector2(Mathf.Cos(v * Mathf.Deg2Rad), Mathf.Cos(u * Mathf.Deg2Rad)) * this.body.linearVelocity.magnitude;

        float z = this.move_input.y;
        float x = this.move_input.x;

        if (Mathf.Abs(counter_velocity.x) > 0.5f && Mathf.Abs(x * this.speed) < 0.05f
            || counter_velocity.x < -0.5f && x * this.speed > 0
            || counter_velocity.x > 0.5f && x * this.speed < 0)
        {
            this.body.AddForce(this.transform.right * -counter_velocity.x * this.counter_strength * this.speed);
        }
        if (Mathf.Abs(counter_velocity.y) > 0.5f && Mathf.Abs(z * this.speed) < 0.05f
            || counter_velocity.y < -0.5f && z * this.speed > 0
            || counter_velocity.y > 0.5f && z * this.speed < 0)
        {
            this.body.AddForce(this.transform.forward * -counter_velocity.y * this.counter_strength * this.speed);
        }
    }

	void limit_horizontal_velocity()
    {
        Vector3 horizontal_velocity = new Vector3(this.body.linearVelocity.x, 0f, this.body.linearVelocity.z);
        if (horizontal_velocity.magnitude > this.max_horizontal_velocity_magnitude)
        {
            Vector3 limited_velocity = horizontal_velocity.normalized * this.max_horizontal_velocity_magnitude;
            this.body.linearVelocity = new Vector3(limited_velocity.x, this.body.linearVelocity.y, limited_velocity.z);
        }
    }

	void OnCollisionStay(Collision other)
	{
    	foreach (ContactPoint contact in other.contacts)
    	{
        	if (Vector3.Angle(Vector3.up, contact.normal) < this.max_slope)
        	{
            	this.is_grounded = true;
            	this.grounded_timer = this.grounded_timeout;
            	return;
        	}
    	}
	}
}
