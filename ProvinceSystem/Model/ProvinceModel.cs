using UnityEngine;
using System.Collections.Generic;

public class ProvinceModel : MonoBehaviour
{
    public SpriteRenderer spriteRenderer;
    
    public string provinceName;
    public long provinceId;
    public Color provinceColor;

    public float provinceTaxIncome;
    public float provinceTradePower;
    public float provinceCurrentPop;
    public float provinceMaxPop;
    public float availableLoot; // we will make this recover after every turn 
    public float defenceForceSize;
    public float defenceForceStr;


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
                if(provinceId == 7)
                {
                    gameObject.tag = "River";
                }else
                {
                    gameObject.tag = provinceTag;
                }
                
            }
            catch
            {
                Debug.LogWarning($"Tag '{provinceTag}' henüz tanımlı değil! Editör'den eklemen lazım.");
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
        else if (collider != null && spriteRenderer != null && spriteRenderer.sprite != null)
        {
            // Collider varsa ama yanlış olabilir - yenile
            Destroy(collider);
            collider = gameObject.AddComponent<PolygonCollider2D>();

        }
        collider.isTrigger = true;  
    }


    public void SetNationColor(Color nationColor)
    {
        provinceColor = nationColor;
        if (spriteRenderer != null)
        {
            spriteRenderer.color = nationColor;
        }
    }
}