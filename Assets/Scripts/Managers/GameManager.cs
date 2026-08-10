using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    // Start is called before the first frame update
    float time = 0;
    void Start()
    {
        UIManager.Instance.ShowPanel<BackpackPanel>();
        InventoryManager.Instance.AddItem(1001, 2);
        

    }

    // Update is called once per frame
    void Update()
    {
        time += Time.deltaTime;
        
    }
}
