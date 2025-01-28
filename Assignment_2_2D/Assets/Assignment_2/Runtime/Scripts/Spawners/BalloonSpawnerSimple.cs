using UnityEngine;

/// <summary>
/// Spawns balloons at regular intervals in "valley" areas of the terrain,
/// using the custom 2D physics manager and scene manager logic.
/// </summary>
public class BalloonSpawnerSimple : MonoBehaviour
{
    [Header("Balloon Spawn Settings")]
    [Tooltip("Prefab used to instantiate new balloons.")]
    public GameObject balloonPrefab;

    [Tooltip("Maximum number of balloons allowed concurrently.")]
    public int maxBalloons = 10;

    [Tooltip("Time interval (seconds) between automatic spawns.")]
    public float spawnRate = 2f;

    [Tooltip("Collision radius assigned to each balloon (for custom physics).")]
    public float balloonRadius = 0.5f;

    [Tooltip("Whether to show debug logs or visual debugging lines.")]
    public bool enableDebugging = true;

    [Header("Parenting/Environment")]
    [Tooltip("If true, newly spawned balloons are parented under the environment root, if found.")]
    public bool parentBalloonsToEnvironment = true;

    // Internal references
    private PhysicsManagerSimple physicsManager;
    private SceneManager sceneManager;

    // Internal timing
    private float nextSpawnTime;

    void Start()
    {
        // Find references
        physicsManager = FindObjectOfType<PhysicsManagerSimple>();
        sceneManager = FindObjectOfType<SceneManager>();

        if (physicsManager == null)
            Debug.LogError("[BalloonSpawnerSimple] PhysicsManagerSimple not found in the scene.");
        if (sceneManager == null)
            Debug.LogError("[BalloonSpawnerSimple] SceneManager not found in the scene.");

        // Start spawn timer
        nextSpawnTime = Time.time + spawnRate;
    }

    void Update()
    {
        // Spawn balloons continuously until max balloons are reached
        if (Time.time >= nextSpawnTime && physicsManager != null && physicsManager.balloons.Count < maxBalloons)
        {
            SpawnBalloon();
            nextSpawnTime = Time.time + spawnRate;
        }
    }

    /// <summary>
    /// Spawns a single balloon in a "valley" area of the terrain,
    /// skipping the tower region, ensuring it doesn't collide with existing balloons.
    /// </summary>
    void SpawnBalloon()
    {
        if (sceneManager == null || physicsManager == null) return;

        // The maximum vertical bound for spawns. 
        // (We assume sceneManager.maxY is the top in local coords.)
        float maxSpawnY = sceneManager.maxY;

        // Ask SceneManager for a random valley spawn location
        Vector3 spawnPos = sceneManager.GetRandomValleySpawnPosition(maxSpawnY);

        if (spawnPos == Vector3.zero)
        {
            // No valid location found
            if (enableDebugging) Debug.Log("[BalloonSpawnerSimple] No valid valley location found.");
            return;
        }

        // Check collision with existing balloons
        if (!IsCollidingWithExistingBalloon(spawnPos))
        {
            // Create the balloon
            GameObject newBalloon = Instantiate(balloonPrefab);

            // Optionally parent it to the environment root if that approach is used
            if (parentBalloonsToEnvironment)
            {
                // Attempt to find an object named "EnvironmentRoot" 
                // (assuming your SceneManager uses that approach).
                GameObject envRoot = GameObject.Find("EnvironmentRoot");
                if (envRoot != null)
                {
                    newBalloon.transform.SetParent(envRoot.transform, false);
                }
            }

            // Set localPosition so it appears in the same local coordinate space as the environment
            newBalloon.transform.localPosition = spawnPos;

            // Register balloon with PhysicsManagerSimple
            physicsManager.AddPhysicsObject(
                newBalloon,
                Vector3.up,     // initial upward velocity
                balloonRadius,
                ObjectType.Balloon
            );

            if (enableDebugging)
            {
                Debug.Log($"[BalloonSpawnerSimple] Spawned balloon at local pos {spawnPos}");
            }
        }
        else
        {
            if (enableDebugging) Debug.Log("[BalloonSpawnerSimple] Collision detected, skipping spawn.");
        }
    }

    /// <summary>
    /// Checks if the given spawn position overlaps any existing balloon (by radius).
    /// </summary>
    bool IsCollidingWithExistingBalloon(Vector3 spawnPosition)
    {
        if (physicsManager == null) return false;

        foreach (var balloon in physicsManager.balloons)
        {
            float distance = Vector3.Distance(balloon.position, spawnPosition);
            // If distance < sum of radii, it's colliding
            if (distance < (balloon.radius + balloonRadius))
            {
                return true;
            }
        }
        return false;
    }
}
