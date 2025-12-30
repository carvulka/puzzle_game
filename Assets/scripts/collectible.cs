using UnityEngine;

public class COLLECTIBLE : MonoBehaviour
{
    public struct UNIQUE
    {
        public int targetable_id;
        public Vector3 target_position;
        public Quaternion target_rotation;
    }

    [HideInInspector] public ITEM item;
    [HideInInspector] public UNIQUE unique;
}
