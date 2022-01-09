using Extensions;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Pawn : MonoBehaviour, IPiece
{
    public GameObject obj {get => gameObject; set{}}
    public Team team {get => _team; set{_team = value;}}
    private Team _team;
    public Piece piece {get => _piece; set{_piece = value;}}
    private Piece _piece;
    public Index location {get => _location; set{_location = value;}}
    private Index _location;
    private Index startLoc;
    public bool captured {get => _captured; set{_captured = value;}}
    private bool _captured = false;
    public ushort value {get => 1; set{}}
    public int goal => team == Team.White ? 18 - (location.row % 2) : location.row % 2;
    public int GetGoalInRow(int r) => team == Team.White ? 18 - (r % 2) : r % 2;

    public Vector3? targetPos {get; private set;} = null;
    public float speed = 15f;

    [SerializeField] private MeshRenderer _meshRenderer;
    public MeshRenderer meshRenderer { get => _meshRenderer; set{}}
    private Color defaultHighlightColor;

    public void Init(Team team, Piece piece, Index startingLocation)
    {
        this.team = team;
        this.piece = piece;
        this.location = startingLocation;
        startLoc = startingLocation;
        defaultHighlightColor = meshRenderer.material.GetColor("_HighlightColor");
    }

    public void MoveTo(Hex hex, Action<Piece> action = null)
    {
        targetPos = hex.transform.position + Vector3.up;
        location = hex.index;

        // If the pawn reaches the other side of the board, it can Promote
        if(location.row == goal && action != null)
            hex.board?.QueryPromote(this, action);
    }

    private void Update() => MoveOverTime();

    private void MoveOverTime()
    {
        if(!targetPos.HasValue)
            return;

        transform.position = Vector3.Lerp(transform.position, targetPos.Value, Time.deltaTime * speed);
        if((transform.position - targetPos.Value).magnitude < 0.03f)
        {
            transform.position = targetPos.Value;
            targetPos = null;
        }
    }

    public void CancelMove()
    {
        targetPos = null;
    }

    public string GetPieceString() => "Pawn";

    public void ResetHighlight() => meshRenderer.material.SetColor("_HighlightColor", defaultHighlightColor);
    public void HighlightWithColor(Color color) => meshRenderer.material.SetColor("_HighlightColor", color);
}