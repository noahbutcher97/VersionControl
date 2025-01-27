using UnityEngine;

public class SceneManager : MonoBehaviour
{
    public LineRenderer lineRenderer;            // Reference to the LineRenderer component for the terrain
    public GameObject playerPrefab;              // The Prefab for the player (castle with launcher)
    public Camera mainCamera;                    // Reference to the main camera
    public int numPoints = 10;                   // Number of points for the terrain line
    public float descentRate = 1f;               // Rate of descent for the valley
    public float valleyLengthPercent = .3f;      // Percentage of terrain length occupied by the valley
    public float heightOffset = 0f;              // Offset for terrain height
    public int slopePointDensity = 3;            // Number of points for the slopes

    public Material dirtMaterial;                // Material for the dirt mesh
    public Material groundMaterial;              // Material for the ground mesh
    public float groundVerticalOffset = 1.0f;    // Vertical offset for the height of the ground above the terrain profile
    public float groundXOffsetScale = 0.2f;      // Scale for X-offset when elevation is changing (ascending or descending)
    public float sharpAngleThreshold = 1.0f;     // Threshold to detect sharp elevation change for 90-degree angles

    private Mesh dirtMesh;                       // Mesh for the dirt below the terrain
    public Mesh groundMesh;                     // Mesh for the ground layer above the terrain

    private GameObject dirtMeshObject;           // GameObject to hold the dirt mesh
    private GameObject groundMeshObject;         // GameObject to hold the ground mesh

    private Vector3 bottomLeftCorner;            // World position of the bottom-left corner of the screen
    private Vector3 topRightCorner;              // World position of the top-right corner of the screen
    private float terrainWidth;                  // Calculated width based on the screen width
    private float minYPosition;                  // Minimum Y position for the terrain (flat ground start)
    private float lowYPosition;                  // Y position for the valley
    private GameObject instantiatedPlayer;       // Reference to the instantiated player prefab
    private PhysicsManagerSimple physicsManager;       // Dynamically found reference to Physics Manager
    public float minX;
    public float maxX;  
    public float minY;
    public float maxY;
    void Start()
    {
        // Find the PhysicsManager dynamically
        physicsManager = FindObjectOfType<PhysicsManagerSimple>();

        if (physicsManager == null)
        {
            Debug.LogError("PhysicsManager not found in the scene.");
        }
        // Set up the scene
        CalculateTerrainBounds();
        GenerateTerrain();
        SpawnPlayer();
    }

    void Update()
    {
        // Example logic: Press 'R' to regenerate the terrain at runtime
        if (Input.GetKeyDown(KeyCode.R))
        {
            CalculateTerrainBounds(); // Ensure the terrain bounds get recalculated
            GenerateTerrain();
            RepositionPlayerOnTerrain();
        }
    }
    // Calculate the bounds for the terrain based on the camera and heightOffset
    private void CalculateTerrainBounds()
    {
        // Calculate the bottom-left corner of the screen in world coordinates
        bottomLeftCorner = mainCamera.ViewportToWorldPoint(new Vector3(0, 0, mainCamera.nearClipPlane));
        bottomLeftCorner.z = 0;  // Ensure z = 0 since we are in 2D

        // Calculate the top-right corner of the screen in world coordinates
        topRightCorner = mainCamera.ViewportToWorldPoint(new Vector3(1, 1, mainCamera.nearClipPlane));
        topRightCorner.z = 0;  // Ensure z = 0 for 2D

        // Set the base minimum Y position (flat ground start) using the absolute value of descentRate
        minYPosition = bottomLeftCorner.y + Mathf.Abs(descentRate);

        // Add the public heightOffset to allow manual adjustment
        minYPosition += heightOffset;

        // Set the valley (low) position lower than the flat ground by descentRate
        lowYPosition = minYPosition - descentRate;

        // Calculate the width of the terrain based on the camera's view
        terrainWidth = topRightCorner.x - bottomLeftCorner.x;
    }
    private void CreateTerrainProfile()
    {
        valleyLengthPercent = Mathf.Clamp(valleyLengthPercent, .05f, .7f);

        // Number of points for each part of the terrain
        int flatStartLength = numPoints / 4;  // Flat start
        int slopePoints = slopePointDensity * 2;  // Points on descent and ascent slopes
        int valleyLengthPoints = Mathf.RoundToInt(numPoints * valleyLengthPercent);  // Valley flat length

        // Total number of points including extra points for slopes
        int totalPoints = flatStartLength + slopePoints + valleyLengthPoints + flatStartLength;
        lineRenderer.positionCount = totalPoints;

        Vector3[] positions = new Vector3[totalPoints];

        int index = 0;
        float terrainXSpacing = terrainWidth / (totalPoints - 1);

        // Flat ground before the valley descent
        for (int i = 0; i < flatStartLength; i++, index++)
        {
            float x = bottomLeftCorner.x + index * terrainXSpacing;
            positions[index] = new Vector3(x, minYPosition, 0);
        }

        // Descent slope into the valley
        for (int i = 0; i < slopePointDensity; i++, index++)
        {
            float x = bottomLeftCorner.x + index * terrainXSpacing;
            float t = (float)i / (slopePointDensity - 1);  // Normalized interpolation value
            float y = Mathf.Lerp(minYPosition, lowYPosition, t);
            positions[index] = new Vector3(x, y, 0);
            minX = x;
        }

        // Flat valley section
        for (int i = 0; i < valleyLengthPoints; i++, index++)
        {
            float x = bottomLeftCorner.x + index * terrainXSpacing;
            positions[index] = new Vector3(x, lowYPosition, 0);
            minY = lowYPosition;
        }

        // Ascent slope out of the valley
        for (int i = 0; i < slopePointDensity; i++, index++)
        {
            float x = bottomLeftCorner.x + index * terrainXSpacing;
            float t = (float)i / (slopePointDensity - 1);  // Normalized interpolation value
            float y = Mathf.Lerp(lowYPosition, minYPosition, t);
            positions[index] = new Vector3(x, y, 0);
            maxX = x;
        }

        // Flat ground after the valley
        for (int i = 0; i < flatStartLength && index < totalPoints; i++, index++)
        {
            float x = bottomLeftCorner.x + index * terrainXSpacing;
            positions[index] = new Vector3(x, minYPosition, 0);
            maxY = minYPosition;
        }

        lineRenderer.SetPositions(positions);
    }

