using UnityEngine;
using System.Collections;


public class fade : MonoBehaviour
{
    public float X = -600.0f;


    public GameObject transitionPanel;

    public IEnumerator endKuroFade()
    {
        for (int n = 0; n < 5; n++)
        {
            if (transitionPanel != null)
            {
                transitionPanel.transform.position += new Vector3(X, 0, 0);
            }
            yield return new WaitForSeconds(0.2f);
        }
    }

    public IEnumerator startKuroFade()
    {
        for (int n = 0; n < 5; n++)
        {
            if (transitionPanel != null)
            {
                transitionPanel.transform.position += new Vector3(-600, 0, 0);
            }
            yield return new WaitForSeconds(0.2f);
        }
    }
}