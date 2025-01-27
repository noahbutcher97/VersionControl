using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HarpoonLauncherTrajectory : MonoBehaviour
{
    public GameObject harpoonPrefab;      // Harpoon prefab
    public Transform launchPoint;         // Launch point of the harpoon
    public LineRenderer trajectoryLine;   // LineRenderer for showing the trajectory

    public float launchForce = 10f;       // Initial launch force
    public float gravity = -9.8f;         // Gravity value

    public int numberOfPoints = 50;       // Number of points to calculate for the trajectory preview
    public float timeStep = 0.1f;         // Time step between points

    private float launchAngle = 45f;      // Launch angle in degrees

    void Update()
    {
        // Adjust the launch angle with up/down arrows (for testing)
        if (Input.GetKey(KeyCode.UpArrow)) launchAngle += 30f * Time.deltaTime;
        if (Input.GetKey(KeyCode.DownArrow)) launchAngle -= 30f * Time.deltaTime;

        // Clamp the angle between 0 and 90 degrees
        launchAngle = Mathf.Clamp(launchAngle, 0, 90);

        // Calculate and display the trajectory
        DisplayTrajectory();
    }

    private void DisplayTrajectory()
    {
        Vector3[] trajectoryPoints = new Vector3[numberOfPoints];
        float angleRad = launchAngle * Mathf.Deg2Rad; // Convert angle to radians
        Vector3 startPos = launchPoint.position;

        // Initial velocity components (X and Y)
        float initialVelocityX = launchForce * Mathf.Cos(angleRad);
        float initialVelocityY = launchForce * Mathf.Sin(angleRad);

        for (int i = 0; i < numberOfPoints; i++)
        {
            // Time at this point in the trajectory
            float t = i * timeStep;

            // Calculate X and Y positions using the kinematic equations
            float xPos = startPos.x + initialVelocityX * t;
            float yPos = startPos.y + initialVelocityY * t + 0.5f * gravity * t * t;

            // Store the calculated point in the array
            trajectoryPoints[i] = new Vector3(xPos, yPos, 0);
        }

        // Set the positions for the LineRenderer to draw the trajectory
        trajectoryLine.positionCount = numberOfPoints;
        trajectoryLine.SetPositions(trajectoryPoints);
    }
}
