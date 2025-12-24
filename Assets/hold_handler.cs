using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class hold_handler : MonoBehaviour
{
	//configuration
	[SerializeField] float reach = 15f;
    [SerializeField] float force = 120f;
	[SerializeField] float damping = 12f;
	InputAction hold_action;
	new Camera camera;
	LineRenderer line_renderer;
	//state
	bool hold_is_pressed;
    bool hold_was_pressed;
    bool hold_was_released;
	GameObject anchor = null;
	float drag_depth = 0f;
	GameObject stored_game_object = null;

	void Start()
    {
        hold_action = InputSystem.actions.FindAction("hold");
		camera = Camera.main;
		line_renderer = GetComponent<LineRenderer>();
		line_renderer.positionCount = 0;
    }

	void Update()
	{
		hold_is_pressed = hold_action.IsPressed();
		hold_was_pressed = hold_action.WasPressedThisFrame();
		hold_was_released = hold_action.WasReleasedThisFrame();

		update_outline();

		if (hold_was_pressed)
		{
			try_attach();
		}

		if (hold_is_pressed && anchor != null)
		{
			drag();
		}

		if (hold_was_released && anchor != null)
		{
			detach();
		}
	}

	void update_outline()
	{
		Ray ray = camera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
		RaycastHit hit;
		if (Physics.Raycast(ray, out hit, reach))
		{
			if (hit.transform.CompareTag("holdable"))
			{
				GameObject game_object = hit.collider.gameObject;
				if (game_object != stored_game_object)
				{
					clear_outline();
					//fix magic
					game_object.layer = 6;
					stored_game_object = game_object;
				}
			}
		}
		else
		{
			clear_outline();
		}
	}

	void clear_outline()
	{
		if (stored_game_object != null)
        {
			//fix magic
            stored_game_object.layer = 0;
            stored_game_object = null;
        }
	}

	void try_attach()
	{
		Ray ray = camera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
		RaycastHit hit;
		if (Physics.Raycast(ray, out hit, reach))
		{
			if (hit.transform.CompareTag("holdable") && hit.rigidbody != null)
			{
				drag_depth = camera.transform.InverseTransformPoint(hit.point).z;

				anchor = new GameObject("attachment_anchor");
				anchor.transform.position = hit.point;

				Rigidbody anchor_body = anchor.AddComponent<Rigidbody>();
				anchor_body.isKinematic = true;

				ConfigurableJoint joint = anchor.AddComponent<ConfigurableJoint>();
				joint.connectedBody = hit.rigidbody;
				joint.connectedAnchor = hit.transform.InverseTransformPoint(hit.point);

				joint.xMotion = ConfigurableJointMotion.Limited;
    			joint.yMotion = ConfigurableJointMotion.Limited;
    			joint.zMotion = ConfigurableJointMotion.Limited;

    			SoftJointLimit limit = new SoftJointLimit();
				//fix magic
    			limit.limit = 2.0f;
    			joint.linearLimit = limit;
				//fix magic
    			joint.linearLimitSpring = new SoftJointLimitSpring { spring = 100f, damper = 10f };

    			joint.angularXMotion = ConfigurableJointMotion.Free;
    			joint.angularYMotion = ConfigurableJointMotion.Free;
    			joint.angularZMotion = ConfigurableJointMotion.Free;

				joint.xDrive = joint_drive_init(force, damping);
				joint.yDrive = joint_drive_init(force, damping);
				joint.zDrive = joint_drive_init(force, damping);

				line_renderer.positionCount = 2;
			}
		}
	}

	JointDrive joint_drive_init(float force, float damping)
	{
		return new JointDrive
		{
			positionSpring = force,
			positionDamper = damping,
			maximumForce = Mathf.Infinity,
		};
	}

	void drag()
	{
		anchor.transform.position = camera.ViewportToWorldPoint(new Vector3(0.5f, 0.5f, drag_depth));
		line_renderer.SetPosition(0, anchor.transform.position);
		ConfigurableJoint joint = anchor.GetComponent<ConfigurableJoint>();
		line_renderer.SetPosition(1, joint.connectedBody.transform.TransformPoint(joint.connectedAnchor));
	}
	void detach()
	{
		line_renderer.positionCount = 0;
		Destroy(anchor);
	}
}
