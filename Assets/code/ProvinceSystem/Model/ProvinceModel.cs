using UnityEngine;
using System.Collections.Generic;

public class ProvinceModel : MonoBehaviour
{
    // Province 7 is the river tile: tagged "River", excluded from province_data.json and nation assignment
    private const long RiverProvinceId = 7;

    public SpriteRenderer spriteRenderer;
    
    public string provinceName;
    public long provinceId;
    public Color provinceColor;

    public float provinceTaxIncome;
    public float provinceTradePower;
    public float provinceCurrentPop;
    public float provinceMaxPop;
    public float availableLoot; // we will make this recover after every turn 



    public StateModel provinceState;
    public NationModel provinceOwner;
    public string provinceTag = "Province";
    public List<ProvinceModel> neighbors = new List<ProvinceModel>();
    public List<string> buildings = new List<string>();

    void OnEnable()
    {
        GameEvents.OnPlayerNationCapitalSet += SwitchSprites;
    }

    void OnDisable()
    {
        GameEvents.OnPlayerNationCapitalSet -= SwitchSprites;
    }

    public void SwitchSprites(ProvinceModel capitalProvince)
    {
       CityCenter cityCenter = GetComponentInChildren<CityCenter>();
         if (cityCenter != null && capitalProvince == this)
         {
            GameLog.Log(GameLogCategory.Core, $"[ProvinceModel] 1234 Switching sprites for capital province: {provinceName}");
            cityCenter.SwitchSprites();
         }
    }
    private void Awake()
    {
        // Tag ataması
        if (!string.IsNullOrEmpty(provinceTag))
        {
            try
            {   
                if(provinceId == RiverProvinceId)
                {
                    gameObject.tag = "River";
                }else
                {
                    gameObject.tag = provinceTag;
                }
                
            }
            catch
            {
                GameLog.Warning(GameLogCategory.Core, $"Tag '{provinceTag}' henüz tanımlı değil! Editör'den eklemen lazım.");
            }
        }
    }
    void Start()
    {
        EnsureCollider();

    }

    public void EnsureCollider()
    {
        PolygonCollider2D collider = GetComponent<PolygonCollider2D>();

        if (collider == null && spriteRenderer != null && spriteRenderer.sprite != null)
        {
            // Yeni collider ekle
            collider = gameObject.AddComponent<PolygonCollider2D>();
        }

        if (collider != null)
        {
            collider.isTrigger = true;
        }
    }

    public Vector3 GetProvincePosition()
    {
        return transform.position;
    }
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
