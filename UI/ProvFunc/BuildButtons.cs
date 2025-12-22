using UnityEngine;

public class BuildButtons : MonoBehaviour
{
     private Builder builder;

     void Start()
    {
        Builder builder = new Builder();
        this.builder = builder;
    }

    
}