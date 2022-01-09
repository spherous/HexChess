using System.Collections.Generic;
using System.Linq;
using Extensions;
using Sirenix.OdinInspector;
using UnityEngine;

public class Jail : MonoBehaviour 
{
    [SerializeField] public Team teamToPrison;

    [ShowInInspector, ReadOnly] List<IPiece> prisonedPieces = new List<IPiece>();
    [SerializeField] private int piecesAcross;
    [SerializeField] private Vector2 xzOffsets = new Vector2();

    public void Enprison(IPiece piece)
    {
        if(prisonedPieces.Contains(piece))
            return;
        
        piece.ResetHighlight();

        piece.CancelMove();
        piece.captured = true;

        Vector3 initialRot = piece.obj.transform.rotation.eulerAngles;
        piece.obj.transform.SetPositionAndRotation(
            position: GetNextPos(),
            rotation: Quaternion.Euler(initialRot.x, 180, initialRot.z)
        );
        piece.obj.transform.parent = transform;
        prisonedPieces.Add(piece);
        Sort();
    }

    public void RemoveFromPrison(IPiece piece)
    {
        if(!prisonedPieces.Contains(piece))
            return;
        
        piece.captured = false;
        piece.obj.transform.parent = null;

        Vector3 rot = piece.obj.transform.rotation.eulerAngles;
        piece.obj.transform.rotation = Quaternion.Euler(rot.x, 0, rot.z);
        prisonedPieces.Remove(piece);
        Sort();
    }

    public void Sort()
    {
        List<IPiece> sortedPieces = prisonedPieces.OrderByDescending(child => child.value).ToList();
        for(int i = 0; i < sortedPieces.Count; i++)
        {
            IPiece sortPiece = sortedPieces[i];
            sortPiece.obj.transform.position = GetPos(i);
        }
    }

    public IPiece GetPieceIfInJail(Piece piece)
    {
        IEnumerable<IPiece> pieces = prisonedPieces.Where(iPiece => iPiece.piece == piece);
        if(pieces.Count() == 1)
            return pieces.First();
        else
            return null;
    }

    public Vector3 GetNextPos() => GetPos(prisonedPieces.Count);

    public Vector3 GetPos(int index)
    {
        // Debug.Log(index);
        int xCount = index % piecesAcross;
        int zCount = ((float)index / (float)piecesAcross).Floor();
        return new Vector3(
            x: transform.position.x + (xzOffsets.x * xCount),
            y: transform.position.y,
            z: transform.position.z + (xzOffsets.y * zCount)
        );
    }

    public void Clear()
    {
        foreach(IPiece piece in prisonedPieces)
            Destroy(piece.obj);
        prisonedPieces.Clear();
    }
}