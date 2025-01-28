using System.Buffers.Text;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TerrainUtilities : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    
    public float GetTerrainHeightAtPosition(float x, float baseY, Vector3[] topTerrainPoints)
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
    public Vector3 GetGroundPositionAtPoint(float x, float baseY, Vector3[] topTerrainPoints)
    {
        float y = GetTerrainHeightAtPosition(x, baseY, topTerrainPoints);
        return new Vector3(x, y, 0f);
    }
    public Vector3 GetTerrainPositionAtPoint(float x, float baseY, Vector3[] topTerrainPoints)
    {
        return GetGroundPositionAtPoint(x, baseY, topTerrainPoints);
    }
    public Vector3 GetClosestTerrainPoint(Vector3 pos, Vector3[] topTerrainPoints)
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
