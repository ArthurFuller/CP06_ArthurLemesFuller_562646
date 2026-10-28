using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class ArenaPlatform : MonoBehaviour
{
    public Bounds Bounds => GetComponent<BoxCollider>().bounds;
}
