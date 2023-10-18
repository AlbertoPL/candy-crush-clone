using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using Random = UnityEngine.Random;
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
    public bool isGridReleased;
    public bool isComboing;
    private HashSet<int> layerMasks;
    private bool areTilesFalling;
    private bool arePhysicsRemoved;

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
        layerMasks = new HashSet<int>
        {
            UNCOLLIDABLE
        };
        GameObject floor = GameObject.Find("InvisibleFloor");
        Renderer floorRenderer = floor.GetComponent<Renderer>();
        floorRenderer.enabled = false;
        GameObject tileSpawner = GameObject.Find("TileSpawner");
        tileSpawner.layer = UNCOLLIDABLE;
        StopTileDrops();
        arePhysicsRemoved = false;
        isComboing = false;
        InitGrid();
    }

    bool AreTilesMoving()
    {
        Vector3 positionOffset = transform.position - new Vector3(gridDimension * distance / 2.0f, gridDimension * distance / 2.0f, 0);
        bool isAnyTileMoving = false;
        // check if all grid objects have finally stopped moving
        for (int column = 0; column < gridDimension && !isAnyTileMoving; column++)
        {
            for (int row = 0; row < gridDimension; row++)
            {
                Rigidbody2D rb = GetRigidBodyAt(column, row);
                if (rb != null && rb.velocity.magnitude > 0.01f)
                {
                    isAnyTileMoving = true;
                    break;
                }
                else if (rb != null && Vector3.Distance(new Vector3(column * distance, row * distance, 0) + positionOffset, rb.transform.position) > 0.5f)
                {
                    isAnyTileMoving = true;
                    break;
                }
            }
        }
        return isAnyTileMoving;
    }

    // Update is called once per frame
    void Update()
    {
        if (isInputDisabled && isGridReleased /*&& !AreTilesMoving()*/ && !arePhysicsRemoved)
        {
            arePhysicsRemoved = true;
            StartCoroutine(RemovePhysicsConstraint());
        }
        /*if (isInputDisabled && isGridReleased && arePhysicsRemoved)
        {
            Debug.Log("Still checking if tiles are falling");
            bool isAnyTileMoving = AreTilesMoving();
            
            if (!isAnyTileMoving)
            {
                Debug.Log("Tiles have stopped falling");
                areTilesFalling = false;
            }
        }*/

        /*if (isInputDisabled)
        {
            //1. check if we need to generate tiles
            //2. allow tiles to drop wait for tiles to fall into place (done by just allowing update function to keep being called)
            //3. if we don't need to generate tiles and nothing is moving...
            // 3a. restore physics constraints
            // 3b. check matches
            // 3c. if check matches returns false, then we are done. If numMoves <= 0 then game over. Otherwise isInputDisabled = false
            // 3d. if check matches returns true, then play pop sound and call RemovePhysicsConstraint();

            bool isAnyTileMoving = false;
            // check if all grid objects have finally stopped moving
            for (int column = 0; column < gridDimension && !isAnyTileMoving; column++)
            {
                for (int row = 0; row < gridDimension; row++)
                {
                    Rigidbody2D rb = GetRigidBodyAt(column, row);
                    if (rb != null && rb.velocity.y < 0)
                    {
                        isAnyTileMoving = true;
                        break;
                    }
                }
            }

            if (!isAnyTileMoving && !areTilesFalling)
            {
                Debug.Log("Tiles have finished moving");
                FillHoles();
                DropTiles();
            }
            else if (!isAnyTileMoving && areTilesFalling)
            {
                Debug.Log("Let's stop, tiles have fully dropped");
                StopTileDrops();
                RestorePhysicsConstraint();
                bool changesOccur = CheckMatches();
                if (!changesOccur)
                {
                    if (numMoves <= 0)
                    {
                        numMoves = 0;
                        GameOver();
                    }
                    else
                    {
                        isInputDisabled = false;
                    }
                }
                else
                {
                    SoundManager.Instance.PlaySound(SoundType.TypePop);
                    RemovePhysicsConstraint();
                }
            }
        }*/
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
             || row < 0 || row >= gridDimension || grid[column, row] == null)
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
             || row < 0 || row >= gridDimension || grid[column, row] == null)
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
        return tile != null ? tile.GetComponent<Tile>() : null;
    }

    public void SwapTiles(Vector2 tile1Position, Vector2 tile2Position)
    {
        GameObject tile1 = grid[(int) tile1Position.x, (int) tile1Position.y];
        SpriteRenderer renderer1 = tile1.GetComponent<SpriteRenderer>();

        GameObject tile2 = grid[(int) tile2Position.x, (int) tile2Position.y];
        SpriteRenderer renderer2 = tile2.GetComponent<SpriteRenderer>();

        StartCoroutine(SmoothTileSwap(renderer1, renderer2, tile1, tile2, 0.5f));
    }

    private IEnumerator SmoothTileSwap(SpriteRenderer tile1, SpriteRenderer tile2, GameObject tile1Go, GameObject tile2Go, float time)
    {
        Debug.Log("Smooth tile swap running");
        isInputDisabled = true;
        Vector2 tile1Pos = tile1.transform.position;
        Vector2 tile2Pos = tile2.transform.position;
        int tile1Layer = tile1Go.layer;
        int tile2Layer = tile2Go.layer;
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
                isInputDisabled = false;
            }
            else
            {
                tile1Go.layer = tile2Layer;
                tile2Go.layer = tile1Layer;
                SoundManager.Instance.PlaySound(SoundType.TypePop);
                numMoves--;
                ReleaseGrid();
                //RemovePhysicsConstraint();
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

    /*bool CheckMatches()
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
    }*/

    bool CheckMatches()
    {
        HashSet<Tile> matchedTiles = new HashSet<Tile>();
        for (int row = 0; row < gridDimension; row++)
        {
            for (int column = 0; column < gridDimension; column++)
            {
                Tile currentTile = GetTileAt(column, row);
                SpriteRenderer currentSpriteRenderer = currentTile.GetComponent<SpriteRenderer>();

                List<Tile> horizontalMatches = FindColumnMatchForTile(column, row, currentSpriteRenderer.sprite);
                if (horizontalMatches.Count >= 2)
                {
                    matchedTiles.UnionWith(horizontalMatches);
                    matchedTiles.Add(currentTile);
                }

                List<Tile> verticalMatches = FindRowMatchForTile(column, row, currentSpriteRenderer.sprite);

                if (verticalMatches.Count >= 2)
                {
                    matchedTiles.UnionWith(verticalMatches);
                    matchedTiles.Add(currentTile);
                }
            }
        }

        foreach (Tile tile in matchedTiles)
        {
            Destroy(tile.gameObject);
        }
        score += matchedTiles.Count;
        return matchedTiles.Count > 0;
    }

    void ReleaseGrid()
    {
        Debug.Log("Releasing grid...");
        isGridReleased = true;
    }

    /*List<SpriteRenderer> FindColumnMatchForTile(int col, int row, Sprite sprite)
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
    }*/
    List<Tile> FindColumnMatchForTile(int col, int row, Sprite sprite)
    {
        List<Tile> result = new List<Tile>();
        for (int i = col + 1; i < gridDimension; i++)
        {
            Tile nextColumn = GetTileAt(i, row);
            if (nextColumn.GetComponent<SpriteRenderer>().sprite != sprite)
            {
                break;
            }
            result.Add(nextColumn);
        }
        return result;
    }
    /*List<SpriteRenderer> FindRowMatchForTile(int col, int row, Sprite sprite)
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
    }*/

    List<Tile> FindRowMatchForTile(int col, int row, Sprite sprite)
    {
        List<Tile> result = new List<Tile>();
        for (int i = row + 1; i < gridDimension; i++)
        {
            Tile nextRow = GetTileAt(col, i);
            if (nextRow.GetComponent<SpriteRenderer>().sprite != sprite)
            {
                break;
            }
            result.Add(nextRow);
        }
        return result;
    }

    /*void FillHoles()
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
    }*/

    //spawns a new tile at the tile spawner. The tile is unfrozen in the y direction and has the same layer as items in its column so that there's no collision side to side
    GameObject SpawnNewTile(int column, int row, int spawnPos)
    {
        Debug.Log($"Spawning new tile at {column}, {row}");
        Vector3 positionOffset = transform.position - new Vector3(gridDimension * distance / 2.0f, 0, 0);
        GameObject tileSpawner = GameObject.Find("TileSpawner");

        GameObject newTile = Instantiate(tilePrefab);
        newTile.layer = UNCOLLIDABLE + column + 1;
        SpriteRenderer renderer = newTile.GetComponent<SpriteRenderer>();
        Rigidbody2D rb = newTile.GetComponent<Rigidbody2D>();
        rb.constraints &= ~RigidbodyConstraints2D.FreezePositionY;
        Debug.Log($"RB constraints: {rb.constraints}");
        renderer.sprite = sprites[Random.Range(0, sprites.Count)];
        Tile tile = newTile.AddComponent<Tile>();
        tile.position = new Vector2(column, row);
        newTile.transform.parent = transform;
        newTile.transform.position = new Vector3(column * distance, (spawnPos + 1) * distance + tileSpawner.transform.position.y + renderer.bounds.size.y, 0) + positionOffset;
        return newTile;
    }

    void FillHoles()
    {
        Debug.Log("Filling holes...");
        for (int column = 0; column < gridDimension; column++)
        {
            int holeCount = 0;
            for (int row = 0; row < gridDimension; row++)
            {
                if (GetTileAt(column, row) == null)
                {
                    holeCount++;
                }
                else if (holeCount > 0)
                {
                    grid[column, row - holeCount] = grid[column, row];
                    grid[column, row - holeCount].GetComponent<Tile>().position = new Vector2(column, row - holeCount);
                }
            }

            for (int x = 0; x < holeCount; ++x)
            {
                grid[column, gridDimension - holeCount + x] = SpawnNewTile(column, gridDimension - holeCount + x, x); //we use x because we want to spawn items from the same starting spot regardless
            }
        }


                /*for (int column = 0; column < gridDimension; column++)
        {
            bool canFindTileAbove = true;
            for (int row = 0; row < gridDimension; row++)
            {
                if (canFindTileAbove && GetSpriteRendererAt(column, row) == null) 
                {
                    bool foundTileAbove = false;
                    //swap tiles if we can
                    for (int x = row + 1; x < gridDimension && canFindTileAbove; x++)
                    {
                        SpriteRenderer currSpriteRenderer = GetSpriteRendererAt(column, x);
                        //We found a non-empty tile above, so let's swap it into the spot that's null and bail out
                        if (currSpriteRenderer != null)
                        {
                            grid[column, row] = GenerateNewTileFromExisting(column, row, currSpriteRenderer);
                            grid[column, x] = null;
                            foundTileAbove = true;
                            break;
                        }
                    }
                    canFindTileAbove = !foundTileAbove;
                }

                //no more tiles to swap, we need to just generate all the remaining missing tiles
                if (!canFindTileAbove)
                {
                    grid[column, row] = GenerateNewTileFromExisting(column, row, null);
                }
            }
        }*/
    }

    void RestorePhysicsConstraint() {
        Debug.Log("Restoring physics constraints...");
        List<int> layerMasksList = layerMasks.ToList();
        for (int x = 0; x < layerMasksList.Count; ++x)
        {
            for (int y = 0; y < layerMasksList.Count; ++y)
            {
                if (x != y)
                {
                    Physics2D.IgnoreLayerCollision(layerMasksList[x], layerMasksList[y], false);
                }
            }
        }

        for (int column = 0; column < gridDimension; ++column)
        {
            for (int row = 0; row < gridDimension; ++row)
            {
                SpriteRenderer currentSpriteRenderer = GetSpriteRendererAt(column, row);
                Rigidbody2D currentRigidBody = GetRigidBodyAt(column, row);
                currentRigidBody.constraints = RigidbodyConstraints2D.FreezeAll;
                currentSpriteRenderer.gameObject.layer = UNCOLLIDABLE;
            }
        }
        Debug.Log("Grid locked");
        isGridReleased = false;
    }

    void DropTiles()
    {
        Physics2D.IgnoreLayerCollision(UNCOLLIDABLE, 31, true);
        Physics2D.IgnoreLayerCollision(UNCOLLIDABLE + 1, 31, true);
        Physics2D.IgnoreLayerCollision(UNCOLLIDABLE + 2, 31, true);
        Physics2D.IgnoreLayerCollision(UNCOLLIDABLE + 3, 31, true);
        Physics2D.IgnoreLayerCollision(UNCOLLIDABLE + 4, 31, true);
        Physics2D.IgnoreLayerCollision(UNCOLLIDABLE + 5, 31, true);
        Physics2D.IgnoreLayerCollision(UNCOLLIDABLE + 6, 31, true);
        Physics2D.IgnoreLayerCollision(UNCOLLIDABLE + 7, 31, true);
        Physics2D.IgnoreLayerCollision(UNCOLLIDABLE + 8, 31, true);
        areTilesFalling = true;
    }

    void StopTileDrops()
    {
        Physics2D.IgnoreLayerCollision(UNCOLLIDABLE, 31, false);
        Physics2D.IgnoreLayerCollision(UNCOLLIDABLE + 1, 31, false);
        Physics2D.IgnoreLayerCollision(UNCOLLIDABLE + 2, 31, false);
        Physics2D.IgnoreLayerCollision(UNCOLLIDABLE + 3, 31, false);
        Physics2D.IgnoreLayerCollision(UNCOLLIDABLE + 4, 31, false);
        Physics2D.IgnoreLayerCollision(UNCOLLIDABLE + 5, 31, false);
        Physics2D.IgnoreLayerCollision(UNCOLLIDABLE + 6, 31, false);
        Physics2D.IgnoreLayerCollision(UNCOLLIDABLE + 7, 31, false);
        Physics2D.IgnoreLayerCollision(UNCOLLIDABLE + 8, 31, false);
        areTilesFalling = false;
    }

    IEnumerator RemovePhysicsConstraint()
    {
        Debug.Log("Removing physics constraints...");
        isComboing = true;
        List<int> columnsWithHoles = new List<int>();
        List<int> rowToStartOn = new List<int>(); //first row above the hole

        for (int column = 0; column < gridDimension; column++)
        {
            for (int row = 0; row < gridDimension; row++)
            {
                Tile currentTile = GetTileAt(column, row);
                if (currentTile == null)
                {
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
        for (int column = 0; column < gridDimension; ++column)
        {
            for (int row = 0; row < gridDimension; ++row)
            {
                Tile currentTile = GetTileAt(column, row);
                if (currentTile != null && columnsWithHoles.Contains(column))
                {
                    currentTile.gameObject.layer = column + UNCOLLIDABLE + 1;
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
                    Physics2D.IgnoreLayerCollision(layerMasksList[x], layerMasksList[y], true);
                }
            }
        }

        /*foreach (SpriteRenderer sr in spriteObjectsToDelete)
        {
            Destroy(sr.gameObject);
        }*/

        for (int x = 0; x < columnsWithHoles.Count; x++)
        {
            //unfreeze all items above the hole so they can be affected by physics
            for (int row = rowToStartOn[x]; row < gridDimension; row++)
            {
                Rigidbody2D itemInColumnAboveHole = GetRigidBodyAt(columnsWithHoles[x], row);
                itemInColumnAboveHole.constraints &= ~RigidbodyConstraints2D.FreezePositionY;
            }
        }

        //first grid tiles drop

        /*yield return new WaitForFixedUpdate();
        yield return new WaitUntil(() => AreTilesMoving() == true);
        //areTilesFalling = true;
        
        */
        yield return new WaitUntil(() => AreTilesMoving() == false);
        //then we fill in the holes and allow new tiles to drop
        FillHoles();

        /*yield return new WaitForFixedUpdate();
        yield return new WaitUntil(() => AreTilesMoving() == true);*/
        Debug.Log("Tiles started to move again");
        Debug.Log(string.Format("Here's the list: ({0}).", string.Join(", ", layerMasksList)));

        //areTilesFalling = true;
        yield return new WaitUntil(() => AreTilesMoving() == true);
        yield return new WaitUntil(() => AreTilesMoving() == false);
        //areTilesFalling = false;
        //now we restore the grid
        RestorePhysicsConstraint();

        //Combo checking!
        bool changesOccur = CheckMatches();
        if (!changesOccur)
        {
            Debug.Log("No more changes");
            if (numMoves <= 0)
            {
                Debug.Log("Game over man");
                numMoves = 0;
                GameOver();
            }
            else
            {
                Debug.Log("Input restored");
                isInputDisabled = false;
            }
        }
        else
        {
            Debug.Log("More changes!");
            SoundManager.Instance.PlaySound(SoundType.TypePop);
            ReleaseGrid();
        }
        arePhysicsRemoved = false;
        isComboing = false;
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
