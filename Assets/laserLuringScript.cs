using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using KModkit;
using Rnd = UnityEngine.Random;

public class laserLuringScript : MonoBehaviour //many many variable names in this script are nonsensical, i'm so sorry that this is how my brain operates
{
    public KMBombModule Module;
    public KMBombInfo Bomb;
    public KMAudio Audio;

    public KMSelectable[] Buttons;
    public KMSelectable[] TargetSels;
    public Light[] Lights;
    public Sprite[] CatSprites; //See spriteSpec.txt for how these are set up!
    public Sprite[] ItemSprites;
    public Sprite[] OtherSprites;
    public SpriteRenderer[] Slots;

    private static readonly string[] CAT_NAMES = { "Cleo", "Dart", "Finn", "Lena", "Nuki", "Remi", "Scar", "Tiki", "Vivi", "Wind" };
    private static readonly string[] COLOR_NAMES = { "black", "blue", "green", "cyan", "red", "magenta", "yellow", "white" };
    private static readonly Color[] COLORS_PROPER = { Color.black, Color.blue, Color.green, Color.cyan, Color.red, Color.magenta, Color.yellow, Color.white };
    int[] ChosenCats;
    int[] ChosenCollars = { -1, -1, -1 };
    int[] CatPosX = { -1, -1, -1 };
    int[] CatPosY = { 18, 18, 18 };
    bool[] CatFacing = { false, false, false }; //left = true
    int? LaserColor = null;
    int TargetCycle; //the number of cats for the current laser color that need to be cycled through

    private const float LEFT_EDGE = -0.0806f;
    private const float TOP_EDGE = 0.0431f;
    private const float GRID_SQ = 0.00575888f;
    private const int SQ_ACROSS = 29;
    private const int SQ_TALL = 19;
    private const int IN_AIR_DURATION = 10;
    private const int SHELF_COUNT = 9;
    private const int CURSOR_LIMIT = 9; //increase if i need it to; just ensure i also add the correct number of slots and targetsels
    string[] ITEM_NAMES = { "vase of flowers", "Wither painting", "trophy", "piggy bank", "not X01 dartboard", "water bottle", "alarm clock", "American flag", "Rubik's cube", "spray bottle", "soda can", "microphone", "VVVVVV", "letter board", "fez", "bobblehead", "Rubik's clock", "rubber duck", "wooden blocks", "meeple", "strawberry jam", "Unown F", "jar with coins", "hard hat", "joker card", "toy ship", "Zed dog", "crewmate", "whiteboard", "peashooter", "PlayStation 5", "birthday sign", "pocket watch", "baseball", "Wii remote", "xmas mat", "jewel", "creeper", "walkie-talkie", "Luxo ball", "toy tank", "maneater", "rum and glass", "deny stamp", "approve stamp", "top hat", "calendar", "chicken", "watering can", "balloon", "glasses", "toy car", "flask", "drinking bird", "oppie", "C tile", "dust bunny", "Maxwell's notebook", "Mort the chicken", "triforce", "portable lantern", "wireframe ball", "20 tile", "infinity gauntlet" };
    int[][] itemWH = new int[][] {
        new int[] {1,3}, new int[] {2,2}, new int[] {2,2}, new int[] {2,1}, new int[] {3,3}, new int[] {1,1}, new int[] {2,1}, new int[] {1,2},
        new int[] {1,1}, new int[] {1,2}, new int[] {1,1}, new int[] {1,1}, new int[] {1,2}, new int[] {2,2}, new int[] {1,1}, new int[] {1,1},
        new int[] {1,1}, new int[] {1,1}, new int[] {1,1}, new int[] {1,1}, new int[] {1,1}, new int[] {1,1}, new int[] {1,1}, new int[] {2,1},
        new int[] {1,1}, new int[] {1,1}, new int[] {1,1}, new int[] {1,1}, new int[] {3,2}, new int[] {1,1}, new int[] {1,3}, new int[] {3,1},
        new int[] {1,1}, new int[] {1,1}, new int[] {1,2}, new int[] {2,1}, new int[] {1,1}, new int[] {1,1}, new int[] {1,2}, new int[] {2,2},
        new int[] {1,1}, new int[] {1,1}, new int[] {2,3}, new int[] {1,1}, new int[] {1,1}, new int[] {2,1}, new int[] {3,2}, new int[] {1,1},
        new int[] {1,1}, new int[] {1,2}, new int[] {2,1}, new int[] {1,1}, new int[] {1,1}, new int[] {1,3}, new int[] {1,1}, new int[] {1,1},
        new int[] {1,1}, new int[] {1,1}, new int[] {1,2}, new int[] {1,1}, new int[] {2,3}, new int[] {2,2}, new int[] {1,1}, new int[] {1,2} 
    };
    int[] IAmScheming = { 2, 3, 5, 7, 11, 13, 17 };
    int[] conv = { 36, 32, 37, 33, 38, 34, 39, 35, 4, 0, 5, 1, 6, 2, 7, 3, 44, 40, 45, 41, 46, 42, 47, 43, 12, 8, 13, 9, 14, 10, 15, 11, 52, 48, 53, 49, 54, 50, 55, 51, 20, 16, 21, 17, 22, 18, 23, 19, 60, 56, 61, 57, 62, 58, 63, 59, 28, 24, 29, 25, 30, 26, 31, 27 };
    int[] itemIxs = { -1, -1, -1 };
    int[][] orders = new int[][] { new int[] { 0, 1, 2 }, new int[] { 0, 2, 1 }, new int[] { 1, 0, 2 }, new int[] { 1, 2, 0 }, new int[] { 2, 0, 1 }, new int[] { 2, 1, 0 } };
    int orderIx = -1;
    private int[] cyclingCursorColors = new int[CURSOR_LIMIT];
    bool animating = false;
    List<int> TargX = new List<int> { };
    List<int> TargY = new List<int> { };
    List<int> TargCol = new List<int> { };
    private int[] ShelfPositions = new int[SQ_ACROSS * SQ_TALL];
    bool[] catSatisfaction = { false, false, false };

