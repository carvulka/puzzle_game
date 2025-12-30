using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class SLOT : MonoBehaviour, IPointerDownHandler, IPointerEnterHandler, IPointerExitHandler
{
    [Header("components/children")]
    [SerializeField] Image stack_image;
    [SerializeField] TextMeshProUGUI stack_count_text;

    //state
    ITEM item = null;
    Stack<COLLECTIBLE.UNIQUE> stack = new Stack<COLLECTIBLE.UNIQUE>();

    void Awake()
    {
        this.stack_image.enabled = false;
        this.stack_count_text.enabled = false;
    }

    public void OnPointerEnter(PointerEventData event_data)
    {
        if (this.stack.Count > 0)
        {
            INVENTORY.instance.show_tooltip(this.item.description);
        }
    }

    public void OnPointerExit(PointerEventData event_data)
    {
        INVENTORY.instance.hide_tooltip();
    }

    public void OnPointerDown(PointerEventData event_data)
    {
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            this.exchange(INVENTORY.instance.mouse_slot);
            if (this.stack.Count > 0)
            {
                INVENTORY.instance.show_tooltip(this.item.description);
            }
        }
    }

    public void exchange(SLOT other)
    {
        (this.item, other.item) = (other.item, this.item);
        (this.stack, other.stack) = (other.stack, this.stack);
        this.update_stack_image();
        this.update_stack_count_text();
        other.update_stack_image();
        other.update_stack_count_text();
    }

    public bool try_merge(ITEM item, COLLECTIBLE.UNIQUE unique)
    {
        if (this.stack.Count > 0 && this.item == item)
        {
            this.stack.Push(unique);
            this.update_stack_count_text();
            return true;
        }
        return false;
    }

    public bool try_allot(ITEM item, COLLECTIBLE.UNIQUE unique)
    {
        if (this.stack.Count == 0)
        {
            this.item = item;
            this.stack.Push(unique);
            this.update_stack_image();
            this.update_stack_count_text();
            return true;
        }
        return false;
    }

    public (ITEM item, COLLECTIBLE.UNIQUE unique) try_remove()
    {
        if (this.stack.Count > 0)
        {
            ITEM item = this.item;
            COLLECTIBLE.UNIQUE unique = this.stack.Pop();
            this.update_stack_image();
            this.update_stack_count_text();
            return (item, unique);
        }
        return (null, default);
    }

    public (ITEM item, COLLECTIBLE.UNIQUE unique) try_get()
    {
        if (this.stack.Count > 0)
        {
            return (this.item, this.stack.Peek());
        }
        return (null, default);
    }

    void update_stack_image()
    {
        this.stack_image.sprite = this.stack.Count > 0 ? item.sprite : null;
        this.stack_image.enabled = this.stack.Count > 0;
    }

    void update_stack_count_text()
    {
        this.stack_count_text.text = this.stack.Count > 1 ? this.stack.Count.ToString() : "";
        this.stack_count_text.enabled = this.stack.Count > 1;
    }
}
