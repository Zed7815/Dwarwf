using UnityEngine;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public enum GameState
    {
        Edit,
        Play
    }

    public GameState currentState = GameState.Edit;
    public PlayerController player;

    public GameObject startButton;
    public GameObject resetButton;

    void Start()
    {
        SetUI();
    }

    public void StartGame()
    {
        currentState = GameState.Play;
        player.StartMove();
        SetUI();
    }

    public void ResetGame()
    {
        currentState = GameState.Edit;
        player.ResetPosition();
        SetUI();
    }

    void SetUI()
    {
        if (currentState == GameState.Edit)
        {
            startButton.SetActive(true); // スタートボタン表示
            resetButton.SetActive(false); // リセットボタンを非表示
        }

        else
        {
            startButton.SetActive(false); // スタートボタンの非表示
            resetButton.SetActive(true); // リセットボタンを表示
        }
    }
}
