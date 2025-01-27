using UnityEngine;
using UnityEditor;

[ExecuteInEditMode]
public class PersistentGridSpawner : MonoBehaviour
{
    // Grid properties
    public int gridWidth = 10;        // Number of columns
    public int gridHeight = 10;       // Number of rows

    // Padding around the grid (in world units)
    public float paddingX = 0.5f;
    public float paddingY = 0.5f;

    // The prefab or sprite to spawn in the grid
    public GameObject spritePrefab;

    // Variables for scaling to screen size
    public float playAreaWidthPercent = 0.9f;  // Use 90% of the screen width
    public float playAreaHeightPercent = 0.9f; // Use 90% of the screen height

    private bool gridDrawn = false;  // Flag for manual Play Mode control

    // Called when a value is changed in the Inspector
    private void OnValidate()
    {
        if (!Application.isPlaying && spritePrefab != null)
        {
            // Automatically update the grid in Edit Mode when variables change
            ClearExistingGrid(true);    // Clear the previous grid before generating a new one
            GenerateGrid();
        }
    }

    private void GenerateGrid()
    {
        if (spritePrefab == null) return;

        Vector2 screenSize = GetScreenSizeInWorldUnits();
        float playAreaWidth = screenSize.x * playAreaWidthPercent - 2 * paddingX;
        float playAreaHeight = screenSize.y * playAreaHeightPercent - 2 * paddingY;

        // Calculate the size of each grid cell
        float cellWidth = playAreaWidth / gridWidth;
        float cellHeight = playAreaHeight / gridHeight;

        // Get sprite size for scaling
        SpriteRenderer spriteRenderer = spritePrefab.GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            Debug.LogError("SpritePrefab does not have a SpriteRenderer component!");
            return;
        }

        // Calculate the scale factor to fit the sprite within the grid cell
        Vector3 spriteSize = spriteRenderer.bounds.size;
        float scaleX = cellWidth / spriteSize.x;
        float scaleY = cellHeight / spriteSize.y;

        // Offset to center the grid on the screen
        Vector2 gridOffset = new Vector2(
            -playAreaWidth / 2 + cellWidth / 2 + paddingX,
            -playAreaHeight / 2 + cellHeight / 2 + paddingY
        );

        // Loop through and create the grid elements
        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                Vector2 spawnPosition = new Vector2(
                    x * cellWidth + gridOffset.x,
                    y * cellHeight + gridOffset.y
                );

                // Instantiate the spritePrefab at the calculated position
                GameObject newSprite = Instantiate(spritePrefab, spawnPosition, Quaternion.identity);

                // Scale the sprite to fit in the cell
                newSprite.transform.localScale = new Vector3(scaleX, scaleY, 1);

                // Parent to keep the hierarchy clean
                newSprite.transform.parent = transform;

                // Name the object for easier identification
                newSprite.name = "Grid Element (" + x + ", " + y + ")";
            }
        }

        gridDrawn = true; // Set the flag to true after generating the grid
    }

    private void ClearExistingGrid(bool immediate = true)
    {
        // Remove all child objects (grid elements)
        int childCount = transform.childCount;
        if (childCount == 0)
        {
            gridDrawn = false; // If no children, grid is not drawn
            return;
        }

        for (int i = childCount - 1; i >= 0; i--)
        {
            if (immediate && !Application.isPlaying)
            {
                // Only use DestroyImmediate in Edit Mode and when not playing
                DestroyImmediate(transform.GetChild(i).gameObject);
            }
            else
            {
                // Use Destroy during Play Mode
                Destroy(transform.GetChild(i).gameObject);
            }
        }

        gridDrawn = false; // Reset the flag after clearing
        EditorUtility.SetDirty(this); // Mark the scene as dirty to ensure changes are saved
    }

    // Calculate the screen size in world units based on the main camera
    Vector2 GetScreenSizeInWorldUnits()
    {
        Camera mainCamera = Camera.main;

        if (mainCamera == null)
        {
            Debug.LogError("Main camera is not assigned!");
            return Vector2.zero;
        }

        // Get the world position of the screen's top-right corner
        Vector3 screenTopRight = mainCamera.ScreenToWorldPoint(new Vector3(Screen.width, Screen.height, mainCamera.nearClipPlane));

        // Get the world position of the screen's bottom-left corner
        Vector3 screenBottomLeft = mainCamera.ScreenToWorldPoint(new Vector3(0, 0, mainCamera.nearClipPlane));

        // Calculate the screen width and height in world units
        float screenWidth = screenTopRight.x - screenBottomLeft.x;
        float screenHeight = screenTopRight.y - screenBottomLeft.y;

        return new Vector2(screenWidth, screenHeight);
    }

    // Public methods for manual control from the Editor
    public void GenerateGridInEditor()
    {
        ClearExistingGrid(true); // Always clear before generating
        GenerateGrid();
    }

    public void ClearGridInEditor()
    {
        ClearExistingGrid(true);
    }
}