using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class Miner : MonoBehaviour
{

    [SerializeField] private int area = 2;
    private Vector3 pos = Vector3.zero;

    // Start is called before the first frame update
    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
    }


    // Update is called once per frame
    void Update()
    {
        if (Input.GetMouseButton(0))
        {
            RaycastHit hit;
            Ray ray = GetComponent<Camera>().ScreenPointToRay(Input.mousePosition);
            
            if (Physics.Raycast(ray, out hit)) {
                
                Chunk chunk = hit.transform.GetComponent<Chunk>();
                if (chunk != null)
                {
                    pos = hit.point;
                    chunk.DestroyVoxel(hit.point, area);
                }
            }
        }

        if (Input.GetMouseButton(1))
        {
            RaycastHit hit;
            Ray ray = GetComponent<Camera>().ScreenPointToRay(Input.mousePosition);
            
            if (Physics.Raycast(ray, out hit)) {
                
                Chunk chunk = hit.transform.GetComponent<Chunk>();
                if (chunk != null)
                {
                    pos = hit.point;
                    chunk.CreateVoxel(hit.point, area);
                }
            }
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(1,0,0,0.2f);
        Gizmos.DrawSphere(pos, 0.1f);
    }
}