    //Logging
    static int moduleIdCounter = 1;
    int moduleId;
    private bool moduleSolved;

    void Awake()
    {
        moduleId = moduleIdCounter++;

        foreach (KMSelectable Button in Buttons)
        {
            Button.OnInteract += delegate () { ButtonPress(Button); return false; };
        }

        foreach (KMSelectable TargetSel in TargetSels)
        {
            TargetSel.OnInteract += delegate () { TargetPress(TargetSel); return false; };
        }
    }

    // Use this for initialization
    void Start()
    {
        float scalar = transform.lossyScale.x; //standard light procedure: all lights must be scaled based on the scale of the bomb
        for (int l = 0; l < 3; l++)
        {
            Lights[l].range *= scalar;
            Lights[l].gameObject.SetActive(false);
        }

        for (int ts = 0; ts < CURSOR_LIMIT; ts++)
        {
            TargetSels[ts].gameObject.SetActive(false);
        }

        orderIx = Rnd.Range(0, 6);

        for (int p = 0; p < 3; p++) { CatPosX[p] = Rnd.Range(4, 25); } //gen three initial positions, these are delicately chosen so they can be manipulated while of course staying in view
        Array.Sort(CatPosX); //put them in ascending order
        CatPosX[0] -= 3; //bump the end cats so the cats can't overlap
        CatPosX[2] += 3;

        GeneratePuzzle();

        for (int m = 0; m < 3; m++)
        {
            bool flipEm = Rnd.Range(0, 2) == 0;
            CatFacing[m] = flipEm;
            SetSprite(CatPosX[m], 17, 3 + m * 2, Slots[m], CatSprites[ChosenCats[m] * 10], Color.white, flipEm, false);
            SetSprite(CatPosX[m], 17, 4 + m * 2, Slots[m + 3], OtherSprites[1], COLORS_PROPER[ChosenCollars[m]], flipEm, false);
        }

        ShelfPositions = GenerateShelves();
        Debug.LogFormat("<Laser Luring #{0}> Shelves: {1}", moduleId, ShelfPositions.Select(i => GetCoord(i)).Join(" "));
        for (int s = 0; s < 9; s++)
        {
            int flipIt = Rnd.Range(0, 4);
            SetSprite(ShelfPositions[s] % SQ_ACROSS, ShelfPositions[s] / SQ_ACROSS, 1, Slots[9 + s], OtherSprites[6], Color.white, flipIt % 2 == 0, flipIt > 1);
        }
    }

