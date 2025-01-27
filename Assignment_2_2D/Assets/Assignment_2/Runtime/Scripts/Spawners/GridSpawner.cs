using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GridSpawner : MonoBehaviour
{
    // Grid dimensions (number of columns and rows)
    public int gridWidth = 10;    // Number of columns
    public int gridHeight = 10;   // Number of rows

    // Padding around the edges of the screen (in world units)
    public float padding = 0.5f;

    // The prefab or sprite to spawn in the grid
    public GameObject spritePrefab;

    // The percentage of the screen to use for the play area (0 to 1)
    public float playAreaWidthPercent = 0.9f;  // Use 90% of the screen width
    public float playAreaHeightPercent = 0.9f; // Use 90% of the screen height

    void Start()
    {
        if (spritePrefab != null)
        {
            GenerateGrid();
        }
        else
        {
            Debug.LogError("SpritePrefab is not assigned! Please assign a prefab.");
        }
    }

    // Generate the grid of sprites
    void GenerateGrid()
    {
        // Get screen size in world units
        Vector2 screenSize = GetScreenSizeInWorldUnits();

        // Adjust the play area size based on the percentage values
        float playAreaWidth = screenSize.x * playAreaWidthPercent - 2 * padding;
        float playAreaHeight = screenSize.y * playAreaHeightPercent - 2 * padding;

        // Calculate the size of each grid cell to fit the play area
        float cellWidth = playAreaWidth / gridWidth;
        float cellHeight = playAreaHeight / gridHeight;

        // Get the size of the sprite for scaling purposes
        SpriteRenderer spriteRenderer = spritePrefab.GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            Debug.LogError("SpritePrefab does not have a SpriteRenderer component!");
            return;
        }

        // Calculate the scale factor to make the sprite fit within the cell
        Vector3 spriteSize = spriteRenderer.bounds.size;
        float scaleX = cellWidth / spriteSize.x;
        float scaleY = cellHeight / spriteSize.y;

        // Adjust the grid to be centered within the play area
        Vector2 gridOffset = new Vector2(
            -playAreaWidth / 2 + cellWidth / 2 + padding,
            -playAreaHeight / 2 + cellHeight / 2 + padding
        );

        // Loop through and spawn the grid elements
        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                // Calculate the position for each grid element
                Vector2 spawnPosition = new Vector2(
                    x * cellWidth + gridOffset.x,
                    y * cellHeight + gridOffset.y
                );

                // Instantiate the spritePrefab at the calculated position
                GameObject newSprite = Instantiate(spritePrefab, spawnPosition, Quaternion.identity);

                // Scale the sprite to fit in the cell
                newSprite.transform.localScale = new Vector3(scaleX, scaleY, 1);

                // Optional: Parent the instantiated object to keep the hierarchy clean
                newSprite.transform.parent = transform;

                // Rename the object for easier identification in the hierarchy
                newSprite.name = "Grid Element (" + x + ", " + y + ")";
            }
        }
    }

    // Function to calculate the screen size in world units
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

        // Calculate screen width and height in world units
        float screenWidth = screenTopRight.x - screenBottomLeft.x;
        float screenHeight = screenTopRight.y - screenBottomLeft.y;

        return new Vector2(screenWidth, screenHeight);
    }
}