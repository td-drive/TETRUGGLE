using UnityEngine;
using System.Collections.Generic;

public class TetrisManager : MonoBehaviour
{
    public GameObject[] tetrominoSprites;
    public float fallTime = 1.0f;
    public float blockSize = 0.9f;
    
    private int[,] grid = new int[10, 20];
    private float previousTime;
    private GameObject currentTetromino;
    private List<GameObject> currentBlocks = new List<GameObject>();
    private int currentTetrominoIndex;
    private Vector2Int currentPosition;
    private int currentRotation;
    private bool isGameOver = false;
    private Transform blockContainer; // 配置済みブロックのコンテナ

    private readonly Vector2Int spawnPosition = new Vector2Int(3, 18);
    
    void Start()
    {
        previousTime = Time.time;
        // ブロックを格納する親オブジェクトを作成
        blockContainer = new GameObject("PlacedBlocks").transform;
        SpawnTetromino();
    }
    
    void Update()
    {
        if (isGameOver || currentTetromino == null) return;
        
        HandleInput();
        HandleFalling();
    }
    
    void HandleInput()
    {
        if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            MoveTetromino(Vector2Int.left);
        }
        else if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            MoveTetromino(Vector2Int.right);
        }
        
        if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            RotateTetromino();
        }
        
        if (Input.GetKey(KeyCode.DownArrow))
        {
            fallTime = 0.1f;
        }
        else
        {
            fallTime = 1.0f;
        }
    }
    
    void HandleFalling()
    {
        if (Time.time - previousTime >= fallTime)
        {
            MoveTetromino(Vector2Int.down);
            previousTime = Time.time;
        }
    }
    
    // グリッド座標からワールド座標に変換
    Vector3 GridToWorldPosition(Vector2Int gridPos)
    {
        return new Vector3(
            gridPos.x * blockSize,
            gridPos.y * blockSize,
            0
        );
    }
    
    void SpawnTetromino()
    {
        currentTetrominoIndex = Random.Range(0, tetrominoSprites.Length);
        currentPosition = spawnPosition;
        currentRotation = 0;
        currentBlocks.Clear();

        // 新しいテトロミノの親オブジェクトを作成
        currentTetromino = new GameObject($"Tetromino_{currentTetrominoIndex}");
        
        // テトロミノのブロックを生成
        int[,,] data = TetrominoData.Tetrominoes;
        for (int y = 0; y < 4; y++)
        {
            for (int x = 0; x < 4; x++)
            {
                if (data[currentTetrominoIndex, y, x] != 0)
                {
                    Vector2Int blockGridPos = new Vector2Int(x, y);
                    GameObject block = Instantiate(
                        tetrominoSprites[currentTetrominoIndex],
                        GridToWorldPosition(currentPosition + blockGridPos),
                        Quaternion.identity,
                        currentTetromino.transform
                    );
                    currentBlocks.Add(block);
                }
            }
        }
            
        if (!ValidMove(Vector2Int.zero))
        {
            isGameOver = true;
            Debug.Log("Game Over!");
            return;
        }
    }
    
    bool ValidMove(Vector2Int moveDirection)
    {
        Vector2Int newPos = currentPosition + moveDirection;
        int[,,] data = TetrominoData.Tetrominoes;
        
        for (int y = 0; y < 4; y++)
        {
            for (int x = 0; x < 4; x++)
            {
                if (data[currentTetrominoIndex, y, x] != 0)
                {
                    Vector2Int gridPos = newPos + new Vector2Int(x, y);
                    
                    if (gridPos.x < 0 || gridPos.x >= 10 || gridPos.y < 0)
                        return false;
                        
                    if (gridPos.y < 20 && grid[gridPos.x, gridPos.y] != 0)
                        return false;
                }
            }
        }
        
        return true;
    }
    
    void MoveTetromino(Vector2Int moveDirection)
    {
        if (ValidMove(moveDirection))
        {
            currentPosition += moveDirection;
            UpdateTetrominoPosition();
        }
        else if (moveDirection == Vector2Int.down)
        {
            PlaceTetromino();
            CheckLines();
            SpawnTetromino();
        }
    }
    
    void UpdateTetrominoPosition()
    {
        if (currentTetromino != null)
        {
            int[,,] data = TetrominoData.Tetrominoes;
            int blockIndex = 0;
            
            for (int y = 0; y < 4; y++)
            {
                for (int x = 0; x < 4; x++)
                {
                    if (data[currentTetrominoIndex, y, x] != 0)
                    {
                        Vector2Int blockGridPos = new Vector2Int(x, y);
                        if (blockIndex < currentBlocks.Count)
                        {
                            currentBlocks[blockIndex].transform.position = 
                                GridToWorldPosition(currentPosition + blockGridPos);
                            blockIndex++;
                        }
                    }
                }
            }
        }
    }
    
    void PlaceTetromino()
    {
        int[,,] data = TetrominoData.Tetrominoes;
        
        for (int y = 0; y < 4; y++)
        {
            for (int x = 0; x < 4; x++)
            {
                if (data[currentTetrominoIndex, y, x] != 0)
                {
                    Vector2Int gridPos = currentPosition + new Vector2Int(x, y);
                    
                    if (gridPos.y < 20 && gridPos.x >= 0 && gridPos.x < 10)
                    {
                        grid[gridPos.x, gridPos.y] = currentTetrominoIndex + 1;
                    }
                }
            }
        }

        // 現在のブロックを配置済みブロックのコンテナに移動
        foreach (GameObject block in currentBlocks)
        {
            block.transform.parent = blockContainer;
        }
        
        Destroy(currentTetromino);
        currentBlocks.Clear();
    }
    
    void CheckLines()
    {
        for (int y = 0; y < 20; y++)
        {
            bool isLine = true;
            
            for (int x = 0; x < 10; x++)
            {
                if (grid[x, y] == 0)
                {
                    isLine = false;
                    break;
                }
            }
            
            if (isLine)
            {
                ClearLine(y);
                MoveLinesDown(y + 1);
                y--; // 同じ行を再チェック
            }
        }
    }
    
    void ClearLine(int y)
    {
        // グリッドのクリア
        for (int x = 0; x < 10; x++)
        {
            grid[x, y] = 0;
        }

        // 該当する行のブロックを削除
        float yWorldPos = y * blockSize;
        foreach (Transform block in blockContainer)
        {
            if (Mathf.Abs(block.position.y - yWorldPos) < 0.1f)
            {
                Destroy(block.gameObject);
            }
        }
    }
    
    void MoveLinesDown(int startY)
    {
        // グリッドの更新
        for (int y = startY; y < 20; y++)
        {
            for (int x = 0; x < 10; x++)
            {
                if (y > 0)
                {
                    grid[x, y - 1] = grid[x, y];
                    grid[x, y] = 0;
                }
            }
        }

        // ブロックの移動
        for (int y = startY; y < 20; y++)
        {
            float yWorldPos = y * blockSize;
            foreach (Transform block in blockContainer)
            {
                if (Mathf.Abs(block.position.y - yWorldPos) < 0.1f)
                {
                    block.position += Vector3.down * blockSize;
                }
            }
        }
    }
    
    void RotateTetromino()
    {
        currentRotation = (currentRotation + 90) % 360;
        if (currentTetromino != null)
        {
            currentTetromino.transform.rotation = Quaternion.Euler(0, 0, currentRotation);
            
            if (!ValidMove(Vector2Int.zero))
            {
                currentRotation = (currentRotation - 90 + 360) % 360;
                currentTetromino.transform.rotation = Quaternion.Euler(0, 0, currentRotation);
            }
        }
    }
}