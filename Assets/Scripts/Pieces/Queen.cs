using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Queen : MonoBehaviour, IPiece
{
    public GameObject obj {get => gameObject; set{}}
    public Team team { get{ return _team; } set{ _team = value; } }
    private Team _team;
    public Piece piece { get{ return _piece; } set{ _piece = value; } }
    private Piece _piece;
    public Index location { get{ return _location; } set{ _location = value; } }
    private Index _location;
    public bool captured { get{ return _captured; } set{ _captured = value; } }
    private bool _captured = false;
    public ushort value {get => 6; set{}}
    private Vector3? targetPos = null;
    public float speed = 15f;
    [SerializeField] private MeshRenderer _meshRenderer;
    public MeshRenderer meshRenderer { get => _meshRenderer; set{}}
    private Color defaultHighlightColor;
    

    public void Init(Team team, Piece piece, Index startingLocation)
    {
        this.team = team;
        this.piece = piece;
        this.location = startingLocation;
        defaultHighlightColor = meshRenderer.material.GetColor("_HighlightColor");
    }

    public void MoveTo(Hex hex, Action<Piece> action = null)
    {
        targetPos = hex.transform.position + Vector3.up;
        location = hex.index;
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

    public string GetPieceString() => "Queen";

    public void ResetHighlight() => meshRenderer.material.SetColor("_HighlightColor", defaultHighlightColor);
    public void HighlightWithColor(Color color) => meshRenderer.material.SetColor("_HighlightColor", color);
}
