using UnityEngine;

public class BasicDrawing : MonoBehaviour 
{
    [SerializeField] private SpriteRenderer drawing;

    private TextureColor32.Sprite canvasSprite;

    private new Collider collider;

    private bool dragging;
    private Vector2 prevMouse, nextMouse;

    private void Awake()
    {
        // create a 512x512 pixel canvas to draw on
        canvasSprite = TextureColor32.Draw.GetSprite(512, 512);
        // tell unity that for every 512 pixels wide this sprite is, it should
        // be one world unit wide
        canvasSprite.SetPixelsPerUnit(512);
        // fill the sprite with transparent pixels
        canvasSprite.Clear(Color.clear);
        // update the real unity texture so that it can be displayed
        canvasSprite.mTexture.Apply();

        // set the existing SpriteRenderer to use the newly created sprite
        drawing.sprite = canvasSprite.uSprite;

        collider = drawing.GetComponent<Collider>();
    }

    private void Update()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit))
        {
            if (hit.collider == collider)
            {
                nextMouse = (Vector2) hit.point * 512;
            }
        }

        if (Input.GetMouseButton(0))
        {
            if (!dragging)
            {
                dragging = true;
            }
            else
            {
                // pick a random line thickness and colour
                int thickness = Random.Range(1, 6);
                Color color = Color.HSVToRGB(Random.value, 0.75f, 1f);
                color.a = .75f;

                // draw a line onto the canvas sprite using alpha blending, 
                // then apply the changes to the real unity texture
                TextureColor32.Draw.Line(canvasSprite, prevMouse, nextMouse, color, thickness, TextureColor32.alpha);
                canvasSprite.Apply();
            }
        }
        else
        {
            dragging = false;
        }

        prevMouse = nextMouse;
    }
}
