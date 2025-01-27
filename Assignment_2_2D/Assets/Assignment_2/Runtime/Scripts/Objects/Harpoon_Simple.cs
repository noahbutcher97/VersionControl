using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Harpoon_Simple : MonoBehaviour
{
    public Vector3 velocity;  // The velocity of the harpoon

    // Initialize the velocity when the harpoon is instantiated
    public void Initialize(Vector3 direction, float launchForce)
    {
        velocity = direction * launchForce;
    }
}
