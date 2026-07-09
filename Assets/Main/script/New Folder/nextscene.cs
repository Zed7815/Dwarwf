using UnityEngine;
using System.Collections;


public class nextscene : MonoBehaviour
{
    public float Y = -600.0f; 


    public GameObject transitionPanel;

    public IEnumerator endKuro()
    {
        for (int n = 0; n < 5; n++)
        {
            if (transitionPanel != null)
            {
                transitionPanel.transform.position += new Vector3(0, Y, 0);
            }
            yield return new WaitForSeconds(0.2f);
        }
    }

    public IEnumerator startKuro()
    {
        for (int n = 0; n < 5; n++)
        {
            if (transitionPanel != null)
            {
                transitionPanel.transform.position += new Vector3(0, -600, 0);
            }
            yield return new WaitForSeconds(0.2f);
        }
    }
}