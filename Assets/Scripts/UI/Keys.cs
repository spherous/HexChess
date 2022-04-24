using System.Collections.Generic;
using System.Linq;
using Extensions;
using TMPro;
using UnityEngine;

public class Keys : MonoBehaviour
{
    private MouseData mouseData;
    private SmoothHalfOrbitalCamera smoothHalfOrbitalCamera;
    private OptionsPanel optionsPanel;
    private Multiplayer multiplayer;
    private OnMouse onMouse;
    private Board board;

    [SerializeField] private GameObject whiteKeys;
    [SerializeField] private GameObject blackKeys;
    public Color highlightColor;
    public Color defaultColor;

    [SerializeField] private List<TextMeshPro> whiteNums = new List<TextMeshPro>();
    [SerializeField] private List<TextMeshPro> whiteLetters = new List<TextMeshPro>();
    [SerializeField] private List<TextMeshPro> blackNums = new List<TextMeshPro>();
    [SerializeField] private List<TextMeshPro> blackLetters = new List<TextMeshPro>();
    
    IEnumerable<Hex> keyHighlightedHexes = Enumerable.Empty<Hex>();

    private TextMeshPro lastWhiteNum;
    private TextMeshPro lastWhiteLetter;
    private TextMeshPro lastBlackNum;
    private TextMeshPro lastBlackLetter;

    private IEnumerable<TextMeshPro> GetAllLastHighlighted()
    {
        yield return lastWhiteNum;
        yield return lastWhiteLetter;
        yield return lastBlackNum;
        yield return lastBlackLetter;
    }

    private void Awake()
    {
        mouseData = GameObject.FindObjectOfType<MouseData>();
        smoothHalfOrbitalCamera = GameObject.FindObjectOfType<SmoothHalfOrbitalCamera>();
        optionsPanel = GameObject.FindObjectOfType<OptionsPanel>();
        multiplayer = GameObject.FindObjectOfType<Multiplayer>();
        onMouse = GameObject.FindObjectOfType<OnMouse>();
        board = GameObject.FindObjectOfType<Board>();

        mouseData.onHoverHex += OnHoverHex;
        mouseData.onHoverKey += OnHoverKey;
    }

    private void OnDestroy() {
        if(mouseData != null)
        {
            mouseData.onHoverHex -= OnHoverHex;
            mouseData.onHoverKey -= OnHoverKey;
        }
    }

    private void OnHoverKey(TextMeshPro hoveredKey)
    {
        if(onMouse.isPickedUp) // Collides with showing valid move locations
            return;
        else if(multiplayer != null && !multiplayer.gameParams.showMovePreviews)
            return;
        else if(!PlayerPrefs.GetInt("HandicapOverlay", true.BoolToInt()).IntToBool())
            return;
        else if(smoothHalfOrbitalCamera != null && smoothHalfOrbitalCamera.freeLooking)
            return;
        else if(optionsPanel != null && optionsPanel.visible)
            return;

        ClearHighlightedHexes();

        if(hoveredKey == null)
            return;

        if(int.TryParse(hoveredKey.text, out int num))
        {
            // use Index here?
            // hovered key is a number, so we highlight the correct rows
            var desired = GetRows(num);

            if(desired.row1 == -1 || desired.row2 == -1)
                return;
            
            // this sucks
            IEnumerable<Hex> hexesInRow1 = board.hexes.Count > desired.row1 ? board.hexes[desired.row1] : Enumerable.Empty<Hex>();
            IEnumerable<Hex> hexesInRow2 = board.hexes.Count > desired.row2 ? board.hexes[desired.row2] : Enumerable.Empty<Hex>();
            IEnumerable<Hex> hexesInRow = hexesInRow1.Concat(hexesInRow2);

            HighlightHexes(hexesInRow);
        }
        else
        {
            // use Index here?
            // hovered key is a letter, so we highlight the correct column
            var desired = GetCols(hoveredKey.text);

            if(desired.col == -1)
                return;

            IEnumerable<Hex> hexesInCol = board.GetHexesInCol(desired.col);
            hexesInCol = hexesInCol.Where(hex => hex.index.row % 2 == 0 == desired.isEven);

            HighlightHexes(hexesInCol);
        }
    }

    private void HighlightHexes(IEnumerable<Hex> toHighlight)
    {
        foreach (Hex hex in toHighlight)
        {
            hex.SetOutlineColor(highlightColor);
            hex.ToggleSelect();
        }

        keyHighlightedHexes = toHighlight;
    }

    private (int row1, int row2) GetRows(int num) => num switch
    {
        1 => (0, 1), 2 => (2, 3),
        3 => (4, 5), 4 => (6, 7),
        5 => (8, 9), 6 => (10, 11),
        7 => (12, 13), 8 => (14, 15),
        9 => (16, 17), 10 => (18, 19),
        _ => (-1, -1) 
    };

    private (bool isEven, int col) GetCols(string text) => text switch
    {
        "A" => (false, 0), "B" => (true, 0),
        "C" => (false, 1), "D" => (true, 1),
        "E" => (false, 2), "F" => (true, 2),
        "G" => (false, 3), "H" => (true, 3),
        "I" => (false, 4), _ => (false, -1)
    };

    private void ClearHighlightedHexes()
    {
        foreach (Hex hex in keyHighlightedHexes)
            hex.ToggleSelect();

        keyHighlightedHexes = Enumerable.Empty<Hex>();
    }

    private void OnHoverHex(Hex hoveredHex)
    {
        if(smoothHalfOrbitalCamera != null && smoothHalfOrbitalCamera.freeLooking)
            return;
        else if(optionsPanel != null && optionsPanel.visible)
            return;

        if(hoveredHex == null)
            Clear();
        else
            HighlightKeys(hoveredHex.index);
    }

    public void SetKeys(Team team)
    {
        whiteKeys.SetActive(team == Team.White);
        blackKeys.SetActive(team == Team.Black);
    }

    public void HighlightKeys(Index hexIndex)
    {
        Clear();
        
        lastWhiteNum = GetNumberText(hexIndex.row, whiteNums);
        lastBlackNum = GetNumberText(hexIndex.row, blackNums);
        lastWhiteLetter = GetLetterText(hexIndex, whiteLetters);
        lastBlackLetter = GetLetterText(hexIndex, blackLetters);

        lastWhiteNum.color = highlightColor;
        lastBlackNum.color = highlightColor;
        lastWhiteLetter.color = highlightColor;
        lastBlackLetter.color = highlightColor;
    }

    public void Clear()
    {
        foreach(TextMeshPro text in GetAllLastHighlighted())
        {
            if(text == null)
                continue;
            text.color = defaultColor;
        }
    }

    public TextMeshPro GetNumberText(int row, List<TextMeshPro> nums) => 
        nums[((float)row / 2f).Floor()];
    
    public TextMeshPro GetLetterText(Index index, List<TextMeshPro> letters)
    {
        bool isEven = index.row % 2 == 0;

        return index.col switch{
            0 when !isEven => letters[0], 0 when isEven => letters[1],
            1 when !isEven => letters[2], 1 when isEven => letters[3],
            2 when !isEven => letters[4], 2 when isEven => letters[5],
            3 when !isEven => letters[6], 3 when isEven => letters[7],
            4 => letters[8], _ => null
        };
    }
}