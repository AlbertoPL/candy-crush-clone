using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

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

    public void SwapTiles(Vector2 tile1Position, Vector2 tile2Position)
    {
        GameObject tile1 = grid[(int) tile1Position.x, (int) tile1Position.y];
        SpriteRenderer renderer1 = tile1.GetComponent<SpriteRenderer>();

        GameObject tile2 = grid[(int) tile2Position.x, (int) tile2Position.y];
        SpriteRenderer renderer2 = tile2.GetComponent<SpriteRenderer>();

        //Sprite temp = renderer1.sprite;
        //renderer1.sprite = renderer2.sprite;
        //renderer2.sprite = temp;
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
                    FillHoles();
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
            for (int row = 0; row < gridDimension; row++) // 1
            {
                while (GetSpriteRendererAt(column, row).sprite == null) // 2
                {
                    for (int filler = row; filler < gridDimension - 1; filler++) // 3
                    {
                        SpriteRenderer current = GetSpriteRendererAt(column, filler); // 4
                        SpriteRenderer next = GetSpriteRendererAt(column, filler + 1);
                        current.sprite = next.sprite;
                    }
                    SpriteRenderer last = GetSpriteRendererAt(column, gridDimension - 1);
                    last.sprite = sprites[Random.Range(0, sprites.Count)]; // 5
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
