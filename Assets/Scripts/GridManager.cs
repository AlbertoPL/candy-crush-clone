using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Services.Analytics;
using UnityEditor.Tilemaps;
using UnityEngine;
using UnityEngine.UIElements;

public class GridManager : MonoBehaviour
{
    public List<Sprite> sprites;
    public GameObject tilePrefab;
    public int gridDimension;
    public float distance;
    private GameObject[,] grid;
    public bool isInputDisabled;
    public bool isGameOver;

    public static GridManager Instance { get; private set; }
    void Awake() { 
        Instance = this;
        score = 0;
        numMoves = startingMoves;
    }

    public GameObject gameOverMenu;
    public TextMeshProUGUI movesText;
    public TextMeshProUGUI scoreText;
    
    public int startingMoves;
    private int _numMoves;

    public int numMoves
    {
        get
        {
            return _numMoves;
        }

        set
        {
            _numMoves = value;
            movesText.text = _numMoves.ToString();
        }
    }

    private int _score;
    public int score
    {
        get
        {
            return _score;
        }

        set
        {
            _score = value;
            scoreText.text = _score.ToString();
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        grid = new GameObject[gridDimension, gridDimension];
        InitGrid();
    }

    // Update is called once per frame
    void Update()
    {

    }

    void InitGrid()
    {
        Vector3 positionOffset = transform.position - new Vector3(gridDimension * distance / 2.0f, gridDimension * distance / 2.0f, 0);
        for (int row = 0; row < gridDimension; row++)
        {
            for (int column = 0; column < gridDimension; column++)
            {
                List<Sprite> possibleSprites = new List<Sprite>(sprites);

                //Choose what sprite to use for this cell
                Sprite left1 = GetSpriteRendererAt(column - 1, row)?.sprite;
                Sprite left2 = GetSpriteRendererAt(column - 2, row)?.sprite;
                if (left2 != null && left1 == left2)
                {
                    possibleSprites.Remove(left1);
                }

                Sprite down1 = GetSpriteRendererAt(column, row - 1)?.sprite;
                Sprite down2 = GetSpriteRendererAt(column, row - 2)?.sprite;
                if (down2 != null && down1 == down2)
                {
                    possibleSprites.Remove(down1);
                }

                GameObject newTile = Instantiate(tilePrefab);
                SpriteRenderer renderer = newTile.GetComponent<SpriteRenderer>();
                renderer.sprite = possibleSprites[Random.Range(0, possibleSprites.Count)];
                Tile tile = newTile.AddComponent<Tile>();
                tile.position = new Vector2(column, row);
                newTile.transform.parent = transform;
                newTile.transform.position = new Vector3(column * distance, row * distance, 0) + positionOffset;

                grid[column, row] = newTile;
            }
        }
    }

    SpriteRenderer GetSpriteRendererAt(int column, int row)
    {
        if (column < 0 || column >= gridDimension
             || row < 0 || row >= gridDimension)
        {
            return null;
        }
        GameObject tile = grid[column, row];
        SpriteRenderer renderer = tile.GetComponent<SpriteRenderer>();
        return renderer;
    }

    Rigidbody2D GetRigidBodyAt(int column, int row)
    {
        if (column < 0 || column >= gridDimension
             || row < 0 || row >= gridDimension)
        {
            return null;
        }
        GameObject tile = grid[column, row];
        Rigidbody2D rigidBody = tile.GetComponent<Rigidbody2D>();
        return rigidBody;
    }

    Tile GetTileAt(int column, int row)
    {
        if (column < 0 || column >= gridDimension
             || row < 0 || row >= gridDimension)
        {
            return null;
        }
        GameObject tile = grid[column, row];
        return tile.GetComponent<Tile>();
    }

    public void SwapTiles(Vector2 tile1Position, Vector2 tile2Position)
    {
        GameObject tile1 = grid[(int) tile1Position.x, (int) tile1Position.y];
        SpriteRenderer renderer1 = tile1.GetComponent<SpriteRenderer>();

        GameObject tile2 = grid[(int) tile2Position.x, (int) tile2Position.y];
        SpriteRenderer renderer2 = tile2.GetComponent<SpriteRenderer>();

        StartCoroutine(SmoothTileSwap(renderer1, renderer2, 0.5f));
    }

    private IEnumerator SmoothTileSwap(SpriteRenderer tile1, SpriteRenderer tile2, float time)
    {
        isInputDisabled = true;
        Vector2 tile1Pos = tile1.transform.position;
        Vector2 tile2Pos = tile2.transform.position;
        SoundManager.Instance.PlaySound(SoundType.TypeMove);

        float elapsedTime = 0;

        while (elapsedTime < time)
        {
            tile1.transform.position = Vector2.Lerp(tile1Pos, tile2Pos, (elapsedTime / time));
            tile2.transform.position = Vector2.Lerp(tile2Pos, tile1Pos, (elapsedTime / time));
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        if (elapsedTime >= time)
        {
            Sprite temp = tile1.sprite;
            tile1.sprite = tile2.sprite;
            tile2.sprite = temp;
            tile1.transform.position = tile1Pos;
            tile2.transform.position = tile2Pos;
            bool changesOccur = CheckMatches();
            if (!changesOccur)
            {
                // swap back, no match 3 attained
                temp = tile1.sprite;
                tile1.sprite = tile2.sprite;
                tile2.sprite = temp;
                SoundManager.Instance.PlaySound(SoundType.TypeNoMatch);
            }
            else
            {
                SoundManager.Instance.PlaySound(SoundType.TypePop);
                numMoves--;
                do
                {
                    yield return StartCoroutine(FillHolesSlowly(0.5f));
                } while (CheckMatches());
                if (numMoves <= 0)
                {
                    numMoves = 0;
                    GameOver();
                }
            }
            isInputDisabled = false;
        }
    }

    bool CheckMatches()
    {
        HashSet<SpriteRenderer> matchedTiles = new HashSet<SpriteRenderer>();
        for (int row = 0; row < gridDimension; row++)
        {
            for (int column = 0; column < gridDimension; column++)
            {
                SpriteRenderer current = GetSpriteRendererAt(column, row);

                List<SpriteRenderer> horizontalMatches = FindColumnMatchForTile(column, row, current.sprite);
                if (horizontalMatches.Count >= 2)
                {
                    matchedTiles.UnionWith(horizontalMatches);
                    matchedTiles.Add(current);
                }

                List<SpriteRenderer> verticalMatches = FindRowMatchForTile(column, row, current.sprite);
                if (verticalMatches.Count >= 2)
                {
                    matchedTiles.UnionWith(verticalMatches);
                    matchedTiles.Add(current);
                }
            }
        }

        foreach (SpriteRenderer renderer in matchedTiles)
        {
            renderer.sprite = null;
        }
        score += matchedTiles.Count;
        return matchedTiles.Count > 0;
    }

    List<SpriteRenderer> FindColumnMatchForTile(int col, int row, Sprite sprite)
    {
        List<SpriteRenderer> result = new List<SpriteRenderer>();
        for (int i = col + 1; i < gridDimension; i++)
        {
            SpriteRenderer nextColumn = GetSpriteRendererAt(i, row);
            if (nextColumn.sprite != sprite)
            {
                break;
            }
            result.Add(nextColumn);
        }
        return result;
    }

    List<SpriteRenderer> FindRowMatchForTile(int col, int row, Sprite sprite)
    {
        List<SpriteRenderer> result = new List<SpriteRenderer>();
        for (int i = row + 1; i < gridDimension; i++)
        {
            SpriteRenderer nextRow = GetSpriteRendererAt(col, i);
            if (nextRow.sprite != sprite)
            {
                break;
            }
            result.Add(nextRow);
        }
        return result;
    }

    void FillHoles()
    {
        for (int column = 0; column < gridDimension; column++)
        {
            for (int row = 0; row < gridDimension; row++)
            {
                while (GetSpriteRendererAt(column, row).sprite == null)
                {
                    for (int filler = row; filler < gridDimension - 1; filler++)
                    {
                        //Tile currentRigid = GetTileAt(column, filler);
                        SpriteRenderer current = GetSpriteRendererAt(column, filler);
                        //Tile nextRigid = GetTileAt(column, filler);
                        SpriteRenderer next = GetSpriteRendererAt(column, filler + 1);
                        //currentRigid.isFalling = true;
                        //nextRigid.isFalling = true;
                        current.sprite = next.sprite;
                    }
                    SpriteRenderer last = GetSpriteRendererAt(column, gridDimension - 1);
                    last.sprite = sprites[Random.Range(0, sprites.Count)];
                }
            }
        }
    }

    IEnumerator FillHolesSlowly(float time)
    {
        float elapsedTime = 0;

        List<int> columnsWithHoles = new List<int>();
        List<int> rowToStartOn = new List<int>();
        List<int> firstPositionOfHole = new List<int>();

        for (int column = 0; column < gridDimension; column++)
        {
            for (int row = 0; row < gridDimension; row++)
            {
                if (GetSpriteRendererAt(column, row).sprite == null)
                {
                    if (columnsWithHoles.Contains(column))
                    {
                        rowToStartOn[columnsWithHoles.IndexOf(column)] = row + 1;
                    }
                    else
                    {
                        columnsWithHoles.Add(column);
                        firstPositionOfHole.Add(row);
                        rowToStartOn.Add(row + 1);
                    }
                }
            }
        }

        //apply a force on all columns as needed, then elapse time until positions have reached the right place
        List<Rigidbody2D> topItems = new List<Rigidbody2D>();
        List<Vector3> finalPosition = new List<Vector3>();
        
        for (int x = 0; x < columnsWithHoles.Count; x++)
        {
            /*for (int row = rowToStartOn[x]; row < gridDimension; row++)
            {
                Vector2 tilePos = GetSpriteRendererAt(columnsWithHoles[x], row).transform.position;
                GetSpriteRendererAt(columnsWithHoles[x], row).transform.position += (Vector3) Vector2.down * Time.deltaTime;
            }*/
            int currentItemRow = rowToStartOn[x];
            if (currentItemRow < gridDimension)
            {
                Rigidbody2D topItemInColumn = GetRigidBodyAt(columnsWithHoles[x], gridDimension - 1); //get the very top most item, not the first item above the hole
                if (topItemInColumn != null)
                {
                    topItemInColumn.AddForce(Vector2.down * 10f, ForceMode2D.Impulse); // only apply a force if there are items above a hole, otherwise no movement in the column
                    topItems.Add(topItemInColumn);
                    finalPosition.Add(GetSpriteRendererAt(columnsWithHoles[x], firstPositionOfHole[x]).transform.position);
                }
            }
        }

        while (elapsedTime < time)
        {
            Debug.Log("Elapsed time: " + elapsedTime);
            Debug.Log("TOP items: " + topItems.Count);
            for (int x = 0; x < topItems.Count; ++x)
            {
                //stop movement if we reach the finalPosition
                Debug.Log("X: " + x);
                Debug.Log("Item pos: " + topItems[x].transform.position.y);
                Debug.Log("Final pos: " + finalPosition[x].y);
                if (topItems[x].transform.position.y <= finalPosition[x].y && topItems[x].velocity.y > 0)
                {
                    topItems[x].velocity = Vector2.zero;
                    topItems[x].transform.position = finalPosition[x];
                }
            }
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        for (int column = 0; column < gridDimension; column++)
        {
            for (int row = 0; row < gridDimension; row++)
            {
                while (GetSpriteRendererAt(column, row).sprite == null)
                {
                    for (int filler = row; filler < gridDimension - 1; filler++)
                    {
                        SpriteRenderer current = GetSpriteRendererAt(column, filler);
                        SpriteRenderer next = GetSpriteRendererAt(column, filler + 1);
                        current.sprite = next.sprite;
                    }
                    SpriteRenderer last = GetSpriteRendererAt(column, gridDimension - 1);
                    last.sprite = sprites[Random.Range(0, sprites.Count)];
                }
            }
        }
    }

    void GameOver()
    {
        isGameOver = true;
        PlayerPrefs.SetInt("score", score);
        gameOverMenu.SetActive(true);

        SoundManager.Instance.PlaySound(SoundType.TypeGameOver);
    }
}
