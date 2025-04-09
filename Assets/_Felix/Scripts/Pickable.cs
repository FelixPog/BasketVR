using System;
using UnityEngine;
using UnityEngine.XR;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Collider))]
public class Pickable : MonoBehaviour
{
    private Rigidbody rigidBody;
    private ControllerInput grabbingController;
    
    void Start()
    {
        rigidBody = GetComponent<Rigidbody>();
    }

    private void FixedUpdate()
    {
        // This choice was made instead of re-parenting transform to avoid subtle position issue when moving the controller really fast

        if (grabbingController != null)
        {
            rigidBody.MovePosition(grabbingController.transform.position);
            rigidBody.MoveRotation(grabbingController.transform.rotation);
        }
    }

    public void Pickup(ControllerInput controller)
    {
        grabbingController =  controller;
        
        transform.localPosition = Vector3.zero;
        rigidBody.isKinematic = true;
        rigidBody.velocity = Vector3.zero;
    }

    public void Release(ControllerInput releaseController, InputDevice releaseDevice)
    {
        if (releaseController != grabbingController)
        {
            return;
        }
        
        if (releaseDevice.TryGetFeatureValue(CommonUsages.deviceVelocity, out Vector3 velocity)
            && releaseDevice.TryGetFeatureValue(CommonUsages.deviceAngularVelocity, out Vector3 angularVelocity))
        {
            Debug.DrawRay(transform.position, velocity, Color.green, 2f);
            rigidBody.velocity = velocity;
            rigidBody.angularVelocity = angularVelocity;
        }
        
        grabbingController = null;
        transform.parent = null;
        rigidBody.isKinematic = false;
        
        
    }
}
