using UnityEngine;
public enum moveState_main
{
    idol,
    straight,
    jump,

}
public class Player_main : MonoBehaviour
{
    [Header("プレイヤーの数値")]
    public float PlayerSpeed = 5.0f;
    moveState state = moveState.idol;
    // Update is called once per frame
    void Update()
    {
        switch (state)
        {
            case moveState.idol:
                stay();
                break;

            case moveState.straight:
                walk();
                break;

            case moveState.jump:

                break;

        }

    }
    void stay()
    {

    }
    void walk()
    {
        transform.Translate(Vector2.right * PlayerSpeed * Time.deltaTime);
    }



    public void StateChange(int  n)
    {
        switch (n)
        {
            case 0:
                state = moveState.idol;
                break;

            case 1:
                state = moveState.straight;
                break;
            case 2:
                state = moveState.jump;
                break;
        }
    }
}
