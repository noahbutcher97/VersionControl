using UnityEngine;

public class PlaneController : MonoBehaviour
{
    public GameObject planePrefab;  // Prefab for the plane
    public GameObject signPrefab;   // Prefab for the sign
    public float planeSpeed = 5f;   // Speed of the plane

    private GameObject planeInstance;
    private GameObject signInstance;
    private bool isPlaneMoving = false;

    void Update()
    {
        // Check for right mouse button click
        if (Input.GetMouseButtonDown(1))  // 1 = Right Mouse Button
        {
            // Spawn the plane at the cursor position
            Vector3 cursorPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            cursorPosition.z = 0;  // Set Z to 0 since we're in 2D

            // Instantiate the plane at the cursor position
            planeInstance = Instantiate(planePrefab, cursorPosition, Quaternion.identity);

            // Instantiate the sign and position it behind the plane
            signInstance = Instantiate(signPrefab, cursorPosition + new Vector3(-1.5f, 0, 0), Quaternion.identity);

            // Start moving the plane
            isPlaneMoving = true;
        }

        // Move the plane if it's active
        if (isPlaneMoving && planeInstance != null)
        {
            MovePlane();
        }
    }

    void MovePlane()
    {
        // Move the plane to the right across the screen
        planeInstance.transform.position += planeSpeed * Time.deltaTime * Vector3.right;

        // Ensure the sign follows the plane with a swaying effect
        if (signInstance != null)
        {
            Vector3 planePosition = planeInstance.transform.position;
            float sway = Mathf.Sin(Time.time * 2f) * 0.5f;  // Add a sine wave for swaying
            signInstance.transform.position = planePosition + new Vector3(-1.5f, sway, 0);
        }
    }
}