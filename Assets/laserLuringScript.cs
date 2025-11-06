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
    public Sprite[] CatSprites; //See spriteSpec.txt for how these are set up!
    public Sprite[] ItemSprites;
    public Sprite[] FurnatureSprites;
    public Sprite[] OtherSprites;
    public SpriteRenderer[] Slots;

    string[] CAT_NAMES = { "Cleo", "Dart", "Finn", "Lena", "Nuki", "Remi", "Scar", "Tiki", "Vivi", "Wind" };
    string[] COLOR_NAMES = { "black", "blue", "green", "cyan", "red", "magenta", "yellow", "white" };
    Color[] COLORS_PROPER = { Color.black, Color.blue, Color.green, Color.cyan, Color.red, Color.magenta, Color.yellow, Color.white };
    int[] ChosenCats;
    int[] ChosenCollars = { -1, -1, -1 };
    int[] CatPosX = { -1, -1, -1 };
    int[] CatPosY = { 17, 17, 17 };
    bool[] CatFacing = { false, false, false };

    float LEFT_EDGE = -0.0806f;
    float TOP_EDGE = 0.0431f;
    float GRID_SQ = 0.00575888f;
    int SQ_ACROSS = 29;
    int SQ_TALL = 23;
    int FLOOR = 18;
    int ITEM_LOWEST = 15;
    int CAT_RANGE = 4;

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

        for (int p = 0; p < 3; p++) { CatPosX[p] = Rnd.Range(3, 26); } //gen three initial positions, these are delicately chosen so they can be manipulated while of course staying in view
        Array.Sort(CatPosX); //put them in ascending order
        CatPosX[0] -= 2; //bump the end cats so the cats can't overlap
        CatPosX[2] += 2;

        GeneratePuzzle();
        //calc favorites
        for (int m = 0; m < 3; m++)
        {
            bool flipEm = Rnd.Range(0, 2) == 0;
            CatFacing[m] = flipEm;
            SetSprite(CatPosX[m], 17, 3+m, Slots[m], CatSprites[ChosenCats[m] * 10], Color.white, flipEm);
            SetSprite(CatPosX[m], 17, 6, Slots[m + 3], OtherSprites[1], COLORS_PROPER[ChosenCollars[m]], flipEm);
        }
        //generate room
    }

    void ButtonPress(KMSelectable BS)
    {
        for (int btn = 0; btn < 3; btn++)
        {
            if (Buttons[btn] == BS)
            {
                Lights[btn].gameObject.SetActive(true);
                Lights[(btn+1)%3].gameObject.SetActive(false);
                Lights[(btn+2)%3].gameObject.SetActive(false);
                /*
                int xx = Rnd.Range(0, SQ_ACROSS);
                int yy = Rnd.Range(0, SQ_TALL);
                //SetSprite(xx, yy, 5, Slots[0], Sprites[0], Color.HSVToRGB(1f, 1f, 1f), b == 0);
                //Debug.Log("x:" + xx + " y:" + yy);
                */
            }
        }
    }

    void GeneratePuzzle()
    {
        int attps = 1;
        int attpsButLess = 0;
    retry:
        ChosenCats = Enumerable.Range(0, 10).ToArray().Shuffle().Take(3).ToArray();
        int[] powersOfTwo = { 4, 2, 1 }; //lazy but idgaf
        powersOfTwo = powersOfTwo.Shuffle();
        for (int q = 0; q < 3; q++) { ChosenCollars[q] = Rnd.Range(0, 8) | powersOfTwo[q]; } //the bitwise or ensures that each of the lasers can be used for different cats
        if (ChosenCollars[0] == ChosenCollars[1] || ChosenCollars[0] == ChosenCollars[2] || ChosenCollars[1] == ChosenCollars[2]) { attpsButLess++; goto retry; }
        int[] catInitSplit = CalcInits(ChosenCats, ChosenCollars);
        int[] catInits = { catInitSplit[0] * 8 + catInitSplit[3], catInitSplit[1] * 8 + catInitSplit[4], catInitSplit[2] * 8 + catInitSplit[5] };
        if (catInits[0] == catInits[1] || catInits[0] == catInits[2] || catInits[1] == catInits[2] ||
            catInitSplit[0] > 7 || catInitSplit[1] > 7 || catInitSplit[2] > 7 || catInitSplit[3] > 7 || catInitSplit[4] > 7 || catInitSplit[5] > 7 ||
            catInitSplit[0] < 0 || catInitSplit[1] < 0 || catInitSplit[2] < 0 || catInitSplit[3] < 0 || catInitSplit[4] < 0 || catInitSplit[5] < 0)
        {
            attps++;
            Debug.Log("no: " + ChosenCats.Join(",") + " " + ChosenCollars.Join(",") + " " + catInitSplit.Join(","));
            goto retry;
        }
        
        Debug.LogFormat("<Laser Luring #{0}> Attempts: {1}{2}", moduleId, attps, attpsButLess == 0 ? "" : "." + attps);
        Debug.LogFormat("[Laser Luring #{0}] Cats: {1} {2}, {3} {4}, {5} {6}", moduleId, CAT_NAMES[ChosenCats[0]], COLOR_NAMES[ChosenCollars[0]], CAT_NAMES[ChosenCats[1]], COLOR_NAMES[ChosenCollars[1]], CAT_NAMES[ChosenCats[2]], COLOR_NAMES[ChosenCollars[2]]);
        Debug.LogFormat("[Laser Luring #{0}] {1}'s initial position: Row {2}, Column {3}", moduleId, CAT_NAMES[ChosenCats[0]], catInitSplit[0], catInitSplit[3]);
        Debug.LogFormat("[Laser Luring #{0}] {1}'s initial position: Row {2}, Column {3}", moduleId, CAT_NAMES[ChosenCats[1]], catInitSplit[1], catInitSplit[4]);
        Debug.LogFormat("[Laser Luring #{0}] {1}'s initial position: Row {2}, Column {3}", moduleId, CAT_NAMES[ChosenCats[2]], catInitSplit[2], catInitSplit[5]);
    }
    
    int[] CalcInits(int[] catV, int[] colV)
    {
        bool[] CAT_GEN = { true, false, false, true, true, false, false, false, true, true };
        string VOWELS = "AEIOU";

        int[] initCols = { -1, -1, -1 };
        int[] initRows = { -1, -1, -1 };

        for (int i = 0; i < 3; i++)
        {
            switch (catV[i])
            {
                case 0: //Cleo
                    int[] cleoTrack = { -1, -1 };
                    for (int a = 0; a < 3; a++)
                    {
                        if (catV[a] == 0) { continue; }
                        cleoTrack[cleoTrack[0] == -1 ? 0 : 1] = colV[a];
                    }
                    initCols[i] = cleoTrack[0];
                    initRows[i] = cleoTrack[1];
                    break;
                case 1: //Dart
                    string dartConcat = "";
                    int dartVowelCount = 0;
                    string dartConsonants = "";
                    int dartConsCount = 0;
                    for (int b = 0; b < 3; b++) { dartConcat += CAT_NAMES[catV[b]].ToUpper(); }
                    for (int d = 0; d < dartConcat.Length; d++)
                    {
                        if (VOWELS.Contains(dartConcat[d]))
                        {
                            dartVowelCount++;
                        }
                        else if (!dartConsonants.Contains(dartConcat[d]))
                        {
                            dartConsonants += dartConcat[d];
                            dartConsCount++;
                        }
                    }
                    initCols[i] = dartVowelCount;
                    initRows[i] = dartConsCount;
                    break;
                case 2: //Finn
                    int[] FINN_FIRSTS = { 3, 4, -1, 12, 14, 18, 19, 20, 22, 23 };
                    int[] FINN_LASTS = { 15, 20, -1, 1, 9, 9, 18, 9, 9, 4 };
                    int[] finnTrack = { -1, -1 };
                    for (int e = 0; e < 3; e++)
                    {
                        if (catV[e] == 2) { continue; }
                        finnTrack[finnTrack[0] == -1 ? 0 : 1] = catV[e];
                    }
                    initRows[i] = Math.Abs(FINN_FIRSTS[finnTrack[0]] - FINN_FIRSTS[finnTrack[1]]) % 8;
                    initCols[i] = Math.Abs(FINN_LASTS[finnTrack[0]] - FINN_LASTS[finnTrack[1]]) % 8;
                    break;
                case 3: //Lena
                    initRows[i] = (CAT_GEN[catV[0]] ? 4 : 0) + (CAT_GEN[catV[1]] ? 2 : 0) + (CAT_GEN[catV[2]] ? 1 : 0);
                    initCols[i] = (CAT_GEN[catV[2]] ? 4 : 0) + (CAT_GEN[catV[1]] ? 2 : 0) + (CAT_GEN[catV[0]] ? 1 : 0);
                    break;
                case 4: //Nuki
                    int nukiColor = -1;
                    int[] nukiTrack = { -1, -1 };
                    for (int f = 0; f < 3; f++)
                    {
                        if (catV[f] == 4)
                        {
                            nukiColor = colV[f];
                        }
                        else
                        {
                            nukiTrack[nukiTrack[0] == -1 ? 0 : 1] = colV[f];
                        }
                    }
                    initRows[i] = nukiColor ^ nukiTrack[0];
                    initCols[i] = nukiColor ^ nukiTrack[1];
                    break;
                case 5: //Remi
                    if (catV[0] < catV[1] && catV[1] < catV[2] && catV[0] < catV[2]) //ABC
                    { initCols[i] = 6; initRows[i] = 4; }
                    if (catV[0] < catV[2] && catV[2] < catV[1] && catV[0] < catV[1]) //ACB
                    { initCols[i] = 5; initRows[i] = 0; }
                    if (catV[1] < catV[0] && catV[0] < catV[2] && catV[1] < catV[2]) //BAC
                    { initCols[i] = 4; initRows[i] = 7; }
                    if (catV[1] < catV[2] && catV[2] < catV[0] && catV[1] < catV[0]) //CAB
                    { initCols[i] = 2; initRows[i] = 5; }
                    if (catV[2] < catV[0] && catV[0] < catV[1] && catV[2] < catV[1]) //BCA
                    { initCols[i] = 3; initRows[i] = 2; }
                    if (catV[2] < catV[1] && catV[1] < catV[0] && catV[2] < catV[0]) //CBA
                    { initCols[i] = 1; initRows[i] = 6; }
                    break;
                case 6: //Scar
                    int[] scarTrack = { -1, -1 };
                    for (int g = 0; g < 3; g++)
                    {
                        if (catV[g] == 6) { continue; }
                        scarTrack[scarTrack[0] == -1 ? 0 : 1] = colV[g];
                    }
                    initCols[i] = COLOR_NAMES[scarTrack[0]].Length;
                    initRows[i] = COLOR_NAMES[scarTrack[1]].Length;
                    break;
                case 7: //Tiki
                    int[] TIKI_LIST = { 6, 5, 1, 0, 7, 4, 2, -1, 8, 3 };
                    int[] tikiTrack = { -1, -1 };
                    for (int h = 0; h < 3; h++)
                    {
                        if (catV[h] == 7) { continue; }
                        tikiTrack[tikiTrack[0] == -1 ? 0 : 1] = catV[h];
                    }
                    initRows[i] = Math.Abs(TIKI_LIST[tikiTrack[0]] - TIKI_LIST[tikiTrack[1]]) - 1;
                    initCols[i] = initRows[i];
                    break;
                case 8: //Vivi
                    string[] viviTrack = { "", "" };
                    for (int j = 0; j < 3; j++)
                    {
                        if (catV[j] == 8) { continue; }
                        viviTrack[viviTrack[0] == "" ? 0 : 1] = CAT_NAMES[catV[j]].ToUpper();
                    }
                    initRows[i] = (VOWELS.Contains(viviTrack[0][1]) ? 4 : 0) + (VOWELS.Contains(viviTrack[0][2]) ? 2 : 0) + (VOWELS.Contains(viviTrack[0][3]) ? 1 : 0);
                    initCols[i] = (VOWELS.Contains(viviTrack[1][1]) ? 4 : 0) + (VOWELS.Contains(viviTrack[1][2]) ? 2 : 0) + (VOWELS.Contains(viviTrack[1][3]) ? 1 : 0);
                    break;
                default: //Wind
                    int windPosition = -1;
                    int[] windTrack = { -1, -1 };
                    for (int k = 0; k < 3; k++)
                    {
                        if (catV[k] == 9)
                        {
                            windPosition = k + 1;
                        }
                        else
                        {
                            windTrack[windTrack[0] == -1 ? 0 : 1] = CAT_GEN[catV[k]] ? 1 : 0;
                        }
                    }
                    initCols[i] = windTrack[0] == windTrack[1] ? windPosition * 2 : windPosition;
                    initRows[i] = 7 - initCols[i];
                    break;
            }
        }

        return new int[] { initRows[0], initRows[1], initRows[2], initCols[0], initCols[1], initCols[2] };
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
