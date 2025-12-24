using UnityEngine;
using UnityEngine.InputSystem;

public class hold_handler : MonoBehaviour
{
	//configuration
	[SerializeField] float reach = 15f;
    [SerializeField] float spring = 120f;
	[SerializeField] float damper = 12f;
	[SerializeField] float limit = 2f;
	[SerializeField] float bounciness = 0.5f;
	[SerializeField] float contact = 0.2f;
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
	//constants
	const int default_layer = 0;
	const int outline_layer = 6;

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
					game_object.layer = outline_layer;
					stored_game_object = game_object;
				}
			}
			else
			{
				clear_outline();
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
            stored_game_object.layer = default_layer;
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

    			joint.linearLimit = new SoftJointLimit() { limit = limit, bounciness = bounciness, contactDistance = contact };
    			joint.linearLimitSpring = new SoftJointLimitSpring { spring = spring, damper = damper };

    			joint.angularXMotion = ConfigurableJointMotion.Free;
    			joint.angularYMotion = ConfigurableJointMotion.Free;
    			joint.angularZMotion = ConfigurableJointMotion.Free;

				joint.xDrive = new JointDrive { positionSpring = spring, positionDamper = damper, maximumForce = Mathf.Infinity };
				joint.yDrive = new JointDrive { positionSpring = spring, positionDamper = damper, maximumForce = Mathf.Infinity };
				joint.zDrive = new JointDrive { positionSpring = spring, positionDamper = damper, maximumForce = Mathf.Infinity };

				line_renderer.positionCount = 2;
			}
		}
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
