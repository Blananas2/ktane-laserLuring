using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using KModkit;
using Rnd = UnityEngine.Random;

public class laserLuringScript : MonoBehaviour
{

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
    int[] CatPosY = { 17, 17, 17 };
    bool[] CatFacing = { false, false, false }; //left = true
    int? LaserColor = null;

    private const float LEFT_EDGE = -0.0806f;
    private const float TOP_EDGE = 0.0431f;
    private const float GRID_SQ = 0.00575888f;
    private const int SQ_ACROSS = 29;
    private const int SQ_TALL = 19;
    private const int MAX_IN_AIR_DURATION = 10;
    private const int SHELF_COUNT = 9;
    int[][] itemWH = new int[][] {
        new int[] {1,3}, new int[] {2,2}, new int[] {2,2}, new int[] {2,1}, new int[] {3,3}, new int[] {1,1}, new int[] {1,2}, new int[] {1,2},
        new int[] {1,2}, new int[] {1,1}, new int[] {1,1}, new int[] {1,1}, new int[] {1,2}, new int[] {2,2}, new int[] {1,1}, new int[] {1,1},
        new int[] {1,1}, new int[] {1,1}, new int[] {1,1}, new int[] {1,1}, new int[] {1,1}, new int[] {1,1}, new int[] {1,1}, new int[] {2,1},
        new int[] {1,1}, new int[] {1,1}, new int[] {1,1}, new int[] {1,1}, new int[] {3,2}, new int[] {1,1}, new int[] {1,3}, new int[] {3,1},
        new int[] {1,1}, new int[] {1,1}, new int[] {1,2}, new int[] {2,1}, new int[] {1,1}, new int[] {1,1}, new int[] {1,2}, new int[] {2,2},
        new int[] {1,1}, new int[] {1,1}, new int[] {2,3}, new int[] {1,1}, new int[] {1,1}, new int[] {2,1}, new int[] {3,2}, new int[] {1,1},
        new int[] {1,1}, new int[] {1,2}, new int[] {2,1}, new int[] {1,1}, new int[] {1,1}, new int[] {1,3}, new int[] {1,1}, new int[] {1,1},
        new int[] {1,1}, new int[] {1,1}, new int[] {1,2}, new int[] {1,1}, new int[] {2,3}, new int[] {2,2}, new int[] {1,1}, new int[] {1,2} 
    };
    int[] conv = { 36, 32, 37, 33, 38, 34, 39, 35, 4, 0, 5, 1, 6, 2, 7, 3, 44, 40, 45, 41, 46, 42, 47, 43, 12, 8, 13, 9, 14, 10, 15, 11, 52, 48, 53, 49, 54, 50, 55, 51, 20, 16, 21, 17, 22, 18, 23, 19, 60, 56, 61, 57, 62, 58, 63, 59, 28, 24, 29, 25, 30, 26, 31, 27 };
    int[] itemIxs = { -1, -1, -1 };
    int[][] orders = new int[][] { new int[] { 0, 1, 2 }, new int[] { 0, 2, 1 }, new int[] { 1, 0, 2 }, new int[] { 1, 2, 0 }, new int[] { 2, 0, 1 }, new int[] { 2, 1, 0 } };
    int orderIx = -1;

    private int[] ShelfPositions = new int[SQ_ACROSS * SQ_TALL];

    //Logging
    static int moduleIdCounter = 1;
    int moduleId;
    private bool moduleSolved;

    void Awake()
    {
        moduleId = moduleIdCounter++;

        foreach (KMSelectable Button in Buttons)
        {
            Button.OnHighlight += delegate () { /*ButtonHover(Button);*/ };
            Button.OnInteract += delegate () { ButtonPress(Button); return false; };
        }

        foreach (KMSelectable TargetSel in TargetSels)
        {
            TargetSel.OnInteract += delegate () { TargetPress(TargetSel); return false; };
        }

        //button.OnInteract += delegate () { buttonPress(); return false; };

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
            SetSprite(CatPosX[m], 17, 3 + m, Slots[m], CatSprites[ChosenCats[m] * 10], Color.white, flipEm, false);
            SetSprite(CatPosX[m], 17, 6, Slots[m + 3], OtherSprites[1], COLORS_PROPER[ChosenCollars[m]], flipEm, false);
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
                        SetSprite(CatPosX[catplant], CatPosY[catplant], 3 + catplant, Slots[catplant], CatSprites[ChosenCats[catplant] * 10], Color.white, CatFacing[catplant], false);
                        SetSprite(CatPosX[catplant], CatPosY[catplant], 6, Slots[catplant + 3], OtherSprites[1], COLORS_PROPER[ChosenCollars[catplant]], CatFacing[catplant], false);
                        break;
                    }
                }

                //TODO: put the target selectables in the right spots, ditto for the sprites, have coroutine handle cycling through colors every second
            }
            else
            {
                Lights[btn].gameObject.SetActive(false);
            }
        }
    }
 
    void TargetPress(KMSelectable TS)
    {
        Debug.Log(TS);
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
        Debug.LogFormat("[Laser Luring #{0}] Favorite items: i{1} for {2}, i{3} for {4}, i{5} for {6}", moduleId, itemIxs[0], CAT_NAMES[ChosenCats[0]], itemIxs[1], CAT_NAMES[ChosenCats[1]], itemIxs[2], CAT_NAMES[ChosenCats[2]]);
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
        int shelfWidth = 5;
        var randomPositions = Enumerable.Range(0, SHELF_COUNT).Select(i => Rnd.Range(0, SQ_ACROSS * SQ_TALL)).ToArray();

        // If the X position is too far right
        if (randomPositions.Any(num => num % SQ_ACROSS > SQ_ACROSS - (shelfWidth + 1)))
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
                        if (f.InAirDuration <= MAX_IN_AIR_DURATION)
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
                        if (f.InAirDuration <= MAX_IN_AIR_DURATION)
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
                        if (f.InAirDuration <= MAX_IN_AIR_DURATION)
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

    /*
    void buttonPress() {

    }
    */

    void SetSprite(float xp, float yp, int zp, SpriteRenderer slot, Sprite spr, Color col, bool fx, bool fy)
    {
        slot.sprite = spr;
        slot.gameObject.transform.localPosition = new Vector3(LEFT_EDGE + xp * GRID_SQ, 0.0103f, TOP_EDGE - yp * GRID_SQ);
        slot.sortingOrder = zp;
        slot.color = col;
        slot.flipX = fx;
        slot.flipY = fy;
    }
}
