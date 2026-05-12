const pool = require('../db');

async function createStep(req, res) {
  const { user_id, step_number, completed, data_json } = req.body;
  try {
    const [result] = await pool.query(
      `INSERT INTO onboarding_steps (user_id, step_number, completed, data_json) VALUES (?, ?, ?, ?)`,
      [user_id, step_number, completed || false, JSON.stringify(data_json || {})]
    );
    res.status(201).json({ stepId: result.insertId });
  } catch (err) {
    console.error(err);
    res.status(500).json({ error: 'Failed to create onboarding step' });
  }
}

async function getSteps(req, res) {
  const user_id = req.params.user_id;
  try {
    const [rows] = await pool.query(`SELECT * FROM onboarding_steps WHERE user_id = ? ORDER BY step_number`, [user_id]);
    res.json(rows);
  } catch (err) {
    console.error(err);
    res.status(500).json({ error: 'Failed to fetch onboarding steps' });
  }
}

async function updateStep(req, res) {
  const { user_id, step_number } = req.params;
  const { completed, data_json } = req.body;
  
  const finalDataJson = typeof data_json === 'object' ? JSON.stringify(data_json) : data_json;

  try {
    // Check if step exists
    const [existing] = await pool.query(
      `SELECT id FROM onboarding_steps WHERE user_id = ? AND step_number = ?`,
      [user_id, step_number]
    );

    if (existing.length > 0) {
      // Update existing
      await pool.query(
        `UPDATE onboarding_steps SET completed = ?, data_json = ?, completed_at = CURRENT_TIMESTAMP WHERE user_id = ? AND step_number = ?`,
        [completed || true, finalDataJson || '{}', user_id, step_number]
      );
    } else {
      // Insert new if not exists (upsert)
      await pool.query(
        `INSERT INTO onboarding_steps (user_id, step_number, completed, data_json, completed_at) VALUES (?, ?, ?, ?, CURRENT_TIMESTAMP)`,
        [user_id, step_number, completed || true, finalDataJson || '{}']
      );
    }
    
    res.json({ success: true, message: 'Step progress saved' });
  } catch (err) {
    console.error('Error in updateStep:', err);
    res.status(500).json({ error: 'Failed to save onboarding step progress' });
  }
}

async function listSteps(req, res) {
  try {
    const [rows] = await pool.query(`SELECT * FROM onboarding_steps ORDER BY user_id, step_number`);
    res.json(rows);
  } catch (err) {
    console.error(err);
    res.status(500).json({ error: 'Failed to fetch onboarding steps' });
  }
}

module.exports = { createStep, getSteps, updateStep, listSteps };