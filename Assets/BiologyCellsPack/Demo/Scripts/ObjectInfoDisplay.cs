using UnityEngine;
using TMPro;

public class ObjectInfoDisplay : MonoBehaviour
{
    public TextMeshProUGUI infoText;

    void Start()
    {
        if (infoText != null) infoText.gameObject.SetActive(false);
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit))
            {
                Debug.Log("Hit: " + hit.collider.gameObject.name);
                ObjectInfo info = hit.collider.GetComponent<ObjectInfo>();
                if (info != null)
                {
                    infoText.text = info.description;
                    infoText.gameObject.SetActive(true);
                }
                else
                {
                    infoText.gameObject.SetActive(false);
                }
            }
            else
            {
                infoText.gameObject.SetActive(false);
            }
        }
    }
}
