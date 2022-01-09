using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Extensions;

public class Arrow : MonoBehaviour
{
    [SerializeField] private List<LineRenderer> lineRenderers = new List<LineRenderer>();
    [SerializeField] private LineRenderer tail;
    [SerializeField] private Transform head;
    public void Init(Vector3 from, Vector3 to, Color color)
    {
        float pos0Offset = tail.GetPosition(0).magnitude;
        float pos1Offset = tail.GetPosition(1).magnitude;
        
        Vector3 forwardDir = (to - from).normalized;
        Vector3 backwardsVec = from - to;
        head.forward = forwardDir;
        transform.position = to;

        // The first 2 nodes must always be the same distance apart. This ensures a perfect match between the head and the tail of the arrow. 
        // Moving the 2nd node would create gaps near the arrow head/tail joint`
        tail.SetPosition(0, backwardsVec.normalized * pos0Offset);
        tail.SetPosition(1, backwardsVec.normalized * pos1Offset);

        // adjust the position of the remaining nodes
        for(int i = 2; i < tail.positionCount; i++)
        {
            Vector3 pos = tail.GetPosition(i);
            float percent = (float)i / (float)(tail.positionCount - 1);
            // if(i != 0 && i != 1)
            pos = backwardsVec * percent;
            tail.SetPosition(i, pos);
        }

        foreach(LineRenderer renderer in lineRenderers)
        {
            renderer.startColor = color;
            renderer.endColor = color;
        }
    }
}