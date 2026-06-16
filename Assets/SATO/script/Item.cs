using UnityEngine;

public class Item : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D collision)
    {
        // プレイヤーが触れたかの判定
        if (collision.CompareTag("Player"))
        {
            // GameManagerにアイテム取得を伝える
            GameManager.instance.AddItem();

            // アイテム消去
            Destroy(gameObject);
        }
    }
}
