using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Services.Analytics;
using UnityEditor.Tilemaps;
using UnityEditorInternal.VersionControl;
using UnityEngine;
using UnityEngine.UIElements;
using System.Linq;

public class GridManager : MonoBehaviour
{
    public static int UNCOLLIDABLE = 6;
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
                newTile.layer = UNCOLLIDABLE; // all new tiles go into layer 6 (UNCOLLIDABLE), as we will need this later to deal with physics
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
        Debug.Log("Smooth tile swap running");
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
            Debug.Log("Hey hey hey heeeey");
            bool changesOccur = CheckMatches();
            if (!changesOccur)
            {
                // swap back, no match 3 attained
                temp = tile1.sprite;
                tile1.sprite = tile2.sprite;
                tile2.sprite = temp;
                SoundManager.Instance.PlaySound(SoundType.TypeNoMatch);
                isInputDisabled = false;
            }
            else
            {
                SoundManager.Instance.PlaySound(SoundType.TypePop);
                numMoves--;
                RemovePhysicsConstraint();
                /*FillHolesSlowly(2f);
                do
                {
                    Debug.Log("We're done!");
                } while (CheckMatches());
                if (numMoves <= 0)
                {
                    numMoves = 0;
                    GameOver();
                }*/
            }
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

    void RemovePhysicsConstraint()
    {
        List<int> columnsWithHoles = new List<int>();
        List<int> rowToStartOn = new List<int>(); //first row above the hole
        List<SpriteRenderer> spriteObjectsToDelete = new List<SpriteRenderer>();

        for (int column = 0; column < gridDimension; column++)
        {
            for (int row = 0; row < gridDimension; row++)
            {
                SpriteRenderer currentSpriteRenderer = GetSpriteRendererAt(column, row);
                if (currentSpriteRenderer.sprite == null)
                {
                    spriteObjectsToDelete.Add(currentSpriteRenderer);
                    if (columnsWithHoles.Contains(column))
                    {
                        rowToStartOn[columnsWithHoles.IndexOf(column)] = row + 1;
                    }
                    else
                    {
                        columnsWithHoles.Add(column);
                        rowToStartOn.Add(row + 1);
                    }
                }
            }
        }

        // all columns that are not a part of the physics should be put into a layer where moving objects cannot collide with them
        // furthermore, objects in a column should only collide with items in their column, so we'll set that as well.
        HashSet<int> layerMasks = new HashSet<int>();
        layerMasks.Add(UNCOLLIDABLE);
        for (int column = 0; column < gridDimension; ++column)
        {
            for (int row = 0; row < gridDimension; ++row)
            {
                SpriteRenderer currentSpriteRenderer = GetSpriteRendererAt(column, row);
                if (columnsWithHoles.Contains(column))
                {
                    currentSpriteRenderer.gameObject.layer = column + UNCOLLIDABLE + 1;
                    layerMasks.Add(column + UNCOLLIDABLE + 1);
                }
            }
        }
        List<int> layerMasksList = layerMasks.ToList();
        for (int x = 0; x < layerMasksList.Count; ++x)
        {
            for (int y = 0; y < layerMasksList.Count; ++y)
            {
                if (x != y)
                {
                    Debug.Log($"{layerMasksList[x]} will ignore collisions from {layerMasksList[y]}");
                    Physics2D.IgnoreLayerCollision(layerMasksList[x], layerMasksList[y], true);
                }
            }
        }

        for (int x = 0; x< gridDimension; ++x)
        {
            for (int y = 0; y < gridDimension; ++y)
            {
                SpriteRenderer currentSpriteRenderer = GetSpriteRendererAt(x, y);
            }
        }

        foreach (SpriteRenderer sr in spriteObjectsToDelete)
        {
            Destroy(sr.gameObject);
        }

        for (int x = 0; x < columnsWithHoles.Count; x++)
        {
            //unfreeze all items above the hole so they can be affected by physics
            for (int row = rowToStartOn[x]; row < gridDimension; row++)
            {
                Rigidbody2D itemInColumnAboveHole = GetRigidBodyAt(columnsWithHoles[x], row);
                itemInColumnAboveHole.constraints &= ~RigidbodyConstraints2D.FreezePositionY;
            }
        }
    }

    void FillHolesSlowly(float time)
    {
        Vector3 positionOffset = transform.position - new Vector3(gridDimension * distance / 2.0f, gridDimension * distance / 2.0f, 0);
        float elapsedTime = 0;

        List<int> columnsWithHoles = new List<int>();
        List<int> rowToStartOn = new List<int>(); //first row above the hole
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
        List<Rigidbody2D> lastItems = new List<Rigidbody2D>();
        List<Vector2> finalPosition = new List<Vector2>();
        
        for (int x = 0; x < columnsWithHoles.Count; x++)
        {
            //unfreeze all items above the hole so they can be affected by physics
            for (int row = rowToStartOn[x]; row < gridDimension; row++)
            {
                Rigidbody2D topItemInColumn = GetRigidBodyAt(columnsWithHoles[x], row);
                topItemInColumn.constraints &= ~RigidbodyConstraints2D.FreezePositionY;
            }

            int currentItemRow = rowToStartOn[x];
            if (currentItemRow < gridDimension)
            {
                Rigidbody2D topItemInColumn = GetRigidBodyAt(columnsWithHoles[x], gridDimension - 1); //get the very top most item, not the first item above the hole
                if (topItemInColumn != null)
                {
                    Debug.Log("Adding force...");
                    finalPosition.Add(new Vector3(columnsWithHoles[x] * distance, firstPositionOfHole[x] * distance, 0) + positionOffset);
                    topItemInColumn.AddForce(Vector2.down * 10f, ForceMode2D.Impulse); // only apply a force if there are items above a hole, otherwise no movement in the column
                    topItems.Add(topItemInColumn);
                    lastItems.Add(GetRigidBodyAt(columnsWithHoles[x], rowToStartOn[x]));
                }
            }
        }

        while (elapsedTime < time)
        {
            for (int x = 0; x < topItems.Count; ++x)
            {
                //stop movement if we reach the finalPosition
                //Debug.Log("X: " + x);
                //Debug.Log("Item pos: " + topItems[x].transform.position.y);
                //Debug.Log("Item velocity: " + topItems[x].velocity.y);
                //Debug.Log("Final pos: " + finalPosition[x].y);
                if (lastItems[x].transform.position.y <= finalPosition[x].y && topItems[x].velocity.y < 0)
                {
                    Debug.Log("WE DONE");
                    topItems[x].velocity = Vector2.zero;
                    lastItems[x].velocity = Vector2.zero;
                    lastItems[x].transform.position = finalPosition[x];
                }
            }
            elapsedTime += Time.deltaTime;
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
