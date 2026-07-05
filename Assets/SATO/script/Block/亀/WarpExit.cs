using UnityEngine;

public class WarpExit : MonoBehaviour
{
    [HideInInspector] public Animator exitAnimator;

    private void Awake()
    {
        exitAnimator = GetComponent<Animator>();
    }
}
