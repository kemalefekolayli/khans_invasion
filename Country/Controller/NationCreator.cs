using UnityEngine;

public class NationCreator : MonoBehaviour {
    public GameObject nationPrefab;
    public NationLoader loader;

    void Start() {
        foreach (var m in loader.allNations) {
            var obj = Instantiate(nationPrefab);
            obj.GetComponent<NationController>().model = m;
        }
    }
}
