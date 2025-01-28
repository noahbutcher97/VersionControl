using UnityEngine;
using System.Collections.Generic;

public class SceneManager : MonoBehaviour
{
    // --------------------------------------------------
    [Header("References")]
    [Tooltip("Main Camera used to view the scene.")]
    public Camera mainCamera;

    [Tooltip("Optional Line Renderer for debugging the brown line.")]
    public LineRenderer lineRenderer;

    [Tooltip("Material for the brown dirt mesh.")]
    public Material dirtMaterial;

    [Tooltip("Material for the green ground mesh (actual collision surface).")]
    public Material groundMaterial;

    [Tooltip("Prefab for the tower/launcher, used to measure tower width.")]
    public GameObject playerPrefab;

    [Header("Terrain Generation (Perlin)")]
    [Tooltip("Number of points defining the terrain line.")]
    public int terrainResolution = 20;

    [Tooltip("Horizontal scale for Perlin noise.")]
    public float noiseScale = 0.3f;

    [Tooltip("Vertical amplitude for terrain peaks.")]
    public float amplitude = 3f;

    [Tooltip("Base ground level on the left side (where the tower is).")]
    public float baseY = 0f;

    [Tooltip("Fraction of total width that remains flat on the left side for the tower.")]
    public float percentageTowerXPadding = 0.1f;

    [Header("Meshes and Offsets")]
    [Tooltip("How far above the brown line to place the green ground mesh.")]
    public float groundVerticalOffset = 1f;

    [Tooltip("If slope difference > this, shift the top mesh horizontally by groundXOffsetScale.")]
    public float sharpAngleThreshold = 1f;

    [Tooltip("Horizontal shift for steep slopes in the top mesh.")]
    public float groundXOffsetScale = 0.2f;

    [Header("Camera Controls")]
    [Tooltip("If > 0, we fix the camera's orthographicSize to this.\nIf == 0, we use baseCameraSize and let the terrain fill that range.")]
    public float forcedCameraSize = 0f;

    [Tooltip("If forcedCameraSize=0, we define the base camera size = baseCameraSize.\nWe then scale the environment to fill that range.\nIf forcedCameraSize>0, we clamp and define the environment by that range.")]
    public float baseCameraSize = 5f;

    [Tooltip("Extra space used in auto-fitting or forced size to avoid clipping edges.")]
    public float somePadding = 1f;

    [Header("Obstacle Generation")]
    [Tooltip("Prefab for obstacles placed randomly on the terrain (optional).")]
    public GameObject obstaclePrefab;

    [Tooltip("How many obstacles to spawn each generation.")]
    public int obstacleCount = 0;

    [Header("Runtime Controls")]
    [Tooltip("Fallback width for the tower if we cannot measure the sprite. Prevents compile errors if referenced.")]
    public float fallbackTowerWidth = 2f;

    [Tooltip("If true, pressing 'R' regenerates the terrain at runtime.")]
    public bool allowRegenerateWithKey = true;

    [Tooltip("Max slope for slope-based 'valley' detection (for balloon spawns).")]
    public float maxValleySlope = 0.2f;

    [Tooltip("Extra horizontal padding so balloons don't spawn near the tower region.")]
    public float towerClearPadding = 2f;

    [Tooltip("Minimum vertical offset above the terrain for balloon spawns.")]
    public float minGroundPadding = 0.5f;

    [Header("Seeds & Random")]
    [Tooltip("If true, we ignore the 'seed' and pick a random seed each time.")]
    public bool useRandomSeed = false;
    [Tooltip("Random range Min for seed if useRandomSeed=true.")]
    public int seedMin = -200;
    [Tooltip("Random range Max for seed if useRandomSeed=true.")]
    public int seedMax = 200;
    [Tooltip("If useRandomSeed=false, we use this integer for Perlin offset.")]
    public int seed = 0;

    // --------------------------------------------------
    // Internal references
    // --------------------------------------------------
    private GameObject environmentRoot;
    private Mesh dirtMesh, groundMesh;
    private GameObject dirtMeshObject, groundMeshObject;
    private GameObject instantiatedPlayer; // Holds the reference to the tower instance


