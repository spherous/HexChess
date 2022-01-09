using System;
using UnityEngine;

public interface IPiece
{
    GameObject obj {get; set;}
    MeshRenderer meshRenderer {get; set;}
    Team team {get; set;}
    Piece piece {get; set;}
    Index location {get; set;}
    bool captured {get; set;}
    ushort value {get; set;}
    void MoveTo(Hex hex, Action<Piece> action = null);
    void CancelMove();
    void Init(Team team, Piece piece, Index startingLocation);
    string GetPieceString();
    void ResetHighlight();
    void HighlightWithColor(Color color);
}