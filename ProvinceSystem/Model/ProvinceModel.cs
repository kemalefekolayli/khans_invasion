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

    public StateModel provinceState;
    public NationModel provinceOwner;
    public string provinceTag = "Province";
    public List<ProvinceModel> neighbors = new List<ProvinceModel>();

    private void Awake()
    {
        // Tag ataması
        if (!string.IsNullOrEmpty(provinceTag))
        {
            try
            {
                gameObject.tag = provinceTag;
            }
            catch
            {
                Debug.LogWarning($"Tag '{provinceTag}' henüz tanımlı değil! Editör'den eklemen lazım.");
            }
        }
    }
    void Start()
    {
        // Eğer collider yoksa, sprite'dan oluştur
        EnsureCollider();

    }

    public void EnsureCollider()
    {
        PolygonCollider2D collider = GetComponent<PolygonCollider2D>();

        if (collider == null && spriteRenderer != null && spriteRenderer.sprite != null)
        {
            // Yeni collider ekle
            collider = gameObject.AddComponent<PolygonCollider2D>();

            // Unity otomatik olarak sprite'dan polygon oluşturur!
            Debug.Log($"✓ {provinceName} için collider oluşturuldu");
        }
        else if (collider != null && spriteRenderer != null && spriteRenderer.sprite != null)
        {
            // Collider varsa ama yanlış olabilir - yenile
            Destroy(collider);
            collider = gameObject.AddComponent<PolygonCollider2D>();
            Debug.Log($"✓ {provinceName} collider'ı yenilendi");
        }
        collider.isTrigger = true;  
    }


    
    // Nation rengi ata
    public void SetNationColor(Color nationColor)
    {
        provinceColor = nationColor;
        if (spriteRenderer != null)
        {
            spriteRenderer.color = nationColor;
        }
    }
}