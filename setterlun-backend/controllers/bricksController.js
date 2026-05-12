const pool = require('../db');

async function createBrick(req, res) {
  console.log("🧱 POST /bricks hit with body:", req.body);
  const { user_id, name_on_brick, business_name, message, brick_position } = req.body;
  try {
    console.log("💾 Attempting to insert brick into database...");
    const [result] = await pool.query(
      `INSERT INTO bricks (user_id, name_on_brick, business_name, message, brick_position) VALUES (?, ?, ?, ?, ?)`,
      [user_id, name_on_brick, business_name, message, JSON.stringify(brick_position || {})]
    );
    console.log("✅ Brick created successfully, ID:", result.insertId);
    res.status(201).json({ brickId: result.insertId });
  } catch (err) {
    console.error("❌ Error creating brick:", err);
    res.status(500).json({ error: 'Failed to create brick' });
  }
}

async function getBricksByUser(req, res) {
  const user_id = req.params.user_id;
  try {
    const [rows] = await pool.query(`SELECT * FROM bricks WHERE user_id = ?`, [user_id]);
    res.json(rows);
  } catch (err) {
    console.error(err);
    res.status(500).json({ error: 'Failed to fetch bricks' });
  }
}

async function updateBrick(req, res) {
  const { id } = req.params;
  const updates = req.body;
  if (updates.brick_position) updates.brick_position = JSON.stringify(updates.brick_position);

  const fields = Object.keys(updates).map(k => `${k} = ?`).join(', ');
  const values = Object.values(updates);

  try {
    await pool.query(`UPDATE bricks SET ${fields}, updated_at = CURRENT_TIMESTAMP WHERE id = ?`, [...values, id]);
    res.json({ success: true });
  } catch (err) {
    console.error(err);
    res.status(500).json({ error: 'Failed to update brick' });
  }
}

async function listBricks(req, res) {
  try {
    const [rows] = await pool.query(`SELECT * FROM bricks ORDER BY created_at DESC`);
    res.json(rows);
  } catch (err) {
    console.error(err);
    res.status(500).json({ error: 'Failed to fetch bricks' });
  }
}

module.exports = { createBrick, getBricksByUser, updateBrick, listBricks };