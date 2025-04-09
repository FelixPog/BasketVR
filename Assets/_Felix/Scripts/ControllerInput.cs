using System.Collections.Generic;
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
    
    private LineRenderer lineRenderer;
    private Pickable pickable = null;
    private Queue<(Vector3 velocity, float timestamp)> velocityBuffer = new();
    private Queue<(Vector3 angularVelocity, float timestamp)> angularVelocityBuffer = new();
    
    [Header("Movement")]
    
    public float movementSpeed = 5.0f;
    public CharacterController characterController;
    public float turnSpeed = 60.0f;
    
    
    private Vector3 lastPosition = Vector3.zero; 
    
    void Start()
    {
        device = InputDevices.GetDeviceAtXRNode(deviceNode);
        lineRenderer = GetComponent<LineRenderer>();
        lastPosition = transform.position;
    }

    void Update()
    {
        ComputeAverageVelocity();
        
        // Movement 
        UpdatePlayerPosition();
        UpdatePlayerRotation();
        
        // Interaction 
        TryPickupAndRelease(AimingRaycast());
        TryGrabAndRelease();
    }

    public Vector3 GetAverageVelocity()
    {
        return GetAverageVectorFromBuffer(velocityBuffer);
    }

    public Vector3 GetAverageAngularVelocity()
    {
        return GetAverageVectorFromBuffer(angularVelocityBuffer);
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

    private Vector3 GetAverageVectorFromBuffer(Queue<(Vector3 velocity, float timestamp)> vectorBuffer)
    {
        if (vectorBuffer.Count == 0)
        {
            return Vector3.zero;
        }

        Vector3 velocitySum = Vector3.zero;
        foreach ((Vector3 velocity, float timeStamp) in vectorBuffer)
        {
            velocitySum += velocity;
        }
        
        return velocitySum / vectorBuffer.Count;
    }
}
