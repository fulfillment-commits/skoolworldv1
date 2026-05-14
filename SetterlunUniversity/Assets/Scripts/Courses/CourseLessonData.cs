using System;
using System.Collections.Generic;
using UnityEngine;

namespace SetterlunUniversity.Courses
{
    [Serializable]
    public class CourseLessonEntry
    {
        [Tooltip("Lesson title shown in the chapter list.")]
        public string title = "Lesson Title";

        [Tooltip("Short description shown below the title in the panel.")]
        [TextArea(2, 4)]
        public string description = "";

        [Tooltip("Vimeo or Loom URL. Share URLs are auto-converted to embed URLs at runtime.")]
        public string videoUrl = "";

        [Tooltip("Optional thumbnail sprite shown in the lesson row.")]
        public Sprite thumbnail;
    }

    /// <summary>
    /// ScriptableObject that holds all lessons for one course (Business Model or Business Portal).
    /// Create via Assets > Create > Setterlun > Course Lesson Data.
    /// </summary>
    [CreateAssetMenu(
        fileName = "NewCourseLessonData",
        menuName = "Setterlun/Course Lesson Data",
        order = 50)]
    public class CourseLessonData : ScriptableObject
    {
        [Tooltip("Course name shown in the panel header.")]
        public string courseName = "Course Name";

        [Tooltip("Course subtitle / short description shown under the header.")]
        [TextArea(2, 3)]
        public string courseDescription = "";

        [Tooltip("Accent colour used for the panel header background.")]
        public Color accentColor = new Color(0.13f, 0.55f, 0.88f, 1f);

        [Tooltip("Ordered list of lessons. Top = Lesson 1.")]
        public List<CourseLessonEntry> lessons = new List<CourseLessonEntry>();

        // ---------------------------------------------------------------
        // Completion state (stored in PlayerPrefs per lesson per course).
        // ---------------------------------------------------------------
        private string PrefKey(int index) => $"Course_{name}_Lesson_{index}_Done";

        public bool IsCompleted(int index)
        {
            if (index < 0 || index >= lessons.Count) return false;
            return PlayerPrefs.GetInt(PrefKey(index), 0) == 1;
        }

        public void SetCompleted(int index, bool done)
        {
            if (index < 0 || index >= lessons.Count) return;
            PlayerPrefs.SetInt(PrefKey(index), done ? 1 : 0);
            PlayerPrefs.Save();
        }

        public void ResetAllProgress()
        {
            for (int i = 0; i < lessons.Count; i++)
                PlayerPrefs.DeleteKey(PrefKey(i));
            PlayerPrefs.Save();
        }

        public int CompletedCount()
        {
            int count = 0;
            for (int i = 0; i < lessons.Count; i++)
                if (IsCompleted(i)) count++;
            return count;
        }
    }
}
