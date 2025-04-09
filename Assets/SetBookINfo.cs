using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class SetBookINfo : MonoBehaviour
{

    public static string info = "Title: Dune\nAuthor: Frank Herbert\nBrief summary: Dune is a science fiction novel set in the distant future amidst a huge interstellar empire. It revolves around the story of Paul Atreides and his noble family's control of the desert planet Arrakis, which is the universe's only source of 'spice', a powerful substance essential for space travel and longevity.\nGenre: Science Fiction";
    [SerializeField] TMP_Text infoText;
    void Start()
    {

        // Start is called before the first frame update
        infoText.text = info;
    }


}
