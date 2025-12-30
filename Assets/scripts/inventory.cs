using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;

public class INVENTORY : MonoBehaviour
{
    public static INVENTORY instance;
    
    [Header("components/children")]
    [SerializeField] SLOT[] slots;
    [SerializeField] public SLOT mouse_slot;
    [SerializeField] Image tooltip_image;
    [SerializeField] TextMeshProUGUI tooltip_description_text;

    void Awake()
    {
        instance = this;
        this.hide_tooltip();
    }

    void OnDisable()
    {
        this.hide_tooltip();
    }

    void Update()
    {
        if (Mouse.current != null)
        {
            this.mouse_slot.transform.position = Mouse.current.position.ReadValue();
        }
    }

    public void show_tooltip(string text)
    {
        this.tooltip_image.enabled = true;
        this.tooltip_description_text.text = text;
        this.tooltip_description_text.enabled = true;
    }

    public void hide_tooltip()
    {
        this.tooltip_image.enabled = false;
        this.tooltip_description_text.enabled = false;
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
}
