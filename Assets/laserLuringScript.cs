using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using KModkit;

public class laserLuringScript : MonoBehaviour {

    public KMBombInfo Bomb;
    public KMAudio Audio;

    public KMSelectable[] Buttons;
    public Light[] Lights;

    //Logging
    static int moduleIdCounter = 1;
    int moduleId;
    private bool moduleSolved;

    void Awake () {
        moduleId = moduleIdCounter++;

        foreach (KMSelectable Button in Buttons) {
            Button.OnInteract += delegate () { ButtonPress(Button); return false; };
        }

        //button.OnInteract += delegate () { buttonPress(); return false; };

    }

    // Use this for initialization
    void Start () {
        float scalar = transform.lossyScale.x; //standard light procedure: all lights must be scaled based on the scale of the bomb
        for (int l = 0; l < 3; l++) {
            Lights[l].range *= scalar;
            Lights[l].gameObject.SetActive(false);
        }


    }

    void ButtonPress(KMSelectable BS) {
        for (int b = 0; b < 3; b++)
        {
            if (Buttons[b] == BS)
            {
                Debug.Log(b); 
            }
        }
    }

    /*
    void buttonPress() {

    }
    */
}