    private string GetCoord(int num)
    {
        return (num % SQ_ACROSS).ToString() + "," + (num / SQ_ACROSS);
    }

    void ButtonPress(KMSelectable BS)
    {
        BS.AddInteractionPunch(0.5f);
        if (moduleSolved) { return; }
        for (int btn = 0; btn < 3; btn++)
        {
            if (Buttons[btn] == BS)
            {
                if (btn == LaserColor) { continue; } //prevents a cat turning around if you select the same as it was previously
                Lights[btn].gameObject.SetActive(true);
                LaserColor = btn;

                int[] catOrd = orders.PickRandom();
                for (int nk = 0; nk < 3; nk++)
                {
                    int catplant = catOrd[nk];
                    
                    int[][] whatthefuck = {
                        new int[] { 4, 5, 7, 6 },
                        new int[] { 2, 3, 7, 6 },
                        new int[] { 1, 3, 5, 7 }
                    };
                    if (whatthefuck[btn].Contains(ChosenCollars[catplant])) //check for if cat has the right component; it may have taken one entire crashout to reach this point
                    {
                        CatFacing[catplant] = !CatFacing[catplant];
                        SetSprite(CatPosX[catplant], CatPosY[catplant] - 1, 3 + catplant * 2, Slots[catplant], CatSprites[ChosenCats[catplant] * 10], Color.white, CatFacing[catplant], false);
                        SetSprite(CatPosX[catplant], CatPosY[catplant] - 1, 4 + catplant * 2, Slots[catplant + 3], OtherSprites[1], COLORS_PROPER[ChosenCollars[catplant]], CatFacing[catplant], false);
                        if (catSatisfaction[catplant])
                        {
                            SetSprite(CatPosX[catplant], CatPosY[catplant] - 1, 9, Slots[6 + catplant], OtherSprites[5], Color.white, CatFacing[catplant], false);
                        }
                        break;
                    }
                }

                PlaceTargets(btn);
            }
            else
            {
                Lights[btn].gameObject.SetActive(false);
            }
        }
    }

