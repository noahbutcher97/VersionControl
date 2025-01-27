using UnityEngine;
using System.Collections.Generic;

public class BalloonSpawner : MonoBehaviour
{
    public GameObject balloonPrefab;  // Reference to the balloon prefab
    public float balloonRadius = 0.5f;  // Default balloon radius
    public float balloonMass = 1f;      // Default balloon mass
    public float chaoticMotionAmount = 0.5f;  // Chaos factor for horizontal jitter

    private PhysicsManager physicsManager;

    void Start()
    {
        // Find PhysicsManager dynamically
        physicsManager = FindObjectOfType<PhysicsManager>();
        if (physicsManager == null)
        {
            Debug.LogError("PhysicsManager not found in the scene.");
        }
    }

    void Update()
    {
        // Spawn balloon under the mouse cursor when left-clicked
        if (Input.GetMouseButtonDown(0))
        {
            Vector3 spawnPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            spawnPosition.z = 0;  // Make sure it's 2D

            GameObject balloon = Instantiate(balloonPrefab, spawnPosition, Quaternion.identity);

            // Set up the collision behaviors and method for balloons
            List<CollisionBehavior> balloonCollisionBehaviors = new List<CollisionBehavior>()
            {
                CollisionBehavior.Bounce
            };

            // Add the physics object to the PhysicsManager
            physicsManager.AddPhysicsObject(balloon, Vector3.zero, balloonRadius, ObjectType.Balloon, CollisionMethod.Circle, balloonCollisionBehaviors, balloonMass, chaoticMotionAmount);
        }
    }
}
