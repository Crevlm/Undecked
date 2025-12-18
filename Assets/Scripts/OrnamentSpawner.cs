using NUnit.Framework;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class OrnamentSpawner : MonoBehaviour
{
    public GameObject[] ornamentPrefabs;
    public SpriteRenderer treeRenderer;
    public int ornamentCount = 20;
    public int maxAttempts = 100;
    public float minOrnamentSpacing = 0.25f;

    private readonly List<Vector3> spawnedPositions = new();


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        SpawnAllOrnaments();
    }
    /// <summary>
    /// Spawns the configured number of ornaments by creating each ornament individually.
    /// </summary>
 
    void SpawnAllOrnaments()
    {
        for (int i = 0; i < ornamentCount; i++)
        {
            SpawnRandomOrnament();
        }
    }
    /// <summary>
    /// Attempts to spawn a randomly selected ornament at a valid position on the tree, ensuring it is not placed too
    /// close to existing ornaments.
    /// </summary>
    
    void SpawnRandomOrnament()
    {
        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            Vector3 randomPos = GetRandomPointOnTree();

            if (IsTooCloseToOtherOrnaments(randomPos))
                continue;

            GameObject randomOrnament =
                ornamentPrefabs[Random.Range(0, ornamentPrefabs.Length)];

            Instantiate(randomOrnament, randomPos, Quaternion.identity);
            spawnedPositions.Add(randomPos);
            return;
        }

        Debug.LogWarning("Failed to find spaced ornament position.");
    }
    /// <summary>
    /// Determines whether the specified candidate position is too close to any existing ornament positions.
    /// </summary>
    /// <param name="candidate">The position to evaluate for proximity to other ornaments.</param>
    /// <returns><see langword="true"/> if the candidate position is closer than the minimum allowed spacing to any existing
    /// ornament; otherwise, <see langword="false"/>.</returns>
    bool IsTooCloseToOtherOrnaments(Vector3 candidate)
    {
        foreach (var pos in spawnedPositions)
        {
            if (Vector3.Distance(candidate, pos) < minOrnamentSpacing)
                return true;
        }

        return false;
    }
    /// <summary>
    /// Returns a randomly selected point located on the visible area of the tree.
    /// </summary>
    
    Vector3 GetRandomPointOnTree()
    {
        Bounds bounds = treeRenderer.bounds;
        
        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            //Random point on the Tree's bounds
            float x = Random.Range(bounds.min.x, bounds.max.x);
            float y = Random.Range(bounds.min.y, bounds.max.y);
            Vector3 worldPos = new Vector3(x, y, 0);

            //Checks to see if this point is on a visable pixel on the tree
            if (IsPointOnTree(worldPos))
            {
                return worldPos;
            }

        }

        return treeRenderer.transform.position;
    }

    bool IsPointOnTree(Vector3 worldPos)
    {
        Sprite sprite = treeRenderer.sprite;
        Texture2D tex = sprite.texture;

        // Convert world position to local sprite position
        Vector3 localPos = treeRenderer.transform.InverseTransformPoint(worldPos);

        // Convert to pixel coordinates using sprite's pixelsPerUnit and pivot
        float pixelX = (localPos.x * sprite.pixelsPerUnit) + sprite.pivot.x;
        float pixelY = (localPos.y * sprite.pixelsPerUnit) + sprite.pivot.y;

       
       

        // Check if within texture bounds
        if (pixelX < 0 || pixelX >= tex.width ||
            pixelY < 0 || pixelY >= tex.height)
        {
            return false;
        }

        // Check the alpha at this pixel
        Color pixelColor = tex.GetPixel((int)pixelX, (int)pixelY);
        return pixelColor.a > 0.1f;
    }


    // Update is called once per frame
    void Update()
    {
        
    }
}