    void PlaceTargets(int chnl)
    {
        TargX.Clear();
        TargY.Clear();
        TargCol.Clear();

        for (int kitn = 0; kitn < 3; kitn++)
        {
            if (catSatisfaction[kitn]) { continue; }
            int cc = ChosenCollars[kitn];
            
            if ((cc & (int)Math.Pow(2, 2-chnl)) == (int)Math.Pow(2, 2-chnl)) //collar check; all i had to do in previous crashout was this hsjkdhsadjkdhsjkal
            {
                //if the cat is on the ground (posY == 18), place a target 3 spaces in front (take into account facing direction; ENSURE CAT CAN'T ESCAPE ROOM THAT WOULD BE BAD)
                if (CatPosY[kitn] == 18)
                {
                    if ((CatFacing[kitn] && CatPosX[kitn] < 4) || (!CatFacing[kitn] && CatPosX[kitn] > 24)) { continue; } //oob check
                    
                    TargX.Add(CatPosX[kitn] + (CatFacing[kitn] ? -3 : 3));
                    TargY.Add(18);
                    TargCol.Add(cc);
                } else //othw put a target on the floor
                {
                    if ((CatFacing[kitn] && CatPosX[kitn] == 0) || (!CatFacing[kitn] && CatPosX[kitn] == 28)) { continue; } //oob check

                    TargX.Add(CatPosX[kitn] + (CatFacing[kitn] ? -1 : 1));
                    TargY.Add(18);
                    TargCol.Add(cc);
                }

                for (int shelf = 0; shelf < SHELF_COUNT; shelf++)
                {
                    int shx = ShelfPositions[shelf] % SQ_ACROSS;
                    int shy = ShelfPositions[shelf] / SQ_ACROSS;
                    
                    //if the cat is on a shelf with an item (shelf ix < 3), place a target on the item (1 tile above shelf)
                    if (shelf < 3 && CatPosY[kitn] == shy - 1 && Math.Abs(CatPosX[kitn] - shx) < 3 && 
                    ((CatFacing[kitn] && shx < CatPosX[kitn]) || (!CatFacing[kitn] && shx > CatPosX[kitn])))
                    {
                        TargX.Add(shx);
                        TargY.Add(shy-1);
                        TargCol.Add(cc);
                    }

                    //if the cat is on one end of the shelf, facing towards the other end, place a target at the other end
                    if (CatPosY[kitn] == shy - 1 && CatPosX[kitn] == shx + (CatFacing[kitn] ? 2 : -2))
                    {
                        TargX.Add(shx + (CatFacing[kitn] ? -2 : 2));
                        TargY.Add(shy-1);
                        TargCol.Add(cc);
                    }

                    if ((CatFacing[kitn] && (shx < CatPosX[kitn] - 3)) || (!CatFacing[kitn] && (shx > CatPosX[kitn] + 3)))
                    {
                        //if manhattan distance between cat's current position and the target position is within duration range, add the target
                        if ((Math.Abs((shx + (CatFacing[kitn] ? 2 : -2)) - CatPosX[kitn]) + Math.Abs((shy - 1) - CatPosY[kitn])) <= IN_AIR_DURATION)
                        {
                            TargX.Add(shx + (CatFacing[kitn] ? 2 : -2));
                            TargY.Add(shy - 1);
                            TargCol.Add(cc);
                        }
                    }
                }
            }
        }

        List<int> Occupied = new List<int> { };
        List<int> Buckets = new List<int> { };
        List<int> Scheme = new List<int> { };
        List<int> Six = new List<int> { };
        
        //Debug.Log(TargX.Join(",") + " : " + TargY.Join(",") + " : " + TargCol.Join(","));
        if (TargX.Count() > CURSOR_LIMIT)
        {
            Debug.LogFormat("[Laser Luring #{0}] ALERT BLAN OF ISSUE: CURSOR_LIMIT needs to be raised to at least {1}", moduleId, TargX.Count());
        }
        for (int twerp = 0; twerp < CURSOR_LIMIT; twerp++)
        {
            if (twerp >= TargX.Count())
            {
                SetSprite(-1, -1, 7, Slots[21+twerp], null, Color.white, false, false);
                cyclingCursorColors[twerp] = -1;
                TargetSels[twerp].gameObject.SetActive(false);
                continue;
            }

            int XY = TargX[twerp] + TargY[twerp] * SQ_ACROSS;

            if (Occupied.Contains(XY)) {
                SetSprite(-1, -1, 7, Slots[21+twerp], null, Color.white, false, false);
                Buckets[Occupied.IndexOf(XY)]++;
                Scheme[Occupied.IndexOf(XY)] *= IAmScheming[TargCol[twerp]-1];
                cyclingCursorColors[twerp] = -1;
            } else 
            {
                SetSprite(TargX[twerp], TargY[twerp], 9, Slots[21+twerp], OtherSprites[0], COLORS_PROPER[TargCol[twerp]], false, false);
                Occupied.Add(XY);
                Buckets.Add(1);
                Scheme.Add(IAmScheming[TargCol[twerp]-1]);
                Six.Add(twerp);
                cyclingCursorColors[twerp] = TargCol[twerp];
                SetTarget(TargX[twerp], TargY[twerp], twerp);
            }
        }
        if (Buckets.Count() == 0) { return; }
        TargetCycle = Buckets.Max();
        if (TargetCycle > 1)
        {
            for (int oq = 0; oq < Occupied.Count(); oq++)
            {
                if (Buckets[oq] > 1)
                {
                    StartCoroutine(CycleTargets(Occupied[oq] % SQ_ACROSS, Occupied[oq] / SQ_ACROSS, Buckets[oq], Scheme[oq], Six[oq]));
                }
            }
        } else
        {
            StopAllCoroutines();
        }
    }