    public float GetTerrainHeightAtPosition(float x)
    {
        Vector3[] positions = new Vector3[lineRenderer.positionCount];
        lineRenderer.GetPositions(positions);

        // Iterate over the line renderer positions to find the segment that contains the given x
        for (int i = 0; i < positions.Length - 1; i++)
        {
            if (x >= positions[i].x && x <= positions[i + 1].x)
            {
                // Linear interpolation to get the Y position between the two points
                float t = (x - positions[i].x) / (positions[i + 1].x - positions[i].x);
                return Mathf.Lerp(positions[i].y, positions[i + 1].y, t);
            }
        }

        // If x is out of bounds, return the y of the first or last point
        return x < positions[0].x ? positions[0].y : positions[positions.Length - 1].y;
    }
  

    public Vector3 GetTerrainPositionAtPoint(float x)
    {
        Vector3[] terrainPoints = new Vector3[lineRenderer.positionCount];
        lineRenderer.GetPositions(terrainPoints);

        // Loop through the terrain points to find the segment that contains the given x position
        for (int i = 0; i < terrainPoints.Length - 1; i++)
        {
            if (x >= terrainPoints[i].x && x <= terrainPoints[i + 1].x)
            {
                // Linear interpolation to get the y value at the given x position
                float t = (x - terrainPoints[i].x) / (terrainPoints[i + 1].x - terrainPoints[i].x);
                float y = Mathf.Lerp(terrainPoints[i].y, terrainPoints[i + 1].y, t);

                return new Vector3(x, y, 0);  // Return the calculated terrain height
            }
        }

        // If x is out of bounds, return a default value (e.g., ground level)
        return new Vector3(x, terrainPoints[^1].y, 0); // Adjust the fallback as needed
    }
    public Vector3 GetClosestTerrainPoint(Vector3 position)
    {
        // Get all points from the terrain's line renderer or mesh
        Vector3[] terrainPoints = new Vector3[lineRenderer.positionCount];
        lineRenderer.GetPositions(terrainPoints);

        // Find the closest point on the terrain to the balloon
        Vector3 closestPoint = terrainPoints[0];
        float closestDistance = Vector3.Distance(position, closestPoint);

        for (int i = 1; i < terrainPoints.Length; i++)
        {
            float distance = Vector3.Distance(position, terrainPoints[i]);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestPoint = terrainPoints[i];
            }
        }

