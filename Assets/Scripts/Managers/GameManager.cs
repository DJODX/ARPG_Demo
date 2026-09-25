using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    // Start is called before the first frame update
    float time = 0;
    void Start()
    {
        if(InventoryManager.Instance.AddItem(2001,1))
        {
            Debug.Log("添加成功");
        }
        else
        {
            Debug.Log("添加失败");
        }


    }

    // Update is called once per frame
    void Update()
    {
        time += Time.deltaTime;    
    }
}