    private IEnumerator CycleTargets(int x, int y, int numb, int hehehe, int slotIx)
    {
        int[] untethered = new int[numb];
        int luigi = 0;
        for (int wa = 0; wa < 7; wa++)
        {
            if (hehehe % IAmScheming[wa] == 0)
            {
                untethered[luigi] = wa + 1;
                luigi++;
            }
        }

        int cycle = -1;

        while (true)
        {
            cycle = (cycle + 1) % numb;
            SetSprite(x, y, 9, Slots[21 + slotIx], OtherSprites[0], COLORS_PROPER[untethered[cycle]], false, false);
            cyclingCursorColors[slotIx] = untethered[cycle];
            yield return new WaitForSeconds(1f);
        }
    }
 
    void TargetPress(KMSelectable TS)
    {
        if (animating) { return; }
        for (int ing = 0; ing < CURSOR_LIMIT; ing++)
        {
            if (TS == TargetSels[ing])
            {

                StartCoroutine(MoveCat(ing));
            }
        }
    }

    private IEnumerator MoveCat(int meow)
    {
        animating = true;

        int who = Array.IndexOf(ChosenCollars, cyclingCursorColors[meow]);
        int whereX = TargX[meow];
        int whereY = TargY[meow];

        for (int weirdth = 0; weirdth < CURSOR_LIMIT; weirdth++)
        {
            SetSprite(whereX, whereY, 9, Slots[21 + weirdth], weirdth == meow ? OtherSprites[4] : null, COLORS_PROPER[LaserColor == 0 ? 4 : LaserColor == 1 ? 2 : 1], false, false);
        }

        float elapsed = 0f;
        if (CatPosY[who] == whereY && (Math.Abs(CatPosX[who] - whereX) < 7 || whereY == 18)) //walk
        {
            float duration = 0.75f;
            while (elapsed < duration)
            {
                SetSprite(Lerp(CatPosX[who], whereX, elapsed / duration), whereY - 1, 3 + who * 2, Slots[who], CatSprites[ChosenCats[who] * 10 + (int)Math.Floor((elapsed / duration) * 8) + 1], Color.white, CatFacing[who], false);
                SetSprite(Lerp(CatPosX[who], whereX, elapsed / duration), whereY - 1, 4 + who * 2, Slots[who + 3], OtherSprites[2], COLORS_PROPER[ChosenCollars[who]], CatFacing[who], false);
                yield return null;
                elapsed += Time.deltaTime;
            }
        } else //pounce
        {
            float duration = 0.2f;
            while (elapsed < duration)
            {
                SetSprite(Lerp(CatPosX[who] + 0.5f, whereX + 0.5f, elapsed / duration), Lerp(CatPosY[who], whereY, elapsed / duration), 3 + who * 2, Slots[who], CatSprites[ChosenCats[who] * 10 + 9], Color.white, CatFacing[who], false);
                SetSprite(Lerp(CatPosX[who] + 0.5f, whereX + 0.5f, elapsed / duration), Lerp(CatPosY[who], whereY, elapsed / duration), 4 + who * 2, Slots[who + 3], OtherSprites[3], COLORS_PROPER[ChosenCollars[who]], CatFacing[who], false);
                yield return null;
                elapsed += Time.deltaTime;
            }
        }

        SetSprite(whereX, whereY - 1, 3 + who * 2, Slots[who], CatSprites[ChosenCats[who] * 10], Color.white, CatFacing[who], false);
        SetSprite(whereX, whereY - 1, 4 + who * 2, Slots[who + 3], OtherSprites[1], COLORS_PROPER[ChosenCollars[who]], CatFacing[who], false);

        CatPosX[who] = whereX;
        CatPosY[who] = whereY;

        for (int sus = 0; sus < SHELF_COUNT; sus++)
        {
            if (ShelfPositions[sus] - SQ_ACROSS == CatPosX[who] + CatPosY[who] * SQ_ACROSS)
            {
                if (orders[orderIx][sus] == who)
                {
                    Debug.LogFormat("[Laser Luring #{0}] {1} knocking over {2} is correct.", moduleId, CAT_NAMES[ChosenCats[who]], ITEM_NAMES[itemIxs[orders[orderIx][sus]]]);
                    catSatisfaction[who] = true;
                    SetSprite(whereX, whereY - 1, 9, Slots[6 + who], OtherSprites[5], Color.white, CatFacing[who], false);
                    // TODO: (later polish step) place a 'fallen' item directly underneath where it was originally placed
                    SetSprite(whereX, whereY, 2, Slots[18 + who], null, Color.white, false, false); //placeholder for the above
                    if (catSatisfaction[0] && catSatisfaction[1] && catSatisfaction[2])
                    {
                        Lights[LaserColor ?? 0].gameObject.SetActive(false); //again, stop it compiler, you're pissing me off
                        Debug.LogFormat("[Laser Luring #{0}] Cat curiosity satisfied, module solved.", moduleId);
                        Module.HandlePass();
                        moduleSolved = true;
                    }
                } else
                {
                    Debug.LogFormat("[Laser Luring #{0}] {1} is not interested in {2}, strike!", moduleId, CAT_NAMES[ChosenCats[who]], ITEM_NAMES[itemIxs[orders[orderIx][sus]]]);
                    Module.HandleStrike();
                }
            }
        }

        SetSprite(whereX, whereY, 9, Slots[21 + meow], null, Color.white, false, false);
        PlaceTargets(LaserColor ?? 0); //compiler shut up i know what i am doing
        animating = false;
    }

