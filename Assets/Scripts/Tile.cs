using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class Tile : MonoBehaviour
{
    private static Tile selected;
#pragma warning disable CS0108 // Member hides inherited member; missing new keyword
    private SpriteRenderer renderer;
#pragma warning restore CS0108 // Member hides inherited member; missing new keyword
    public Vector2 position;

    // Start is called before the first frame update
    void Start()
    {
        renderer = GetComponent<SpriteRenderer>();
    }

    // Update is called once per frame
    void Update()
    {
    }
    public void Select()
    {
        renderer.color = Color.grey;
    }

    public void Unselect()
    {
        renderer.color = Color.white;
    }

    private void OnMouseDown()
    {
        if (GridManager.Instance.isGameOver || GridManager.Instance.isInputDisabled)
        {
            return;
        }
        if (selected == null || Vector2.Distance(selected.position, position) != 1)
        {
            if (selected != null)
            {
                selected.Unselect();
            }
            SoundManager.Instance.PlaySound(SoundType.TypeSelect);
            selected = this;
            Select();
            return;
        }

        if (selected != this)
        {
            GridManager.Instance.SwapTiles(position, selected.position);
        }

        SoundManager.Instance.PlaySound(SoundType.TypeSelect);
        selected.Unselect();
        selected = null;
    }

    // called when the cube hits the floor
    void OnCollisionEnter2D(Collision2D col)
    {
        Debug.Log($"OnCollisionEnter2D: {position.x},{position.y}");
    }
}
