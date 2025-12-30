using UnityEngine;

public class COLLECTIBLE : MonoBehaviour
{
    public struct UNIQUE
    {
        public int targetable_id;
        public Vector3 target_position;
        public Quaternion target_rotation;
        public bool should_despawn;
        public bool is_locked;
    }

    [HideInInspector] public ITEM item;
    [HideInInspector] public UNIQUE unique;
}
