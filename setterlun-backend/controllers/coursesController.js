const pool = require('../db');

async function createCourse(req, res) {
  const { name, description, video_link, recommended_for, universal } = req.body;
  try {
    const [result] = await pool.query(
      `INSERT INTO courses (name, description, video_link, recommended_for, universal)
       VALUES (?, ?, ?, ?, ?)`,
      [name, description, video_link, JSON.stringify(recommended_for || []), universal || false]
    );
    res.status(201).json({ courseId: result.insertId });
  } catch (err) {
    console.error(err);
    res.status(500).json({ error: 'Failed to create course' });
  }
}

async function getCourse(req, res) {
  const id = req.params.id;
  try {
    const [rows] = await pool.query(`SELECT * FROM courses WHERE id = ?`, [id]);
    if (!rows.length) return res.status(404).json({ error: 'Course not found' });
    res.json(rows[0]);
  } catch (err) {
    console.error(err);
    res.status(500).json({ error: 'Failed to fetch course' });
  }
}

async function updateCourse(req, res) {
  const id = req.params.id;
  const updates = req.body;
  if (updates.recommended_for) updates.recommended_for = JSON.stringify(updates.recommended_for);

  const fields = Object.keys(updates).map(k => `${k} = ?`).join(', ');
  const values = Object.values(updates);

  try {
    await pool.query(`UPDATE courses SET ${fields}, updated_at = CURRENT_TIMESTAMP WHERE id = ?`, [...values, id]);
    res.json({ success: true });
  } catch (err) {
    console.error(err);
    res.status(500).json({ error: 'Failed to update course' });
  }
}

async function listCourses(req, res) {
  try {
    const [rows] = await pool.query(`SELECT * FROM courses ORDER BY created_at DESC`);
    res.json(rows);
  } catch (err) {
    console.error(err);
    res.status(500).json({ error: 'Failed to fetch courses' });
  }
}

module.exports = { createCourse, getCourse, updateCourse, listCourses };