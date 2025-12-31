using UnityEngine;
using System.Collections.Generic;


[RequireComponent(typeof(Army))]
public class ArmyVisuals : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    
    [Header("Sprite Options")]
    [SerializeField] private List<Sprite> spriteVariants;
    [SerializeField] private bool randomizeOnStart = true;
    
    private Vector3 lastPosition;
    
    private void Awake()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();
        
        lastPosition = transform.position;
    }
    
    private void Start()
    {
        if (randomizeOnStart && spriteVariants != null && spriteVariants.Count > 0)
        {
            RandomizeSprite();
        }
    }
    
    private void LateUpdate()
    {
        UpdateFacing();
        lastPosition = transform.position;
    }
    

    private void UpdateFacing()
    {
        if (spriteRenderer == null) return;
        
        Vector3 delta = transform.position - lastPosition;
        
        if (delta.x > 0.001f)
            spriteRenderer.flipX = false;
        else if (delta.x < -0.001f)
            spriteRenderer.flipX = true;
    }
    

    public void RandomizeSprite()
    {
        if (spriteRenderer == null || spriteVariants == null || spriteVariants.Count == 0)
            return;
        
        int index = Random.Range(0, spriteVariants.Count);
        spriteRenderer.sprite = spriteVariants[index];
    }
    

    public void SetSprite(int index)
    {
        if (spriteRenderer == null || spriteVariants == null)
            return;
        
        if (index >= 0 && index < spriteVariants.Count)
        {
            spriteRenderer.sprite = spriteVariants[index];
        }
    }

    public void SetSprite(Sprite sprite)
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.sprite = sprite;
        }
    }
    

    public void FaceDirection(bool faceRight)
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.flipX = !faceRight;
        }
    }
}