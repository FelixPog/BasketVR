using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Collider))]
public class Pickable : MonoBehaviour
{
    [Min(0.0f)] public float throwPowerMultiplier = 1.5f;
    //[Min(0.0f)] public float maxVelocity = 30.0f;
    
    private bool shouldRelease;
    private Rigidbody rigidBody;
    private ControllerInput controller;
    
    void Start()
    {
        rigidBody = GetComponent<Rigidbody>();
    }

    private void FixedUpdate()
    {
        if (controller != null && !shouldRelease)
        {
            // This choice was made instead of re-parenting transform to avoid subtle position issue when moving the controller really fast
            rigidBody.MovePosition(controller.transform.position);
            rigidBody.MoveRotation(controller.transform.rotation);
        }
        else if (controller != null && shouldRelease)
        {
            rigidBody.isKinematic = false;
            shouldRelease = false;
            Vector3 velocity = controller.GetPeakAverageVelocity();
            rigidBody.velocity = velocity * throwPowerMultiplier;
            rigidBody.angularVelocity = controller.GetPeakAverageAngularVelocity();
            controller = null;
            transform.parent = null;
        }
    }

    public void Pickup(ControllerInput controller)
    {
        this.controller =  controller;
        
        transform.localPosition = Vector3.zero;
        rigidBody.isKinematic = true;
        rigidBody.velocity = Vector3.zero;
    }

    public void Release(ControllerInput releaseController)
    {
        if (releaseController != controller)
        {
            return;
        }
        
        shouldRelease = true;
    }
}
