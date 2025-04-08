using UnityEngine;
using UnityEngine.XR;

[RequireComponent(typeof(LineRenderer))]
public class ControllerInput : MonoBehaviour
{
    public XRNode controllerNode;
    public float maxDistance = 10f;
    public LayerMask interactableLayers;
    
    private InputDevice controller;
    private LineRenderer lineRenderer;
    private GameObject grabbedObject = null;
    
    void Start()
    {
        controller = InputDevices.GetDeviceAtXRNode(controllerNode);
        lineRenderer = GetComponent<LineRenderer>();
    }

    void Update()
    {
        RaycastHit hit = AimingRaycast();

        if (grabbedObject == null && hit.collider != null && IsGrabbing())
        {
            GameObject hitObject = hit.collider.gameObject;
            
            if (hitObject.GetComponent<Pickable>())
            {
                grabbedObject = hitObject;
                hitObject.transform.parent = transform;
            }
        }
        else if (grabbedObject != null && !IsGrabbing())
        {
            grabbedObject.transform.parent = null;
            grabbedObject = null;
        }
    }

    private RaycastHit AimingRaycast()
    {
        Vector3 origin = transform.position;
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
        if (controller.TryGetFeatureValue(CommonUsages.gripButton, out bool isGrabbing))
        {
            return isGrabbing;
        }
        return false;
    }
}
