using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Desktopirates
{
    /// <summary>
    /// Lets a slider preview its value without applying an expensive or disruptive change
    /// until the pointer is released. Keyboard changes are committed on deselect/submit.
    /// </summary>
    public sealed class DeferredSliderCommit : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, ISubmitHandler, IDeselectHandler
    {
        private Slider slider;
        private Action<float> preview;
        private Action<float> commit;
        private bool pointerHeld;
        private float committedValue;

        public void Initialize(Slider source, Action<float> previewChanged, Action<float> valueCommitted)
        {
            slider = source;
            preview = previewChanged;
            commit = valueCommitted;
            committedValue = source.value;
            source.onValueChanged.AddListener(HandleValueChanged);
            preview?.Invoke(source.value);
        }

        public void OnPointerDown(PointerEventData eventData) => pointerHeld = true;

        public void OnPointerUp(PointerEventData eventData)
        {
            pointerHeld = false;
            CommitIfChanged();
        }

        private void Update()
        {
            // The native window is resized after release, which can prevent Unity from
            // receiving the normal PointerUp on a frameless layered window. Polling the
            // physical button guarantees exactly one commit without moving the slider.
            if (pointerHeld && !Input.GetMouseButton(0))
            {
                pointerHeld = false;
                CommitIfChanged();
            }
        }

        public void OnSubmit(BaseEventData eventData) => CommitIfChanged();

        public void OnDeselect(BaseEventData eventData)
        {
            if (!pointerHeld) CommitIfChanged();
        }

        private void HandleValueChanged(float value) => preview?.Invoke(value);

        private void CommitIfChanged()
        {
            if (slider == null || Mathf.Approximately(slider.value, committedValue)) return;
            committedValue = slider.value;
            commit?.Invoke(slider.value);
        }
    }
}
