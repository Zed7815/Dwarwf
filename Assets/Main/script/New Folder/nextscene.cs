using UnityEngine;
using System.Collections;
public class nextscene : MonoBehaviour
{
    public GameObject GameObject;
    
    public IEnumerator kuro()
    {
        for (int n = 0; n < 5; n++)
        {
            GameObject.transform.position += new Vector3(0, -365, 0);
            yield return new WaitForSeconds(0.5f);
        }
    }
}