    // The brown line + green line
    private Vector3[] terrainPoints;
    private Vector3[] topTerrainPoints;

    // Where the flat tower region ends
    private float towerRegionEndX = 0f;

    // Computed bounding
    public float minX, maxX, minY, maxY;

    // Some minimum thresholds to ensure at least 2 peaks, enough space for tower, etc.
    private const float MIN_CAMERA_SIZE = 3f;  
    private const int MIN_TERRAIN_RESOLUTION = 6; 

    // --- NEW: Store a reference to the tower so it won't be toggled ---
    private GameObject towerInstance;

    void Start()
    {
        if (!mainCamera) mainCamera = Camera.main;
        GenerateEnvironment();
    }

    void Update()
    {
        if (allowRegenerateWithKey && Input.GetKeyDown(KeyCode.R))
        {
            GenerateEnvironment();
        }
    }

    /// <summary>
    /// Public method to regenerate the entire environment (brown + green mesh),
    /// obstacles, and reposition the tower if needed.
    /// We do NOT destroy balloons or toggle the tower (just re-place it).
    /// </summary>
    public void GenerateEnvironment()
    {
        // 1) Clean up old environment
        if (environmentRoot) Destroy(environmentRoot);
        environmentRoot = new GameObject("EnvironmentRoot");

        // 2) Clamp forcedCameraSize and terrainResolution
        if (forcedCameraSize > 0f && forcedCameraSize < MIN_CAMERA_SIZE)
            forcedCameraSize = MIN_CAMERA_SIZE;

        if (terrainResolution < MIN_TERRAIN_RESOLUTION)
            terrainResolution = MIN_TERRAIN_RESOLUTION;

        // 3) Possibly randomize or set the seed
        if (useRandomSeed)
        {
            seed = Random.Range(-200, 200);
        }

        // 4) Decide local coordinate extents based on forced or base camera size
        float halfHeight, halfWidth;
        if (forcedCameraSize > 0f)
        {
            mainCamera.orthographicSize = forcedCameraSize;
            halfHeight = forcedCameraSize;
            halfWidth = forcedCameraSize * mainCamera.aspect;
        }
        else
        {
            mainCamera.orthographicSize = baseCameraSize;
            halfHeight = baseCameraSize;
            halfWidth = baseCameraSize * mainCamera.aspect;
        }
        float effectiveCameraSize = forcedCameraSize > 0 ? forcedCameraSize : baseCameraSize;
        if (effectiveCameraSize <= 0f)
        {
            effectiveCameraSize = 5f; // Default fallback value
        }
        mainCamera.orthographicSize = effectiveCameraSize;

        // local coords => [-halfWidth..+halfWidth, -halfHeight..+halfHeight]
        Vector3 bottomLeft = new Vector3(-halfWidth, -halfHeight, 0f);
        Vector3 topRight = new Vector3(halfWidth, halfHeight, 0f);

        // 5) Build the brown line in local coords
        terrainPoints = CreateBrownLine(bottomLeft, topRight);

        // 6) Build the dirt mesh
        BuildDirtMesh(terrainPoints, bottomLeft);

        // 7) Build the green mesh
        topTerrainPoints = CreateTopTerrainPoints(terrainPoints);
        BuildGreenGroundMesh(terrainPoints, topTerrainPoints);

        // 8) Spawn or reposition tower
        SpawnOrRepositionPlayer(); // << changed to fix toggling

        // 9) Spawn obstacles
        SpawnObstacles();

        // 10) Compute bounding
        ComputeLocalBounding();

        // 11) Scale environmentRoot so it fits exactly within the camera's view
        ScaleEnvironmentToCameraBounds();

        // 12) Move environmentRoot so center is at camera center
        environmentRoot.transform.position = Vector3.zero;
        Vector3 camPos = mainCamera.transform.position;
        camPos.x = (minX + maxX) / 2f;
        camPos.y = (minY + maxY) / 2f;
        mainCamera.transform.position = camPos;
    }

    // --------------------------------------------------
    // Building terrain lines
    // --------------------------------------------------

