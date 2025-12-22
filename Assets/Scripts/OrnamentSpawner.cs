using NUnit.Framework;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class OrnamentSpawner : MonoBehaviour
{
    [Header("Spawner Settings")]
    public GameObject[] ornamentPrefabs;
    public SpriteRenderer treeRenderer;
    public int ornamentCount = 20;
    public int maxAttempts = 100;
    public float minOrnamentSpacing = 1f;

    // Tracks previously used spawn positions for spacing checks
    private readonly List<Vector3> spawnedPositions = new();

    // Tracks spawn ornament instances to be destroyed on restart
    private readonly List<GameObject> spawnedOrnaments = new();


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
        if (treeRenderer == null)
        {
            Debug.LogWarning("OrnamentSpawner: treeRenderer is not assigned.");
            return;
        }

        if (ornamentPrefabs == null || ornamentPrefabs.Length == 0)
        {
            Debug.LogWarning("OrnamentSpawner: ornamentPrefabs is empty.");
            return;
        }

        for (int i = 0; i < ornamentCount; i++)
        {
            SpawnRandomOrnament();
        }

        
        PolygonCollider2D treeCollider = treeRenderer.GetComponent<PolygonCollider2D>();
        if (treeCollider != null)
        {
            treeCollider.enabled = false;
        }
    }

    /// <summary>
    /// Clears all previously spawned ornaments and spawns a fresh set.
    /// </summary>
    /// <remarks>
    /// This is intended to be called by a game flow controller when restarting a round.
    /// </remarks>
    public void RespawnAllOrnaments()
    {
        ClearSpawnedOrnaments();
        SpawnAllOrnaments();
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

            GameObject randomOrnamentPrefab = ornamentPrefabs[Random.Range(0, ornamentPrefabs.Length)];

            // Parent under the spawner so cleanup is easy and consistent.
            GameObject instance = Instantiate(randomOrnamentPrefab, randomPos, Quaternion.identity, transform);

            spawnedPositions.Add(randomPos);
            spawnedOrnaments.Add(instance);
            return;
        }

        Debug.LogWarning("Failed to find spaced ornament position.");
    }

    /// <summary>
    /// Destroys all spawned ornament instances and clears cached spawn data.
    /// </summary>
    /// <remarks>
    /// Clearing spawnedPositions is required so the spacing checks don't treat old positions as occupied.
    /// </remarks>
    private void ClearSpawnedOrnaments()
    {
        for (int i = 0; i < spawnedOrnaments.Count; i++)
        {
            if (spawnedOrnaments[i] != null)
            {
                Destroy(spawnedOrnaments[i]);
            }
        }

        spawnedOrnaments.Clear();
        spawnedPositions.Clear();
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
        PolygonCollider2D treeCollider = treeRenderer.GetComponent<PolygonCollider2D>();

        // Add margin to keep ornaments away from bottom
        float marginBottom = bounds.size.y * 0.15f; // //value from the bottom of the tree to keep them from weirdly spawning too close to the boxes

        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            float x = Random.Range(bounds.min.x, bounds.max.x);
            float y = Random.Range(bounds.min.y + marginBottom, bounds.max.y); 
            Vector3 worldPos = new Vector3(x, y, 0);

            // Check if point is inside the polygon collider
            if (treeCollider != null && treeCollider.OverlapPoint(worldPos))
            {
                return worldPos;
            }
        }

        return treeRenderer.transform.position;
    }

    
    }

}
