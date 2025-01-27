using UnityEngine;
using System.Collections.Generic;

public class HarpoonLauncherSimple : MonoBehaviour
{
    public GameObject harpoonPrefab;
    public Transform launchPoint;    
    public SpriteRenderer launcherSprite;
    public LineRenderer trajectoryRenderer;
    public int trajectoryPoints = 30;
    public float angleSpeed = 30f;
    public float harpoonRadius = 0.2f;
    public float harpoonMass = 1f;
    public float customGravity = -9.8f;
    public float minForce = 10f;
    public float maxForce = 50f;
    public bool useMouse = false;
    private PhysicsManagerSimple physicsManager;
    private float launchAngle = 45f;
    private bool launchHeld = false;
    private float launchHoldTime = 0;
    private float launchForce = 0;
    public float launchSpeedTime = 1f;
    void Start()
    {
        // Find PhysicsManagerSimple dynamically
        physicsManager = FindObjectOfType<PhysicsManagerSimple>();

        if (physicsManager == null)
        {
            Debug.LogError("PhysicsManagerSimple not found.");
        }

        if (!physicsManager.enableDebugging)
        {
            trajectoryRenderer.enabled = false;
        }
    }

    void Update()
    {
        if (!useMouse)
        {
            if (Input.GetKey(KeyCode.UpArrow))
            {
                launchAngle += angleSpeed * Time.deltaTime;
            }
            if (Input.GetKey(KeyCode.DownArrow))
            {
                launchAngle -= angleSpeed * Time.deltaTime;
            }

            launchAngle = Mathf.Clamp(launchAngle, -30, 90);
            UpdateLauncherRotation();

            if (Input.GetKey(KeyCode.Space))
            {
                launchHeld = true;
                launchHoldTime += Time.deltaTime/launchSpeedTime;
                launchForce = Mathf.Clamp(launchForce + launchHoldTime, minForce, maxForce);
                if (physicsManager.enableDebugging)
                {
                    ShowTrajectory();
                }

            }
            else if (launchHeld)
            {
                LaunchHarpoon();
                launchHeld = false;
                launchHoldTime = 0;
                launchForce = minForce;
                EraseTrajectory();
            }
            /*if (Input.GetKeyDown(KeyCode.Space))
            {
                LaunchHarpoon();
            }*/

        }
        else
        {
            // --- 1) Rotate launcher to face the mouse ---

            // Get mouse position in world space
            Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            // Force z=0 if the launcher is in 2D
            mousePos.z = 0f;

            // Calculate direction from the launcher to the mouse
            Vector3 direction = mousePos - transform.position;

            // Get angle in degrees
            float angleRad = Mathf.Atan2(direction.y, direction.x);
            float angleDeg = angleRad * Mathf.Rad2Deg;

            // (Optional) If you want to clamp the angle to a range, do it here:
            // angleDeg = Mathf.Clamp(angleDeg, -30f, 90f);

            // Store it in launchAngle so other logic can use it
            launchAngle = Mathf.Clamp(angleDeg,-30f,90f);

            // Update the rotation of the launcher sprite
            UpdateLauncherRotation();
            if (Input.GetKey(KeyCode.Mouse0))
            {
                launchHeld = true;
                launchHoldTime += Time.deltaTime / launchSpeedTime;
                launchForce = Mathf.Clamp(launchForce + launchHoldTime, minForce, maxForce);
                if (physicsManager.enableDebugging)
                {
                    ShowTrajectory();
                }
            }
            else if (launchHeld)
            {
                LaunchHarpoon();
                launchHeld = false;
                launchHoldTime = 0;
                launchForce = minForce;
                EraseTrajectory();
            }
        }
    }

    private void UpdateLauncherRotation()
    {
        launcherSprite.transform.rotation = Quaternion.Euler(0, 0, launchAngle);
    }

    private void LaunchHarpoon()
    {
        GameObject harpoon = Instantiate(harpoonPrefab, launchPoint.position, Quaternion.identity);
        float angleRad = launchAngle * Mathf.Deg2Rad;
        Vector3 initialVelocity = new Vector3(Mathf.Cos(angleRad), Mathf.Sin(angleRad), 0) * launchForce;

        physicsManager.AddPhysicsObject(harpoon, initialVelocity, harpoonRadius, ObjectType.Harpoon);
    }

    private void ShowTrajectory()
    {
        if (!trajectoryRenderer) return;

        Vector3[] points = new Vector3[trajectoryPoints];
        float timeStep = 0.1f;
        float angleRad = launchAngle * Mathf.Deg2Rad;
        Vector3 initialVelocity = new Vector3(Mathf.Cos(angleRad), Mathf.Sin(angleRad), 0) * launchForce;

        for (int i = 0; i < trajectoryPoints; i++)
        {
            float t = i * timeStep;
            Vector3 point = launchPoint.position + initialVelocity * t + 0.5f * new Vector3(0, customGravity, 0) * t * t;
            points[i] = point;
        }

        trajectoryRenderer.positionCount = trajectoryPoints;
        trajectoryRenderer.SetPositions(points);
    }

    private void EraseTrajectory()
    {
        if (!trajectoryRenderer) return;
        trajectoryRenderer.positionCount = 0;
    }
}