    private Vector3[] CreateBrownLine(Vector3 bottomLeft, Vector3 topRight)
    {
        Vector3[] points = new Vector3[terrainResolution];

        float totalWidth = topRight.x - bottomLeft.x;

        // measure tower width if possible
        float towerW = MeasureTowerWidth();
        // clamp fraction for tower region
        float neededFrac = (towerW / totalWidth);
        if (neededFrac > 1f) neededFrac = 1f;
        percentageTowerXPadding = Mathf.Max(percentageTowerXPadding, neededFrac);

        float xStep = totalWidth / (terrainResolution - 1);

        towerRegionEndX = bottomLeft.x + totalWidth * percentageTowerXPadding;

        for (int i = 0; i < terrainResolution; i++)
        {
            float x = bottomLeft.x + i * xStep;
            float y;
            if (x <= towerRegionEndX)
            {
                // partial flat region
                y = baseY;
            }
            else
            {
                // offset by seed
                float offsetX = (x + seed * 12347f) * noiseScale;
                float noiseVal = Mathf.PerlinNoise(offsetX, 0f) - 0.5f;
                y = baseY + noiseVal * amplitude;
            }
            points[i] = new Vector3(x, y, 0f);
        }

        // If a lineRenderer was assigned, show it
        if (lineRenderer)
        {
            lineRenderer.positionCount = points.Length;
            lineRenderer.SetPositions(points);
        }

        return points;
    }

    private float MeasureTowerWidth()
    {
        if (playerPrefab)
        {
            var sr = playerPrefab.GetComponentInChildren<SpriteRenderer>();
            if (sr)
            {
                return sr.bounds.size.x;
            }
        }
        return fallbackTowerWidth;
    }

    private void BuildDirtMesh(Vector3[] brownPoints, Vector3 bottomLeft)
    {
        dirtMeshObject = new GameObject("DirtMesh");
        dirtMeshObject.transform.SetParent(environmentRoot.transform, false);

        var mf = dirtMeshObject.AddComponent<MeshFilter>();
        var mr = dirtMeshObject.AddComponent<MeshRenderer>();
        mr.material = dirtMaterial;

        dirtMesh = new Mesh();
        mf.mesh = dirtMesh;

        int n = brownPoints.Length;
        Vector3[] verts = new Vector3[n * 2];

        for (int i = 0; i < n; i++)
        {
            verts[i] = brownPoints[i];
            // the bottom row is near bottomLeft.y - somePadding
            verts[i + n] = new Vector3(
                brownPoints[i].x,
                bottomLeft.y - somePadding,
                0f
            );
        }

        int[] triangles = new int[(n - 1) * 6];
        int t = 0;
        for (int i = 0; i < n - 1; i++)
        {
            int tl = i;
            int tr = i + 1;
            int bl = i + n;
            int br = i + n + 1;

            triangles[t++] = tl;
            triangles[t++] = bl;
            triangles[t++] = br;

            triangles[t++] = tl;
            triangles[t++] = br;
            triangles[t++] = tr;
        }

        dirtMesh.vertices = verts;
        dirtMesh.triangles = triangles;
        dirtMesh.RecalculateNormals();
    }

    private Vector3[] CreateTopTerrainPoints(Vector3[] brownPoints)
    {
        int n = brownPoints.Length;
        Vector3[] green = new Vector3[n];

        for (int i = 0; i < n; i++)
        {
            float xShift = 0f;
            if (i > 0 && i < n - 1)
            {
                float dy = brownPoints[i].y - brownPoints[i - 1].y;
                if (Mathf.Abs(dy) > sharpAngleThreshold)
                {
                    xShift = (dy < 0) ? groundXOffsetScale : -groundXOffsetScale;
                }
            }
            float topX = brownPoints[i].x + xShift;
            float topY = brownPoints[i].y + groundVerticalOffset;
            green[i] = new Vector3(topX, topY, 0f);
        }
        return green;
    }

