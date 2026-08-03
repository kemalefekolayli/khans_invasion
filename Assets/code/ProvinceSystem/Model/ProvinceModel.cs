using System.Collections.Generic;
using UnityEngine;

public class ProvinceModel : MonoBehaviour
{
    private const long RiverProvinceId = 7;

    public SpriteRenderer spriteRenderer;
    public string provinceName;
    public long provinceId;
    public Color provinceColor;
    public float provinceTaxIncome;
    public float provinceTradePower;
    public float provinceCurrentPop;
    public float provinceMaxPop;
    public float availableLoot;
    public StateModel provinceState;
    public NationModel provinceOwner;
    public string provinceTag = "Province";
    public List<ProvinceModel> neighbors = new List<ProvinceModel>();
    public List<string> buildings = new List<string>();

    private void OnEnable()
    {
        GameEvents.OnPlayerNationCapitalSet += SwitchSprites;
    }

    private void OnDisable()
    {
        GameEvents.OnPlayerNationCapitalSet -= SwitchSprites;
    }

    public void SwitchSprites(ProvinceModel capitalProvince)
    {
        CityCenter cityCenter = GetComponentInChildren<CityCenter>();
        if (cityCenter != null && capitalProvince == this)
        {
            cityCenter.SwitchSprites();
        }
    }

    private void Awake()
    {
        if (string.IsNullOrEmpty(provinceTag)) return;

        try
        {
            gameObject.tag = provinceId == RiverProvinceId ? "River" : provinceTag;
        }
        catch
        {
            GameLog.Warning(GameLogCategory.Core, $"Tag '{provinceTag}' is not defined yet. Add it in the editor.");
        }
    }

    private void Start()
    {
        EnsureCollider();
    }

    public void EnsureCollider()
    {
        PolygonCollider2D collider = GetComponent<PolygonCollider2D>();
        if (collider == null && spriteRenderer != null && spriteRenderer.sprite != null)
        {
            collider = gameObject.AddComponent<PolygonCollider2D>();
        }

        if (collider != null)
        {
            collider.isTrigger = true;
        }
    }

    public Vector3 GetProvincePosition() => transform.position;

    public void SetNationColor(Color nationColor)
    {
        provinceColor = nationColor;
        if (spriteRenderer != null)
        {
            spriteRenderer.color = nationColor;
        }
    }

    public void SetProvinceName(string newName, bool renameGameObject = true)
    {
        if (string.IsNullOrEmpty(newName)) return;

        provinceName = newName;
        if (renameGameObject)
        {
            gameObject.name = newName;
        }
    }
}
