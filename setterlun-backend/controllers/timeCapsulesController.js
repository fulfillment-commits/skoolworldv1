const pool = require('../db');

async function createCapsule(req, res) {
  const { user_id, message, lock_until } = req.body;
  try {
    const [result] = await pool.query(
      `INSERT INTO time_capsules (user_id, message, lock_until) VALUES (?, ?, ?)`,
      [user_id, message, lock_until]
    );
    res.status(201).json({ capsuleId: result.insertId });
  } catch (err) {
    console.error(err);
    res.status(500).json({ error: 'Failed to create time capsule' });
  }
}

async function getCapsulesByUser(req, res) {
  const user_id = req.params.user_id;
  try {
    const [rows] = await pool.query(`SELECT * FROM time_capsules WHERE user_id = ? ORDER BY created_at DESC`, [user_id]);
    res.json(rows);
  } catch (err) {
    console.error(err);
    res.status(500).json({ error: 'Failed to fetch time capsules' });
  }
}

async function listCapsules(req, res) {
  try {
    const [rows] = await pool.query(`SELECT * FROM time_capsules ORDER BY created_at DESC`);
    res.json(rows);
  } catch (err) {
    console.error(err);
    res.status(500).json({ error: 'Failed to fetch time capsules' });
  }
}

module.exports = { createCapsule, getCapsulesByUser, listCapsules };