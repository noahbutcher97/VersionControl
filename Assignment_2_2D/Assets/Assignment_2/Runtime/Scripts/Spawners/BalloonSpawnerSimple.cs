using UnityEngine;

public class BalloonSpawnerSimple : MonoBehaviour
{
    public GameObject balloonPrefab;
    public int maxBalloons = 10;
    public float spawnRate = 2f;
    public bool enableDebugging = true;
    public float xMinOffset = 0f;
    public float yMinOffset = 0f;
    public float xMaxOffset = 0f;
    public float yMaxOffset = 0f;
    public float balloonRadius = 0.5f;
    private float nextSpawnTime;
    private PhysicsManagerSimple physicsManager;
    private SceneManager sceneManager;

    private Vector2 minX;  // Start of the valley (X, Y)
    private Vector2 valleyEnd;    // End of the valley (X, Y)
    private float valleyTopY;     // Y position for the top of the cliffs
    private float valleyBottomY;  // Y position for the valley floor (top of the mesh)

    private Vector3 valleyBottomLeft;
    private Vector3 valleyBottomRight;
    private Vector3 valleyTopLeft;
    private Vector3 valleyTopRight;


    void Start()
    {
        physicsManager = FindObjectOfType<PhysicsManagerSimple>();
        sceneManager = FindObjectOfType<SceneManager>();

        if (physicsManager == null)
        {
            Debug.LogError("PhysicsManager not found.");
        }
        if (sceneManager == null)
        {
            Debug.LogError("SceneManager not found.");
        }

        // Delay calculation to ensure terrain is generated
        Invoke("CalculateValleyBounds", 1f);  // Delayed to allow terrain to generate

        nextSpawnTime = Time.time + spawnRate;
    }

    void Update()
    {
        CalculateValleyBounds();
        // Spawn balloons continuously until max balloons are reached
        if (Time.time >= nextSpawnTime && physicsManager.balloons.Count < maxBalloons)
        {
            SpawnBalloon();
            nextSpawnTime = Time.time + spawnRate;
        }       
       
    }

    void SpawnBalloon()
    {
        // Use calculated valley bounds for random spawn position
        float randomX = Random.Range(minX.x, valleyEnd.x);
        float randomY = Random.Range(valleyBottomLeft.y, valleyTopRight.y);

        Vector3 spawnPosition = new Vector3(randomX, randomY, 0);

        // Check if the spawn position is clear of other balloons
        if (!IsCollidingWithExistingBalloon(spawnPosition))
        {
            GameObject newBalloon = Instantiate(balloonPrefab, spawnPosition, Quaternion.identity);
            physicsManager.AddPhysicsObject(newBalloon, new Vector3(0, 1f, 0), balloonRadius, ObjectType.Balloon);
        }
    }

    bool IsCollidingWithExistingBalloon(Vector3 spawnPosition)
    {
        foreach (var balloon in physicsManager.balloons)
        {
            float distance = Vector3.Distance(balloon.position, spawnPosition);
            if (distance < balloon.radius * 2)
            {
                return true;
            }
        }
        return false;
    }

    private void CalculateValleyBounds()
    {

        float minX = sceneManager.minX + xMinOffset;
        float maxX = sceneManager.maxX + xMaxOffset;
        float minY = sceneManager.minY + yMinOffset;
        float maxY = sceneManager.maxY + yMaxOffset;


        // Set the valley bounds based on the detected valley bottom and sides
        this.minX = new Vector2(minX, minY);
        valleyEnd = new Vector2(maxX, maxY);

        // Debug log the calculated valley bounds
        Debug.Log($"Valley Bounds: Start({this.minX.x}, {this.minX.y}), End({valleyEnd.x}, {valleyEnd.y}), TopY: {valleyTopY}, BottomY: {valleyBottomY}");
        valleyBottomLeft = new Vector3(minX, minY, 0);
        valleyBottomRight = new Vector3(maxX, minY, 0);
        valleyTopLeft = new Vector3(minX, maxY, 0);
        valleyTopRight = new Vector3(maxX, maxY, 0);

        if (enableDebugging)
        {
            // Draw lines connecting the four corners of the valley bounds
            Debug.DrawLine(valleyBottomLeft, valleyBottomRight, Color.green);  // Bottom boundary
            Debug.DrawLine(valleyTopLeft, valleyTopRight, Color.green);        // Top boundary
            Debug.DrawLine(valleyBottomLeft, valleyTopLeft, Color.green);      // Left boundary
            Debug.DrawLine(valleyBottomRight, valleyTopRight, Color.green);    // Right boundary
        }
    }
}
