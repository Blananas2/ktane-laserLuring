using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using KModkit;
using Rnd = UnityEngine.Random;

public class laserLuringScript : MonoBehaviour {

    public KMBombInfo Bomb;
    public KMAudio Audio;

    public KMSelectable[] Buttons;
    public Light[] Lights;
    public Color[] Hues; //WYMRCGB
    public Sprite[] CatSprites; //See spriteSpec.txt for how these are set up!
    public Sprite[] ItemSprites;
    public Sprite[] FurnatureSprites;
    public Sprite[] OtherSprites;
    public SpriteRenderer[] Slots;

    float LEFT_EDGE = -0.0806f;
    float TOP_EDGE = 0.0431f;
    float GRID_SQ = 0.00575888f;
    int SQ_ACROSS = 29;
    int SQ_TALL = 23;

    //Logging
    static int moduleIdCounter = 1;
    int moduleId;
    private bool moduleSolved;

    void Awake () {
        moduleId = moduleIdCounter++;

        foreach (KMSelectable Button in Buttons) {
            Button.OnHighlight += delegate () { /*ButtonHover(Button);*/ };
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

    void ButtonPress(KMSelectable BS)
    {
        for (int b = 0; b < 3; b++) {
            if (Buttons[b] == BS)
            {
                Lights[b].gameObject.SetActive(true);
                int xx = Rnd.Range(0, SQ_ACROSS);
                int yy = Rnd.Range(0, SQ_TALL);
                //SetSprite(xx, yy, 5, Slots[0], Sprites[0], Color.HSVToRGB(1f, 1f, 1f), b == 0);
                //Debug.Log("x:" + xx + " y:" + yy);
            } 
        }
    }

    /*
    void buttonPress() {

    }
    */
    
    void SetSprite(float xp, float yp, int zp, SpriteRenderer slot, Sprite spr, Color col, bool fx)
    {
        slot.sprite = spr;
        slot.gameObject.transform.localPosition = new Vector3(LEFT_EDGE + xp * GRID_SQ, 0.0103f, TOP_EDGE - yp * GRID_SQ);
        slot.sortingOrder = zp;
        slot.color = col;
        slot.flipX = fx;
    }
}
