using UnityEngine;
using UnityEngine.XR;

[RequireComponent(typeof(LineRenderer))]
public class ControllerInput : MonoBehaviour
{
    public XRNode deviceNode;
    public float maxDistance = 10f;
    public LayerMask interactableLayers;

    public SphereCollider grabInteractionZone;
    
    public float movementSpeed = 5.0f;
    public CharacterController characterController;
    public float turnSpeed = 60.0f;
    
    private InputDevice device;
    private LineRenderer lineRenderer;
    private Pickable pickable = null;
    
    void Start()
    {
        device = InputDevices.GetDeviceAtXRNode(deviceNode);
        lineRenderer = GetComponent<LineRenderer>();
    }

    void Update()
    {
        UpdatePlayerPosition();
        UpdatePlayerRotation();
        TryPickupAndRelease(AimingRaycast());
        TryGrabAndRelease();
    }

    private RaycastHit AimingRaycast()
    {
        Vector3 origin = transform.position;
        
        if (pickable != null)
        {
            lineRenderer.SetPosition(0, origin);
            lineRenderer.SetPosition(1, origin);
            return new RaycastHit();
        }
        
        Vector3 direction = transform.forward;

        Ray ray = new Ray(origin, direction);

        Vector3 endPosition;

        if (Physics.Raycast(ray, out var hit, maxDistance, interactableLayers))
        {
            endPosition = hit.point;
        }
        else
        {
            endPosition = origin + direction * maxDistance;
        }

        lineRenderer.SetPosition(0, origin);
        lineRenderer.SetPosition(1, endPosition);
        
        return hit;
    }
    
    private bool IsTriggerPressed()
    {
        if (device.TryGetFeatureValue(CommonUsages.triggerButton, out bool isTriggerPressed))
        {
            return isTriggerPressed;
        }
        return false;
    }

    private bool IsGrabPressed()
    {
        if (device.TryGetFeatureValue(CommonUsages.gripButton, out bool isGrabPressed))
        {
            return isGrabPressed;
        }
        return false;
    }

    private void TryPickupAndRelease(RaycastHit hit)
    {
        if (pickable == null && hit.collider != null && IsTriggerPressed())
        {
            pickable = hit.collider.gameObject.GetComponent<Pickable>();
            if (pickable != null)
            {
                pickable.Pickup(this);
            }
        }
        else if (pickable != null && !IsTriggerPressed() && !IsGrabPressed())
        {
            pickable.Release(this, device);
            pickable = null;
        }
    }

    private void TryGrabAndRelease()
    {
        if (pickable == null && grabInteractionZone != null && IsGrabPressed())
        {
            Collider[] hitColliders = new Collider[10];
            int collidersFoundCount = Physics.OverlapSphereNonAlloc(grabInteractionZone.transform.position, grabInteractionZone.radius, hitColliders);
            for (int  i = 0; i < collidersFoundCount; i++)
            {
                pickable = hitColliders[i].GetComponent<Pickable>();
                if (pickable != null)
                {
                    pickable.Pickup(this);
                    break;
                }
            }
        }
        else if (pickable != null && !IsTriggerPressed() && !IsGrabPressed())
        {
            pickable.Release(this, device);
            pickable = null;
        }
    }
    
    private void UpdatePlayerPosition()
    {
        if (characterController == null || deviceNode != XRNode.LeftHand)
        {
            return;
        }
        
        device.TryGetFeatureValue(CommonUsages.primary2DAxis, out Vector2 moveInput);
        Vector3 moveDirection = transform.TransformDirection(new Vector3(moveInput.x, 0, moveInput.y));
        moveDirection.y = 0.0f;

        characterController.Move(moveDirection * movementSpeed * Time.deltaTime);
    }

    private void UpdatePlayerRotation()
    {
        if (deviceNode != XRNode.RightHand)
        {
            return;
        }
        
        device.TryGetFeatureValue(CommonUsages.primary2DAxis, out Vector2 turnInput);
        
        float turnAmount = turnInput.x * turnSpeed * Time.deltaTime;
        characterController.transform.Rotate(0, turnAmount, 0);
    }
}
