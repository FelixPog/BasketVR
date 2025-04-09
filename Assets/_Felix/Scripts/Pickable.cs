using System;
using UnityEngine;
using UnityEngine.XR;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Collider))]
public class Pickable : MonoBehaviour
{
    private Rigidbody rigidBody;
    private ControllerInput controller;
    
    void Start()
    {
        rigidBody = GetComponent<Rigidbody>();
    }

    private void FixedUpdate()
    {
        // This choice was made instead of re-parenting transform to avoid subtle position issue when moving the controller really fast

        if (controller != null)
        {
            rigidBody.MovePosition(controller.transform.position);
            rigidBody.MoveRotation(controller.transform.rotation);
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
        
        rigidBody.velocity = controller.GetAverageVelocity();
        rigidBody.angularVelocity = controller.GetAverageAngularVelocity();
        
        controller = null;
        transform.parent = null;
        rigidBody.isKinematic = false;
        
        
    }
}
