using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyOneController : MonoBehaviour
{
    [SerializeField]float serchRadius = 5.0f;
    [SerializeField]float moveSpeed = 1.0f;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        GameObject player = GameObject.FindWithTag("Player");
        Vector2 thisPos = this.gameObject.transform.position;
        Vector2 playerPos = player.transform.position;
        float distance = Vector2.Distance(thisPos,playerPos);

        if(distance < serchRadius){
            this.transform.position = Vector2.MoveTowards(this.transform.position, playerPos, moveSpeed*Time.deltaTime);
        }
    }

    void OnCollisionEnter2D(Collision2D collision2D){
        if(collision2D.gameObject.tag == "Player"){
            Destroy(this.gameObject);
        }
    }
}
