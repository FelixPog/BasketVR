using UnityEngine;
using UnityEngine.XR;

[RequireComponent(typeof(LineRenderer))]
public class ControllerInput : MonoBehaviour
{
    public XRNode deviceNode;
    public float maxDistance = 10f;
    public LayerMask interactableLayers;
    
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
        RaycastHit hit = AimingRaycast();

        if (pickable == null && hit.collider != null && IsGrabbing())
        {
            pickable = hit.collider.gameObject.GetComponent<Pickable>();
            if (pickable != null)
            {
                pickable.Pickup(this);
            }
        }
        else if (pickable != null && !IsGrabbing())
        {
            pickable.Release(this, device);
            pickable = null;
        }
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
    
    private bool IsGrabbing()
    {
        if (device.TryGetFeatureValue(CommonUsages.gripButton, out bool isGrabbing))
        {
            return isGrabbing;
        }
        return false;
    }
}
