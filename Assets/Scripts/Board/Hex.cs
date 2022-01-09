using System;
using System.Collections.Generic;
using Extensions;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;

public class Hex : SerializedMonoBehaviour
{
    public bool selected {get; private set;}
    [OdinSerialize, ReadOnly] public Board board {get; private set;}
    [OdinSerialize, ReadOnly] public Index index {get; private set;}
    [SerializeField, ReadOnly] private MeshRenderer meshRenderer;

    public Color outlineColor;

    IEnumerable<(Hex neighbor, HexNeighborDirection direction)> NeighborsWithDirection()
    {
        foreach(HexNeighborDirection direction in EnumArray<HexNeighborDirection>.Values)
        {
            Hex neighbor = board?.GetNeighborAt(index, direction);
            yield return (neighbor, direction);
        }
    }

    public void AssignIndex(Index index, Board board = null)
    {
        this.index = index;
        this.board = board;
    } 

    public void ToggleSelect() => (selected ? (Action)Deselect : (Action)Select)();
    
    public void Highlight(Color highlightColor)
    {
        meshRenderer.material.SetColor("_HighlightColor", highlightColor);
        meshRenderer.material.SetFloat("_HighlightPower", 0.6f);
    }

    public void SetHighlightPower(float pow)
    {
        meshRenderer.material.SetFloat("_HighlightPower", pow);
    }

    public void Unhighlight() => meshRenderer.material.SetFloat("_HighlightPower", 0f);

    [Button(ButtonSizes.Medium, ButtonStyle.FoldoutButton)]
    public void SetColor(Color color)
    {
        if(meshRenderer.sharedMaterial == null)
            return;
#if UNITY_EDITOR
        Material mat = new Material(meshRenderer.sharedMaterial);
        mat.SetColor("_BaseColor", color);
        meshRenderer.material = mat;
#elif !UNITY_EDITOR
        meshRenderer.material.SetColor("_BaseColor", color);
#endif
    }

    [Button(ButtonSizes.Medium, ButtonStyle.FoldoutButton)]
    public void SetOutlineColor(Color color)
    {
        outlineColor = color;
#if UNITY_EDITOR
        Material mat = new Material(meshRenderer.sharedMaterial);
        mat.SetColor("_EdgeColor", color);
        meshRenderer.material = mat;
#else
        meshRenderer.material.SetColor("_EdgeColor", color);
#endif
    }

    public Color GetOutlineColor() => meshRenderer.material.GetColor("_EdgeColor");

    [Button(ButtonSizes.Medium, ButtonStyle.FoldoutButton)]
    public void SetOutlineThickness(float thickness)
    {
#if UNITY_EDITOR
        Material mat = new Material(meshRenderer.sharedMaterial);
        mat.SetFloat("_Thickness", thickness);
        meshRenderer.material = mat;
#else
        meshRenderer.material.SetFloat("_Thickness", thickness);
#endif
    }
    

    private void Select()
    {
        // Debug.Log($"Selecting hex: {index.GetKey()}");

        selected = true;

        // Update edges of this hex based on the direction to each neighbor
        foreach(var (neighbor, direction) in NeighborsWithDirection())
            if(neighbor == null || !neighbor.selected || neighbor.outlineColor != outlineColor)
                UpdateEdge(direction);

        UpdateNeighbors();
    }

    private void Deselect()
    {
        // Debug.Log($"Deselecting hex: {index.GetKey()}");
        selected = false;
        
        // Clear self
        if(meshRenderer != null)
        {
            for(int i = 0; i < 6; i++)
            {
    #if UNITY_EDITOR
                Material mat = new Material(meshRenderer.sharedMaterial);
                mat.SetFloat($"_Edge{i}", 0f);
                meshRenderer.material = mat;
    #elif !UNITY_EDITOR
                meshRenderer.material.SetFloat($"_Edge{i}", 0f);
    #endif 
            }
        }

        UpdateNeighbors();
    }

    public void UpdateEdge(HexNeighborDirection toUpdate)
    {
#if UNITY_EDITOR
        Material mat = new Material(meshRenderer.sharedMaterial);
        mat.SetFloat(
            name: $"_Edge{(int)toUpdate}", 
            value: Mathf.Abs(meshRenderer.sharedMaterial.GetFloat($"_Edge{(int)toUpdate}") - 1).Floor()
        );
        meshRenderer.material = mat;
#elif !UNITY_EDITOR
        meshRenderer.material.SetFloat(
            name: $"_Edge{(int)toUpdate}", 
            value: Mathf.Abs(meshRenderer.material.GetFloat($"_Edge{(int)toUpdate}") - 1).Floor()
        );
#endif 
    }

    public void UpdateNeighbors()
    {
        foreach(var (neighbor, direction) in NeighborsWithDirection())
            if(neighbor != null && neighbor.selected && neighbor.outlineColor == outlineColor)
                neighbor.UpdateEdge(direction.OppositeDirection());
    }
}