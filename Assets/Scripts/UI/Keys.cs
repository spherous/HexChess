using System.Collections;
using System.Collections.Generic;
using Extensions;
using TMPro;
using UnityEngine;

public class Keys : MonoBehaviour
{
    [SerializeField] private GameObject whiteKeys;
    [SerializeField] private GameObject blackKeys;
    public Color highlightColor;
    public Color defaultColor;

    [SerializeField] private List<TextMeshPro> whiteNums = new List<TextMeshPro>();
    [SerializeField] private List<TextMeshPro> whiteLetters = new List<TextMeshPro>();
    [SerializeField] private List<TextMeshPro> blackNums = new List<TextMeshPro>();
    [SerializeField] private List<TextMeshPro> blackLetters = new List<TextMeshPro>();

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