    float Lerp(float a, float b, float t)
    { //this assumes t is in the range 0-1
        return a * (1f - t) + b * t;
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

        for (int fx = 0; fx < 3; fx++)
        {
            itemIxs[fx] = conv[catInits[fx]];
        }

        Debug.LogFormat("<Laser Luring #{0}> Attempts: {1}{2}", moduleId, attps, attpsButLess == 0 ? "" : "." + attps);
        Debug.LogFormat("[Laser Luring #{0}] Cats: {1} {2}, {3} {4}, {5} {6}", moduleId, CAT_NAMES[ChosenCats[0]], COLOR_NAMES[ChosenCollars[0]], CAT_NAMES[ChosenCats[1]], COLOR_NAMES[ChosenCollars[1]], CAT_NAMES[ChosenCats[2]], COLOR_NAMES[ChosenCollars[2]]);
        Debug.LogFormat("[Laser Luring #{0}] {1}'s initial position: Row {2}, Column {3}", moduleId, CAT_NAMES[ChosenCats[0]], catInitSplit[0], catInitSplit[3]);
        Debug.LogFormat("[Laser Luring #{0}] {1}'s initial position: Row {2}, Column {3}", moduleId, CAT_NAMES[ChosenCats[1]], catInitSplit[1], catInitSplit[4]);
        Debug.LogFormat("[Laser Luring #{0}] {1}'s initial position: Row {2}, Column {3}", moduleId, CAT_NAMES[ChosenCats[2]], catInitSplit[2], catInitSplit[5]);
        Debug.LogFormat("[Laser Luring #{0}] Favorite items: {1} for {2}, {3} for {4}, {5} for {6}", moduleId, ITEM_NAMES[itemIxs[0]], CAT_NAMES[ChosenCats[0]], ITEM_NAMES[itemIxs[1]], CAT_NAMES[ChosenCats[1]], ITEM_NAMES[itemIxs[2]], CAT_NAMES[ChosenCats[2]]);
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

    private int[] GenerateShelves()
    {
        TryAgain:
        var shelfList = new List<int>();
        int SHELF_WIDTH = 5;
        var randomPositions = Enumerable.Range(0, SHELF_COUNT).Select(i => Rnd.Range(0, SQ_ACROSS * SQ_TALL)).ToArray();

        // If the X position is too far right
        if (randomPositions.Any(num => num % SQ_ACROSS > SQ_ACROSS - (SHELF_WIDTH + 1)))
            goto TryAgain;

        // If the Y position is too high or too low
        if (randomPositions.Any(num => num / SQ_ACROSS < 3 || num / SQ_ACROSS > SQ_TALL - 4))
            goto TryAgain;

        for (int i = 0; i < randomPositions.Length; i++)
        {
            var ps = Enumerable.Range(0, 5).Select(x => randomPositions[i] + x).ToArray();
            shelfList.AddRange(ps);
        }

        var requiredClearAreas = new List<int>();
        for (int i = 0; i < shelfList.Count; i++)
        {
            var A = shelfList.Select(x => x - (SQ_ACROSS * 1)).ToList();
            var B = shelfList.Select(x => x - (SQ_ACROSS * 2)).ToList();
            var C = shelfList.Select(x => x - (SQ_ACROSS * 3)).ToList();
            requiredClearAreas.AddRange(A);
            requiredClearAreas.AddRange(B);
            requiredClearAreas.AddRange(C);
        }
        if (shelfList.Any(i => requiredClearAreas.Contains(i)))
            goto TryAgain;

        // If there is a position that more than one shelf occupies
        if (shelfList.Distinct().Count() != shelfList.Count())
            goto TryAgain;

        var arr = new int[SHELF_COUNT];
        for (int i = 0; i < SHELF_COUNT; i++)
            arr[i] = shelfList[i * 5 + 2];

        // If any of the shelves are aligned vertically
        if (arr.Select(i => i % SQ_ACROSS).Distinct().Count() != arr.Select(i => i % SQ_ACROSS).Count())
            goto TryAgain;

        // If none of the shelves are reachable (the cat can be in the air for at most 10 units)
        bool valid = AreShelvesReachable(shelfList);
        if (!valid)
            goto TryAgain;

        arr = arr.OrderBy(v => v / SQ_ACROSS).ToArray();

        int[] waluigi = orders[orderIx];

        for (int nit = 0; nit < 3; nit++)
        {
            int it = waluigi[nit];
            bool possibleBump = Rnd.Range(0, 2) == 0;
            SetSprite((arr[nit] % SQ_ACROSS) + (itemWH[itemIxs[it]][0] == 2 ? (possibleBump ? 0.5f : -0.5f) : 0), (arr[nit] / SQ_ACROSS) - 1 - (itemWH[itemIxs[it]][1] - 1) * 0.5f, 2, Slots[18 + it], ItemSprites[itemIxs[it]], Color.white, false, false);
        }

        string str = "";
        for (int p = 0; p < SQ_ACROSS * SQ_TALL; p++)
        {
            if (p % SQ_ACROSS == 0)
                str += "\n";
            if (shelfList.Contains(p))
                str += "▓";
            else
                str += "░";
        }
        Debug.LogFormat("<Laser Luring #{0}> Shelves but string:\n{1}", moduleId, str);

        return arr;
    }

    class Fuckage
    {
        public int Position;
        public int InAirDuration;

        public Fuckage(int position, int inAirDuration)
        {
            Position = position;
            InAirDuration = inAirDuration;
        }

        public bool Equals(Fuckage other)
        {
            return other != null && Position == other.Position;
        }
    }

    struct QueueItem
    {
        public Fuckage Index;
        public Fuckage Parent;

        public QueueItem(Fuckage index, Fuckage parent)
        {
            Index = index;
            Parent = parent;
        }
    }

    private bool AreShelvesReachable(List<int> shelfPositions)
    {
        var standablePositions = new List<int>();

        var shelfCenters = new int[SHELF_COUNT];
        for (int i = 0; i < SHELF_COUNT; i++)
            shelfCenters[i] = shelfPositions[i * 5 + 2];

        var s = shelfPositions.Select(i => i - SQ_ACROSS).ToList();

        standablePositions.AddRange(s);
        standablePositions.AddRange(Enumerable.Range((SQ_TALL - 1) * SQ_ACROSS, SQ_ACROSS));

        for (int i = 0; i < standablePositions.Count; i++)
        {
            var visited = new Dictionary<int, QueueItem>();
            var q = new Queue<QueueItem>();
            var goal = new Fuckage(standablePositions[i], 0);

            var start = new Fuckage(standablePositions.Last(), 0);

            q.Enqueue(new QueueItem(start, null));

            while (q.Count > 0)
            {
                var qi = q.Dequeue();
                if (visited.ContainsKey(qi.Index.Position))
                    continue;
                visited[qi.Index.Position] = qi;
                if (qi.Index.Equals(goal))
                    goto Found;

                if (qi.Index.Position % SQ_ACROSS > 0)
                {
                    var f = new Fuckage(qi.Index.Position - 1, qi.Index.InAirDuration + 1);
                    if (!shelfPositions.Contains(f.Position))
                    {
                        if (standablePositions.Contains(qi.Index.Position))
                            f.InAirDuration = 0;
                        if (f.InAirDuration <= IN_AIR_DURATION)
                            q.Enqueue(new QueueItem(f, qi.Index));
                    }
                }

                if (qi.Index.Position % SQ_ACROSS < SQ_ACROSS - 1)
                {
                    var f = new Fuckage(qi.Index.Position + 1, qi.Index.InAirDuration + 1);
                    if (!shelfPositions.Contains(f.Position))
                    {
                        if (standablePositions.Contains(qi.Index.Position))
                            f.InAirDuration = 0;
                        if (f.InAirDuration <= IN_AIR_DURATION)
                            q.Enqueue(new QueueItem(f, qi.Index));
                    }
                }

                if (qi.Index.Position / SQ_ACROSS > 0)
                {
                    var f = new Fuckage(qi.Index.Position - SQ_ACROSS, qi.Index.InAirDuration + 1);
                    if (!shelfPositions.Contains(f.Position))
                    {
                        if (standablePositions.Contains(qi.Index.Position))
                            f.InAirDuration = 0;
                        if (f.InAirDuration <= IN_AIR_DURATION)
                            q.Enqueue(new QueueItem(f, qi.Index));
                    }
                }
            }
            return false;
            Found:;
            continue;
        }
        return true;
    }

    void SetSprite(float xp, float yp, int zp, SpriteRenderer slot, Sprite spr, Color col, bool fx, bool fy)
    {
        slot.sprite = spr;
        slot.gameObject.transform.localPosition = new Vector3(LEFT_EDGE + xp * GRID_SQ, 0.0103f, TOP_EDGE - yp * GRID_SQ);
        slot.sortingOrder = zp;
        slot.color = col;
        slot.flipX = fx;
        slot.flipY = fy;
    }

    void SetTarget(int x, int y, int ix)
    {
        TargetSels[ix].gameObject.SetActive(true);
        TargetSels[ix].gameObject.transform.localPosition = new Vector3(LEFT_EDGE + x * GRID_SQ, 0.0103f, TOP_EDGE - y * GRID_SQ);
    }
}
