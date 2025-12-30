using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

public class HOTBAR : MonoBehaviour
{
    public static HOTBAR instance;

    [Header("components/children")]
    [SerializeField] SLOT[] slots;
    [SerializeField] Transform selected_slot_border;
    [SerializeField] TextMeshProUGUI score_text;
    
    //state
    int selected_slot_index = 0;
    int score = 0;

    
    void Awake()
    {
        instance = this;
    }

    void Start()
    {
        this.selected_slot_border.position = this.slots[this.selected_slot_index].transform.position;
    }

    void Update()
    {
        if (Keyboard.current.digit1Key.wasPressedThisFrame) { this.selected_slot_index = 0; }
        if (Keyboard.current.digit2Key.wasPressedThisFrame) { this.selected_slot_index = 1; }
        if (Keyboard.current.digit3Key.wasPressedThisFrame) { this.selected_slot_index = 2; }
        if (Keyboard.current.digit4Key.wasPressedThisFrame) { this.selected_slot_index = 3; }
        if (Keyboard.current.digit5Key.wasPressedThisFrame) { this.selected_slot_index = 4; }
        if (Keyboard.current.digit6Key.wasPressedThisFrame) { this.selected_slot_index = 5; }

        this.selected_slot_border.position = this.slots[this.selected_slot_index].transform.position;
    }

    public SLOT selected_slot()
    {
        return this.slots[this.selected_slot_index];
    }
    
    public bool try_merge(ITEM item, COLLECTIBLE.UNIQUE unique)
    {
        foreach (var slot in this.slots)
        {
            if (slot.try_merge(item, unique))
            {
                return true;
            }
        }
        return false;
    }

    public bool try_allot(ITEM item, COLLECTIBLE.UNIQUE unique)
    {
        foreach (var slot in this.slots)
        {
            if (slot.try_allot(item, unique))
            {
                return true;
            }
        }
        return false;
    }

    public void increment_score()
    {
        this.score = this.score + 1;
        this.score_text.text = this.score.ToString();
    }
}