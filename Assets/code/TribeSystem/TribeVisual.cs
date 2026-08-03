using UnityEngine;

/// <summary>Chooses a tribe sprite, draws its neutral flag, and displays population.</summary>
public class TribeVisual : MonoBehaviour
{
    [Header("Character Sprite")]
    [SerializeField] private SpriteRenderer characterRenderer;
    [SerializeField] private Sprite[] characterSprites;
    private TextMesh populationText;
    private TribeGroup tribe;

    private void Awake()
    {
        tribe = GetComponent<TribeGroup>();
        ChooseRandomSprite();
        CreateNeutralFlag();
        CreatePopulationLabel();
    }

    private void LateUpdate()
    {
        if (tribe != null && populationText != null)
        {
            populationText.text = Mathf.RoundToInt(tribe.Population).ToString();
        }
    }

    private void ChooseRandomSprite()
    {
        if (characterRenderer == null)
        {
            characterRenderer = GetComponent<SpriteRenderer>();
        }

        if (characterRenderer == null || characterSprites == null || characterSprites.Length == 0)
        {
            return;
        }

        characterRenderer.sprite = characterSprites[Random.Range(0, characterSprites.Length)];
        characterRenderer.enabled = characterRenderer.sprite != null;
        characterRenderer.sortingOrder = 3;
    }

    private void CreateNeutralFlag()
    {
        GameObject flag = new GameObject("NeutralFlag");
        flag.transform.SetParent(transform, false);
        flag.transform.localPosition = new Vector3(0.32f, 0.95f, 0f);
        flag.transform.localScale = Vector3.one * 0.35f;
        SpriteRenderer renderer = flag.AddComponent<SpriteRenderer>();
        renderer.sprite = CreateFlagSprite();
        renderer.sortingOrder = 6;
    }

    private void CreatePopulationLabel()
    {
        GameObject label = new GameObject("PopulationLabel");
        label.transform.SetParent(transform, false);
        label.transform.localPosition = new Vector3(0f, 1.3f, 0f);
        populationText = label.AddComponent<TextMesh>();
        populationText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        populationText.anchor = TextAnchor.MiddleCenter;
        populationText.alignment = TextAlignment.Center;
        populationText.characterSize = 0.11f;
        populationText.fontSize = 48;
        populationText.color = Color.white;
        populationText.text = tribe != null ? Mathf.RoundToInt(tribe.Population).ToString() : "0";
        label.GetComponent<MeshRenderer>().sortingOrder = 7;
    }

    private static Sprite CreateFlagSprite()
    {
        Texture2D texture = new Texture2D(12, 8, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Point;
        Color white = new Color(0.86f, 0.86f, 0.8f, 1f);
        for (int y = 0; y < texture.height; y++)
        for (int x = 0; x < texture.width; x++)
        {
            bool border = x < 2 || x == texture.width - 1 || y == 0 || y == texture.height - 1;
            texture.SetPixel(x, y, border ? Color.black : white);
        }
        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.1f, 0.5f), texture.height);
    }
}