    private void BuildGreenGroundMesh(Vector3[] brownPoints, Vector3[] greenPoints)
    {
        groundMeshObject = new GameObject("GroundMesh");
        groundMeshObject.transform.SetParent(environmentRoot.transform, false);

        var mf = groundMeshObject.AddComponent<MeshFilter>();
        var mr = groundMeshObject.AddComponent<MeshRenderer>();
        mr.material = groundMaterial;

        groundMesh = new Mesh();
        mf.mesh = groundMesh;

        int n = brownPoints.Length;
        Vector3[] verts = new Vector3[n * 2];

        for (int i = 0; i < n; i++)
        {
            verts[i] = greenPoints[i];
            verts[i + n] = brownPoints[i];
        }

        int[] triangles = new int[(n - 1) * 6];
        int tt = 0;
        for (int i = 0; i < n - 1; i++)
        {
            int tl = i;
            int tr = i + 1;
            int bl = i + n;
            int br = i + n + 1;

            triangles[tt++] = tl;
            triangles[tt++] = bl;
            triangles[tt++] = br;

            triangles[tt++] = tl;
            triangles[tt++] = br;
            triangles[tt++] = tr;
        }

        groundMesh.vertices = verts;
        groundMesh.triangles = triangles;
        groundMesh.RecalculateNormals();
    }

    private void SpawnOrRepositionPlayer()
    {
        if (!playerPrefab || topTerrainPoints == null || topTerrainPoints.Length == 0) return;

        //  --- The fix: we DO NOT parent the tower to environmentRoot. 
        //  If it already exists, reuse it. Otherwise spawn once in world coords.

        if (!instantiatedPlayer)
        {
            // see if a "Tower" object already in scene
            var existingTower = GameObject.Find("Tower");
            if (existingTower)
            {
                instantiatedPlayer = existingTower;
            }
            else
            {
                // spawn in world coords, not under environmentRoot
                instantiatedPlayer = Instantiate(playerPrefab);
                instantiatedPlayer.name = "Tower";
            }
        }

        // Now we just place it in the correct world position:
        Vector3 leftPoint = topTerrainPoints[0];

        // offset if we have a sprite
        var sr = instantiatedPlayer.GetComponentInChildren<SpriteRenderer>();
        if (sr)
        {
            float tw = sr.bounds.size.x;
            float th = sr.bounds.size.y;
            Vector3 pos = new Vector3(
                leftPoint.x + tw * 0.5f,
                leftPoint.y + th * 0.5f,
                0f
            );
            instantiatedPlayer.transform.position = pos;
        }
        else
        {
            instantiatedPlayer.transform.position = leftPoint;
        }
    }

    private void SpawnObstacles()
    {
        if (!obstaclePrefab || obstacleCount <= 0) return;

        // remove old obstacles in environmentRoot
        var oldObs = GameObject.FindGameObjectsWithTag("Obstacle");
        foreach (var o in oldObs)
        {
            if (o.transform.parent == environmentRoot.transform)
            {
                Destroy(o);
            }
        }

        for (int i = 0; i < obstacleCount; i++)
        {
            float rx = Random.Range(minX, maxX);
            float gy = GetTerrainHeightAtPosition(rx);
            Vector3 spawnPos = new Vector3(rx, gy, 0f);

            // obstacles are still placed under environmentRoot
            GameObject obs = Instantiate(obstaclePrefab, environmentRoot.transform);
            obs.transform.localPosition = spawnPos;
            obs.tag = "Obstacle";
        }
    }

    private void ComputeLocalBounding()
    {
        if (topTerrainPoints == null || topTerrainPoints.Length == 0) return;

        float locMinX = float.MaxValue;
        float locMaxX = float.MinValue;
        float locMinY = float.MaxValue;
        float locMaxY = float.MinValue;

        foreach (var p in topTerrainPoints)
        {
            if (p.x < locMinX) locMinX = p.x;
            if (p.x > locMaxX) locMaxX = p.x;
            if (p.y < locMinY) locMinY = p.y;
            if (p.y > locMaxY) locMaxY = p.y;
        }

        minX = locMinX; maxX = locMaxX;
        minY = locMinY; maxY = locMaxY;
    }

