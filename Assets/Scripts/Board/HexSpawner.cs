using System.Collections;
using System.Collections.Generic;
using Extensions;
using Sirenix.OdinInspector;
using UnityEngine;

public class HexSpawner : MonoBehaviour
{
    [SerializeField] private GameObject hexPrefab;
    List<List<Hex>> hexes = new List<List<Hex>>();
    public Color color;
    public Color outlineColor;
    public float Apothem(float radius) => (radius.Sqr() - (radius * 0.5f).Sqr()).Sqrt();

    [Button("Spawn Hexes")]
    private void SpawnHexes(int rows, int cols, float radius = 1f, float height = 1f, float minHeight = 1f, float maxHeight = 1f)
    {
        if(hexes.Count > 0)
            ClearHexes();
        
        for(int row = 0; row < rows; row++)
        {
            hexes.Add(new List<Hex>());
            for(int col = 0; col < cols; col++)
            {
                if(cols % 2 != 0 && col == cols - 1 && row % 2 == 0)
                    continue;

                GameObject newGo = Instantiate(
                    original: hexPrefab,
                    position: new Vector3(
                        x: radius * 3 * col + Get_X_Offset(row, radius),
                        y: UnityEngine.Random.Range(minHeight, maxHeight),
                        z: row * Apothem(radius)
                    ),
                    rotation: Quaternion.identity,
                    parent: transform
                );

                Hex newHex = newGo.GetComponent<Hex>();

                newHex.transform.localScale = new Vector3(
                    x: newHex.transform.localScale.x * radius,
                    y: newHex.transform.localScale.y * height,
                    z: newHex.transform.localScale.z * radius
                );

                newHex.AssignIndex(new Index(row, col));
                newHex.SetOutlineColor(outlineColor);
                newHex.ToggleSelect();
                newHex.SetOutlineThickness(0.01f);

                hexes[row].Add(newHex);
                newHex.SetColor(color);
            }
        }
    }

    private float Get_X_Offset(int row, float radius) => row % 2 == 0 ? radius * 1.5f : 0f;

    [Button("Clear Hexes")]
    private void ClearHexes()
    {
        for(int row = 0; row < hexes.Count; row++)
        {
            for(int col = 0; col < hexes[row].Count; col++)
            {
                if(hexes[row][col] != null)
                {
#if UNITY_EDITOR
                    DestroyImmediate(hexes[row][col].gameObject);
#elif !UNITY_EDITOR
                    Destroy(hexes[row][col].gameObject);
#endif                
                }
            }
        }
        hexes = new List<List<Hex>>();
    }
}