using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.XR;

[RequireComponent(typeof(LineRenderer))]
public class ControllerInput : MonoBehaviour
{
    [Header("Device")]
    
    public XRNode deviceNode;
    
    private InputDevice device;
    
    [Header("Interaction")]
    
    public float maxDistance = 10f;
    public LayerMask interactableLayers;
    public SphereCollider grabInteractionZone;
    public float velocityBufferDuration = 0.25f;
    public int pickVelocityCount = 10;
    
    private LineRenderer lineRenderer;
    private Pickable pickable = null;
    private Queue<(Vector3 velocity, float timestamp)> velocityBuffer = new();
    private Queue<(Vector3 angularVelocity, float timestamp)> angularVelocityBuffer = new();
    
    [Header("Movement")]
    
    public float movementSpeed = 5.0f;
    public CharacterController characterController;
    
    void Start()
    {
        device = InputDevices.GetDeviceAtXRNode(deviceNode);
        lineRenderer = GetComponent<LineRenderer>();
    }

    void Update()
    {
        ComputeAverageVelocity();
        
        // Movement 
        UpdatePlayerPosition();
        
        // Interaction 
        TryPickupAndRelease(AimingRaycast());
        TryGrabAndRelease();
    }

    public Vector3 GetPeakAverageVelocity()
    {
        return GetPeakAverageVectorFromBuffer(velocityBuffer);
    }

    public Vector3 GetPeakAverageAngularVelocity()
    {
        return GetPeakAverageVectorFromBuffer(angularVelocityBuffer);
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
            pickable.Release(this);
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
            pickable.Release(this);
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

        if (moveInput == Vector2.zero)
        {
            return;
        }

        Transform cameraTransform = Camera.main.transform;
        Vector3 cameraForward = cameraTransform.forward;
        Vector3 cameraRight = cameraTransform.right;
        
        cameraForward.y = 0f;
        cameraRight.y = 0f;
        cameraForward.Normalize();
        cameraRight.Normalize();
        
        Vector3 moveDirection = cameraForward * moveInput.y + cameraRight * moveInput.x;

        characterController.Move(moveDirection * movementSpeed * Time.deltaTime);
    }
    
    private void ComputeAverageVelocity()
    {
        if (device.TryGetFeatureValue(CommonUsages.deviceVelocity, out Vector3 velocityInput)
            && device.TryGetFeatureValue(CommonUsages.deviceAngularVelocity, out Vector3 angularVelocityInput))
        {
            float now = Time.time;
            velocityBuffer.Enqueue((velocityInput, now));
            angularVelocityBuffer.Enqueue((angularVelocityInput, now));

            while (velocityBuffer.Count > 0 && now - velocityBuffer.Peek().timestamp > velocityBufferDuration)
            {
                velocityBuffer.Dequeue();
            }
            
            while (angularVelocityBuffer.Count > 0 && now - angularVelocityBuffer.Peek().timestamp > velocityBufferDuration)
            {
                angularVelocityBuffer.Dequeue();
            }
        }
    }

    private Vector3 GetPeakAverageVectorFromBuffer(Queue<(Vector3 velocity, float timestamp)> vectorBuffer)
    {
        List<Vector3> peakVelocities = vectorBuffer
            .OrderByDescending(peakVelocity => peakVelocity.velocity.magnitude)
            .Take(pickVelocityCount)
            .Select(peakVelocity => peakVelocity.velocity)
            .ToList();

        Vector3 averagePeak = peakVelocities.Aggregate(Vector3.zero, (sum, v) => sum + v) / peakVelocities.Count;
        return averagePeak;
    }
}
