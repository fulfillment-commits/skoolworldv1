const pool = require('../db');

async function createProfile(req, res) {
  const {
    user_id, bio, email_visibility, phone_visibility,
    city, country, skills, ads, seo, content_creation, other_skills
  } = req.body;

  try {
    const [result] = await pool.query(
      `INSERT INTO personal_profiles 
      (user_id, bio, email_visibility, phone_visibility, city, country, skills, ads, seo, content_creation, other_skills)
      VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)`,
      [
        user_id, bio, email_visibility || 'private', phone_visibility || 'private',
        city, country, JSON.stringify(skills || []),
        ads || false, seo || false, content_creation || false, JSON.stringify(other_skills || [])
      ]
    );
    res.status(201).json({ profileId: result.insertId });
  } catch (err) {
    console.error(err);
    res.status(500).json({ error: 'Failed to create profile' });
  }
}

async function getProfile(req, res) {
  const user_id = req.params.user_id;
  try {
    const [rows] = await pool.query(`SELECT * FROM personal_profiles WHERE user_id = ?`, [user_id]);
    if (!rows.length) return res.status(404).json({ error: 'Profile not found' });
    res.json(rows[0]);
  } catch (err) {
    console.error(err);
    res.status(500).json({ error: 'Failed to fetch profile' });
  }
}

async function updateProfile(req, res) {
  const user_id = req.params.user_id;
  const updates = req.body;

  // Convert arrays to JSON if needed
  if (updates.skills) updates.skills = JSON.stringify(updates.skills);
  if (updates.other_skills) updates.other_skills = JSON.stringify(updates.other_skills);

  const fields = Object.keys(updates).map(k => `${k} = ?`).join(', ');
  const values = Object.values(updates);

  try {
    await pool.query(`UPDATE personal_profiles SET ${fields}, updated_at = CURRENT_TIMESTAMP WHERE user_id = ?`, [...values, user_id]);
    res.json({ success: true });
  } catch (err) {
    console.error(err);
    res.status(500).json({ error: 'Failed to update profile' });
  }
}

async function listProfiles(req, res) {
  try {
    const [rows] = await pool.query(`SELECT * FROM personal_profiles ORDER BY created_at DESC`);
    res.json(rows);
  } catch (err) {
    console.error(err);
    res.status(500).json({ error: 'Failed to fetch profiles' });
  }
}

module.exports = { createProfile, getProfile, updateProfile, listProfiles };