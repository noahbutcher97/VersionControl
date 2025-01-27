using System.Collections.Generic;
using UnityEngine;
using static PhysicsManager;

public class HarpoonLauncher : MonoBehaviour
{
    public GameObject harpoonPrefab;       // Harpoon prefab
    public Transform launchPoint;          // Launch point of the harpoon
    public SpriteRenderer launcherSprite;  // SpriteRenderer for the harpoon launcher contraption
    public float launchForce = 10f;        // Launch force applied to the harpoon
    public float angleSpeed = 30f;         // Speed at which the angle changes
    public float harpoonRadius = 0.2f;     // Radius (size) of the harpoon
    public LineRenderer trajectoryLine;    // LineRenderer to show the trajectory

    private float launchAngle = 45f;       // Initial launch angle in degrees
    private PhysicsManager physicsManager; // Reference to the PhysicsManager

    void Start()
    {
        // Find the PhysicsManager dynamically
        physicsManager = FindObjectOfType<PhysicsManager>();

        if (physicsManager == null)
        {
            Debug.LogError("PhysicsManager not found in the scene.");
        }

        // Disable trajectory line initially
        if (trajectoryLine != null)
        {
            trajectoryLine.enabled = false;
        }
    }

    void Update()
    {
        // Adjust the launch angle using up/down arrows
        if (Input.GetKey(KeyCode.UpArrow))
        {
            launchAngle += angleSpeed * Time.deltaTime;
        }
        if (Input.GetKey(KeyCode.DownArrow))
        {
            launchAngle -= angleSpeed * Time.deltaTime;
        }

        // Clamp the launch angle between 0 and 90 degrees
        launchAngle = Mathf.Clamp(launchAngle, 0, 90);

        // Update the rotation of the launcher sprite to reflect the current launch angle
        UpdateLauncherRotation();

        // Display trajectory if debugging is enabled in PhysicsManager
        if (physicsManager != null && physicsManager.enableDebugging)
        {
            DrawTrajectory();
        }
        else
        {
            if (trajectoryLine != null)
            {
                trajectoryLine.enabled = false;
            }
        }

        // Launch the harpoon when the spacebar is pressed
        if (Input.GetKeyDown(KeyCode.Space))
        {
            LaunchHarpoon();
        }
    }

    // Method to update the rotation of the launcher sprite
    private void UpdateLauncherRotation()
    {
        // Set the rotation of the launcher sprite based on the current launch angle
        launcherSprite.transform.rotation = Quaternion.Euler(0, 0, launchAngle);
    }

    private void LaunchHarpoon()
    {
        // Instantiate the harpoon at the launch point
        GameObject harpoon = Instantiate(harpoonPrefab, launchPoint.position, Quaternion.identity);

        // Calculate the initial velocity of the harpoon based on the launch angle
        float angleRad = launchAngle * Mathf.Deg2Rad;  // Convert launch angle to radians
        Vector3 initialVelocity = new Vector3(Mathf.Cos(angleRad), Mathf.Sin(angleRad), 0) * launchForce;

        // Register the harpoon with the PhysicsManager with the specific radius for harpoons
        if (physicsManager != null)
        {
            physicsManager.AddPhysicsObject(harpoon, initialVelocity, harpoonRadius, ObjectType.Harpoon, CollisionMethod.Circle, new List<CollisionBehavior> { CollisionBehavior.Destroy },3f, 0f);
        }

        // Set the harpoon's initial rotation to face the direction of launch
        harpoon.transform.rotation = Quaternion.Euler(0, 0, launchAngle);
    }

    // Draw the trajectory of the harpoon based on the current launch angle and launch force
    private void DrawTrajectory()
    {
        int numPoints = 30;  // Number of points to display in the trajectory
        Vector3[] points = new Vector3[numPoints];

        float timeStep = 0.1f;  // Time step for calculating positions
        Vector3 initialVelocity = new Vector3(Mathf.Cos(launchAngle * Mathf.Deg2Rad), Mathf.Sin(launchAngle * Mathf.Deg2Rad), 0) * launchForce;
        Vector3 currentPosition = launchPoint.position;

        for (int i = 0; i < numPoints; i++)
        {
            float t = i * timeStep;
            Vector3 newPos = currentPosition + initialVelocity * t + 0.5f * t * t * new Vector3(0, physicsManager.gravity, 0);
            points[i] = newPos;
        }

        // Set trajectory line points
        if (trajectoryLine != null)
        {
            trajectoryLine.enabled = true;
            trajectoryLine.positionCount = numPoints;
            trajectoryLine.SetPositions(points);
        }
    }
}
