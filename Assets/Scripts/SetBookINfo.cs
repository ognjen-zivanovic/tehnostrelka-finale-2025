using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class SetBookINfo : MonoBehaviour
{

    public static string info;
    [SerializeField] TMP_Text infoText;
    [SerializeField] TMP_Text infoText2;

    void Start()
    {
        infoText.text = info;
        infoText2.text = info;
    }


}
