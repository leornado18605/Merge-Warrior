using ObjectPooling;
using UnityEngine;
using System.Collections.Generic;

public class GridManager : MonoBehaviour
{
    public GameObject tilePrefab;
    public int rows = 8;
    public int cols = 8;
    public float tileSize = 1f;

    private GameObject[,] tiles;
    private GameObject[] areaSpawn;

    private List<GameObject> board1Tiles = new List<GameObject>();
    private List<GameObject> board2Tiles = new List<GameObject>();

    void Start()
    {
        int totalTiles = rows * cols * 2;
        PoolManager.CreatePool(tilePrefab, initialSize: totalTiles, maxSize: totalTiles, autoExpand: false);
        tiles = new GameObject[rows, cols];
        SpawnBoards();
    }

    void SpawnBoards()
    {
        Vector3 parentPos = transform.position;
        float gap = tileSize;
        float board1StartZ = -(rows * tileSize + gap);

        SpawnSingleBoard(parentPos + new Vector3(0, 0, board1StartZ), true);  // Board1
        SpawnSingleBoard(parentPos, false);  // Board2
    }

    void SpawnSingleBoard(Vector3 startPos, bool isBoard1)
    {
        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                Vector3 offset = new Vector3(c * tileSize, 0, r * tileSize);
                Vector3 spawnPos = startPos + offset;
                GameObject tile = PoolManager.Spawn(tilePrefab, spawnPos, Quaternion.identity, transform);

                if (isBoard1)
                {
                    board1Tiles.Add(tile);
                }
                else
                {
                    board2Tiles.Add(tile);
                    tiles[r, c] = tile;
                }
            }
        }
    }

    public void ClearBoard()
    {
        // Clear Board1 tiles
        foreach (GameObject tile in board1Tiles)
        {
            if (tile != null)
            {
                PoolManager.Release(tile);
            }
        }
        board1Tiles.Clear();

        // Clear Board2 tiles
        foreach (GameObject tile in board2Tiles)
        {
            if (tile != null)
            {
                PoolManager.Release(tile);
            }
        }
        board2Tiles.Clear();

        // Clear tiles array
        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                tiles[r, c] = null;
            }
        }
    }

    // Clear Board1
    public void ClearBoard1()
    {
        foreach (GameObject tile in board1Tiles)
        {
            if (tile != null)
            {
                PoolManager.Release(tile);
            }
        }
        board1Tiles.Clear();
    }

    // Clear Board2
    public void ClearBoard2()
    {
        foreach (GameObject tile in board2Tiles)
        {
            if (tile != null)
            {
                PoolManager.Release(tile);
            }
        }
        board2Tiles.Clear();

        // Clear tiles array
        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                tiles[r, c] = null;
            }
        }
    }
}