        return closestPoint;
    }
    // Method to return the ground position (based on the ground mesh) at a given X coordinate

    public Vector3 GetClosestPointOnGroundMesh(Vector3 harpoonPosition)
    {
        Mesh groundMesh = groundMeshObject.GetComponent<MeshFilter>().mesh;
        Vector3[] vertices = groundMesh.vertices;

        // Transform the vertices to world space
        Vector3 closestPoint = Vector3.zero;
        float closestDistance = Mathf.Infinity;

        foreach (var vertex in vertices)
        {
            // Convert local vertex to world position
            Vector3 worldVertex = groundMeshObject.transform.TransformPoint(vertex);

            // Calculate the distance between the harpoon and this vertex
            float distance = Vector3.Distance(harpoonPosition, worldVertex);

            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestPoint = worldVertex;
            }
        }

        return closestPoint;
    }

    public Vector3 GetGroundPositionAtPoint(float x)
    {
        Vector3[] terrainPoints = new Vector3[lineRenderer.positionCount];
        lineRenderer.GetPositions(terrainPoints);

        // Iterate over the terrain points to find the segment that contains the given x
        for (int i = 0; i < terrainPoints.Length - 1; i++)
        {
            if (x >= terrainPoints[i].x && x <= terrainPoints[i + 1].x)
            {
                // Interpolate the Y position between the two points
                float t = (x - terrainPoints[i].x) / (terrainPoints[i + 1].x - terrainPoints[i].x);
                float y = Mathf.Lerp(terrainPoints[i].y, terrainPoints[i + 1].y, t);

                return new Vector3(x, y, 0);  // Return the interpolated ground position
            }
        }

        // If x is out of bounds, return the nearest terrain point's Y position
        return x < terrainPoints[0].x ? terrainPoints[0] : terrainPoints[terrainPoints.Length - 1];
    }


  
    // Generate the terrain profile, dirt mesh, and ground mesh
    private void GenerateTerrain()
    {
        // Clear the existing line, dirt, and ground meshes
        ClearTerrain();

        // Generate the new terrain profile using the updated values
        CreateTerrainProfile();

        // Create the dirt mesh beneath the terrain
        GenerateDirtMesh();

        // Create the ground mesh along the terrain
        GenerateGroundMesh();
    }

    // Generate the terrain profile
  


    // Generate a dirt mesh beneath the terrain
    private void GenerateDirtMesh()
    {
        if (dirtMeshObject != null)
        {
            Destroy(dirtMeshObject); // Destroy any previous dirt mesh objects
        }

        dirtMeshObject = new GameObject("DirtMesh");
        dirtMeshObject.transform.position = Vector3.zero;

        MeshFilter meshFilter = dirtMeshObject.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer = dirtMeshObject.AddComponent<MeshRenderer>();

        meshRenderer.material = dirtMaterial;

        dirtMesh = new Mesh();
        meshFilter.mesh = dirtMesh;

        Vector3[] terrainPoints = new Vector3[lineRenderer.positionCount];
        lineRenderer.GetPositions(terrainPoints);

        Vector3[] vertices = new Vector3[terrainPoints.Length * 2];
        for (int i = 0; i < terrainPoints.Length; i++)
        {
            vertices[i] = terrainPoints[i];
            vertices[i + terrainPoints.Length] = new Vector3(terrainPoints[i].x, bottomLeftCorner.y, 0);
        }

        int[] triangles = new int[(terrainPoints.Length - 1) * 6];
        for (int i = 0; i < terrainPoints.Length - 1; i++)
        {
            int topLeft = i;
            int topRight = i + 1;
            int bottomLeft = i + terrainPoints.Length;
            int bottomRight = i + terrainPoints.Length + 1;

            triangles[i * 6] = topLeft;
            triangles[i * 6 + 1] = bottomLeft;
            triangles[i * 6 + 2] = bottomRight;

            triangles[i * 6 + 3] = topLeft;
            triangles[i * 6 + 4] = bottomRight;
            triangles[i * 6 + 5] = topRight;
        }

        dirtMesh.vertices = vertices;
        dirtMesh.triangles = triangles;
        dirtMesh.RecalculateNormals();
    }

    // Generate a ground mesh that runs along the top of the terrain profile
    private void GenerateGroundMesh()
    {
        if (groundMeshObject != null)
        {
            Destroy(groundMeshObject); // Destroy any previous ground mesh objects
        }

        groundMeshObject = new GameObject("GroundMesh");
        groundMeshObject.transform.position = Vector3.zero;

        MeshFilter meshFilter = groundMeshObject.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer = groundMeshObject.AddComponent<MeshRenderer>();

        meshRenderer.material = groundMaterial;

        groundMesh = new Mesh();
        meshFilter.mesh = groundMesh;

        Vector3[] terrainPoints = new Vector3[lineRenderer.positionCount];
        lineRenderer.GetPositions(terrainPoints);

        // Create vertices for the ground mesh (runs along the top of the terrain and scales dynamically)
        Vector3[] vertices = new Vector3[terrainPoints.Length * 2];

        for (int i = 0; i < terrainPoints.Length; i++)
        {
            // Calculate x-offset based on the change in x distance (to avoid disjointed sections)
            float xOffset = 0f;

            if (i > 0 && i < terrainPoints.Length - 1)
            {
                // Calculate slope (difference in elevation)

                float deltaY = terrainPoints[i].y - terrainPoints[i - 1].y;
                if (deltaY == 0)
                {
                    deltaY = terrainPoints[i + 1].y - terrainPoints[i].y;
                }
                // Determine if the slope is steep enough to create a sharp 90-degree corner
                if (Mathf.Abs(deltaY) > sharpAngleThreshold)
                {
                    // If it's a sharp descent, shift forward; if it's a sharp ascent, shift backward
                    xOffset = deltaY < 0 ? groundXOffsetScale : -groundXOffsetScale;
                }
            }

            // Apply both X and Y offsets
            vertices[i] = new Vector3(terrainPoints[i].x + xOffset, terrainPoints[i].y + groundVerticalOffset, 0);

            // Bottom vertices follow the terrain profile
            vertices[i + terrainPoints.Length] = terrainPoints[i];
        }

        // Create triangles for the ground mesh
        int[] triangles = new int[(terrainPoints.Length - 1) * 6];
        for (int i = 0; i < terrainPoints.Length - 1; i++)
        {
            int topLeft = i;
            int topRight = i + 1;
            int bottomLeft = i + terrainPoints.Length;
            int bottomRight = i + terrainPoints.Length + 1;

            // First triangle (top-left, bottom-left, bottom-right)
            triangles[i * 6] = topLeft;
            triangles[i * 6 + 1] = bottomLeft;
            triangles[i * 6 + 2] = bottomRight;

            // Second triangle (top-left, bottom-right, top-right)
            triangles[i * 6 + 3] = topLeft;
            triangles[i * 6 + 4] = bottomRight;
            triangles[i * 6 + 5] = topRight;
        }

        groundMesh.vertices = vertices;
        groundMesh.triangles = triangles;
        groundMesh.RecalculateNormals();
    }

    // Method to spawn the player prefab (castle with launcher) at the bottom-left of the screen
    private void SpawnPlayer()
    {
        if (playerPrefab == null)
        {
            Debug.LogError("PlayerPrefab is not assigned in the Inspector!");
            return;
        }

        Vector3 bottomLeft = mainCamera.ViewportToWorldPoint(new Vector3(0, 0, mainCamera.nearClipPlane));

        float prefabWidth = playerPrefab.GetComponentInChildren<SpriteRenderer>().bounds.size.x;
        float prefabHeight = playerPrefab.GetComponentInChildren<SpriteRenderer>().bounds.size.y;
        Vector3 spawnPosition = new Vector3(bottomLeft.x + prefabWidth / 2, minYPosition + prefabHeight / 2, 0);

        instantiatedPlayer = Instantiate(playerPrefab, spawnPosition, Quaternion.identity);
    }

    // Method to reposition the player prefab on the terrain after regenerating the terrain
    private void RepositionPlayerOnTerrain()
    {
        if (instantiatedPlayer == null)
        {
            Debug.LogError("PlayerPrefab is not instantiated yet.");
            return;
        }

        float prefabWidth = instantiatedPlayer.GetComponentInChildren<SpriteRenderer>().bounds.size.x;
        float prefabHeight = instantiatedPlayer.GetComponentInChildren<SpriteRenderer>().bounds.size.y;
        Vector3 bottomLeft = mainCamera.ViewportToWorldPoint(new Vector3(0, 0, mainCamera.nearClipPlane));
        Vector3 newPosition = new Vector3(bottomLeft.x + prefabWidth / 2, minYPosition + prefabHeight / 2, 0);

        instantiatedPlayer.transform.position = newPosition;
    }

    // Clear existing terrain (line, ground, and dirt meshes)
    private void ClearTerrain()
    {
        lineRenderer.positionCount = 0;

        if (dirtMeshObject != null)
        {
            Destroy(dirtMeshObject);
        }

        if (groundMeshObject != null)
        {
            Destroy(groundMeshObject);
        }
    }

    // Any additional functions or logic for SceneManager can go here
}

