const pool = require('../db');

async function assignCourse(req, res) {
  const { user_id, course_id, status, progress } = req.body;
  try {
    const [result] = await pool.query(
      `INSERT INTO user_course_assignments (user_id, course_id, status, progress) VALUES (?, ?, ?, ?)`,
      [user_id, course_id, status || 'locked', progress || 0]
    );
    res.status(201).json({ assignmentId: result.insertId });
  } catch (err) {
    console.error(err);
    res.status(500).json({ error: 'Failed to assign course' });
  }
}

async function getUserCourses(req, res) {
  const user_id = req.params.user_id;
  try {
    const [rows] = await pool.query(`SELECT * FROM user_course_assignments WHERE user_id = ?`, [user_id]);
    res.json(rows);
  } catch (err) {
    console.error(err);
    res.status(500).json({ error: 'Failed to fetch user courses' });
  }
}

async function updateUserCourse(req, res) {
  const { user_id, course_id } = req.params;
  const updates = req.body;

  const fields = Object.keys(updates).map(k => `${k} = ?`).join(', ');
  const values = Object.values(updates);

  try {
    await pool.query(`UPDATE user_course_assignments SET ${fields}, completed_at = CURRENT_TIMESTAMP WHERE user_id = ? AND course_id = ?`, [...values, user_id, course_id]);
    res.json({ success: true });
  } catch (err) {
    console.error(err);
    res.status(500).json({ error: 'Failed to update user course' });
  }
}

async function listAssignments(req, res) {
  try {
    const [rows] = await pool.query(`SELECT * FROM user_course_assignments ORDER BY assigned_at DESC`);
    res.json(rows);
  } catch (err) {
    console.error(err);
    res.status(500).json({ error: 'Failed to fetch assignments' });
  }
}

module.exports = { assignCourse, getUserCourses, updateUserCourse, listAssignments };