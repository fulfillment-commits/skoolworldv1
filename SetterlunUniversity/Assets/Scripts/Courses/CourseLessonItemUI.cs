using UnityEngine;
using UnityEngine.UI;
using TMPro;
using SetterlunUniversity.Courses;

namespace SetterlunUniversity.Courses
{
    /// <summary>
    /// Controls one lesson row inside the horizontal chapter list.
    /// Attach to the Lesson Item prefab.
    /// </summary>
    public class CourseLessonItemUI : MonoBehaviour
    {
        [Header("Labels")]
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text lessonNumberText;

        [Header("Completion")]
        [SerializeField] private GameObject completeOverlay;   // semi-transparent overlay when done
        [SerializeField] private Image completeIcon;           // checkmark icon
        [SerializeField] private Color completeIconColor = new Color(0.18f, 0.80f, 0.44f);
        [SerializeField] private Color incompleteIconColor = new Color(0.75f, 0.75f, 0.75f);

        [Header("Selection")]
        [SerializeField] private GameObject selectedIndicator; // highlight ring / underline
        [SerializeField] private Button itemButton;

        // ---------------------------------------------------------------
        // State
        // ---------------------------------------------------------------
        private CoursePanelScreen _panel;
        private int _index;
        private bool _isCompleted;

        // ---------------------------------------------------------------
        // Setup
        // ---------------------------------------------------------------
        public void Setup(CoursePanelScreen panel, int index, CourseLessonEntry entry, bool completed)
        {
            _panel = panel;
            _index = index;
            _isCompleted = completed;

            if (lessonNumberText != null)
                lessonNumberText.text = $"{index + 1:00}";

            if (titleText != null)
                titleText.text = entry.title;

            RefreshVisuals(completed, isSelected: false);

            if (itemButton != null)
            {
                itemButton.onClick.RemoveAllListeners();
                itemButton.onClick.AddListener(OnClick);
            }
        }

        // ---------------------------------------------------------------
        // State updates
        // ---------------------------------------------------------------
        public void SetSelected(bool selected)
        {
            if (selectedIndicator != null)
                selectedIndicator.SetActive(selected);
        }

        public void SetCompleted(bool completed)
        {
            _isCompleted = completed;
            RefreshVisuals(completed, selectedIndicator != null && selectedIndicator.activeSelf);
        }

        private void RefreshVisuals(bool completed, bool isSelected)
        {
            if (completeIcon != null)
                completeIcon.color = completed ? completeIconColor : incompleteIconColor;

            if (completeOverlay != null)
                completeOverlay.SetActive(completed);

            if (selectedIndicator != null)
                selectedIndicator.SetActive(isSelected);
        }

        // ---------------------------------------------------------------
        // Interaction
        // ---------------------------------------------------------------
        private void OnClick()
        {
            _panel?.OnLessonItemClicked(_index);
        }
    }
}