    private void ClearOldTerrain()
    {
        if (lineRenderer) lineRenderer.positionCount = 0;

        if (dirtMeshObject)   Destroy(dirtMeshObject);
        if (groundMeshObject) Destroy(groundMeshObject);

        // remove obstacles from environmentRoot
        var oldObs = GameObject.FindGameObjectsWithTag("Obstacle");
        foreach (var o in oldObs)
        {
            if (o.transform.parent == environmentRoot?.transform)
                Destroy(o);
        }

        if (environmentRoot) Destroy(environmentRoot);
    }

    private void ScaleEnvironmentToCameraBounds()
    {
        float halfHeight = mainCamera.orthographicSize;
        float halfWidth = halfHeight * mainCamera.aspect;

        float envWidth = (maxX - minX);
        float envHeight = (maxY - minY);
        if (envWidth <= 0 || envHeight <= 0) return;

        float scaleX = (2f * halfWidth) / envWidth;
        float scaleY = (2f * halfHeight) / envHeight;
        float scale = Mathf.Min(scaleX, scaleY);

        environmentRoot.transform.localScale = new Vector3(scale, scale, 1f);
    }

    // --------------------------------------------------
    // Balloon Collisions
    // --------------------------------------------------

    public float GetTerrainHeightAtPosition(float x)
    {
        if (topTerrainPoints == null || topTerrainPoints.Length == 0)
            return baseY;

        if (x <= topTerrainPoints[0].x)
            return topTerrainPoints[0].y;
        if (x >= topTerrainPoints[topTerrainPoints.Length - 1].x)
            return topTerrainPoints[topTerrainPoints.Length - 1].y;

        for (int i = 0; i < topTerrainPoints.Length - 1; i++)
        {
            float xA = topTerrainPoints[i].x;
            float xB = topTerrainPoints[i + 1].x;
            if (x >= xA && x <= xB)
            {
                float t = (x - xA) / (xB - xA);
                float yA = topTerrainPoints[i].y;
                float yB = topTerrainPoints[i + 1].y;
                return Mathf.Lerp(yA, yB, t);
            }
        }
        return topTerrainPoints[topTerrainPoints.Length - 1].y;
    }

    public bool IsValleyArea(float x, float deltaX = 0.1f)
    {
        float yC = GetTerrainHeightAtPosition(x);
        float yL = GetTerrainHeightAtPosition(x - deltaX);
        float yR = GetTerrainHeightAtPosition(x + deltaX);

        float sL = Mathf.Abs(yC - yL) / deltaX;
        float sR = Mathf.Abs(yR - yC) / deltaX;
        float sMax = Mathf.Max(sL, sR);

        return (sMax <= maxValleySlope);
    }

    public Vector3 GetRandomValleySpawnPosition(float maxSpawnY, int maxAttempts = 50)
    {
        float spawnXMin = towerRegionEndX + towerClearPadding;
        if (spawnXMin > maxX) spawnXMin = maxX - 0.01f;

        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {

            float randX = Random.Range(spawnXMin, maxX);
            if (!IsValleyArea(randX)) continue;

            float groundY = GetTerrainHeightAtPosition(randX);
            float minY = groundY + minGroundPadding;
            if (minY >= maxSpawnY) continue;

            float randY = Random.Range(minY, maxSpawnY);
            if (randX < minX || randX > maxX || randY < minY || randY > maxY) continue; // Skip invalid positions
            return new Vector3(randX, randY, 0f);
        }
        return Vector3.zero;
    }

    public Vector3 GetGroundPositionAtPoint(float x)
    {
        float y = GetTerrainHeightAtPosition(x);
        return new Vector3(x, y, 0f);
    }

    public Vector3 GetTerrainPositionAtPoint(float x)
    {
        return GetGroundPositionAtPoint(x);
    }

    public Vector3 GetClosestTerrainPoint(Vector3 pos)
    {
        if (topTerrainPoints == null || topTerrainPoints.Length == 0)
            return pos;

        Vector3 closest = topTerrainPoints[0];
        float dist = Vector3.Distance(pos, closest);
        for (int i = 1; i < topTerrainPoints.Length; i++)
        {
            float d2 = Vector3.Distance(pos, topTerrainPoints[i]);
            if (d2 < dist)
            {
                dist = d2;
                closest = topTerrainPoints[i];
            }
        }
        return closest;
    }
}
