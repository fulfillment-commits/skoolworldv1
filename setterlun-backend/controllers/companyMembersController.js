const pool = require('../db');

async function addMember(req, res) {
  const { company_id, user_id, role } = req.body;
  try {
    const [result] = await pool.query(
      `INSERT INTO company_members (company_id, user_id, role) VALUES (?, ?, ?)`,
      [company_id, user_id, role || 'member']
    );
    res.status(201).json({ memberId: result.insertId });
  } catch (err) {
    console.error(err);
    res.status(500).json({ error: 'Failed to add member' });
  }
}

async function getMember(req, res) {
  const id = req.params.id;
  try {
    const [rows] = await pool.query(`SELECT * FROM company_members WHERE id = ?`, [id]);
    if (!rows.length) return res.status(404).json({ error: 'Member not found' });
    res.json(rows[0]);
  } catch (err) {
    console.error(err);
    res.status(500).json({ error: 'Failed to fetch member' });
  }
}

async function updateMember(req, res) {
  const id = req.params.id;
  const updates = req.body;
  const fields = Object.keys(updates).map(k => `${k} = ?`).join(', ');
  const values = Object.values(updates);
  try {
    await pool.query(`UPDATE company_members SET ${fields} WHERE id = ?`, [...values, id]);
    res.json({ success: true });
  } catch (err) {
    console.error(err);
    res.status(500).json({ error: 'Failed to update member' });
  }
}

async function listMembers(req, res) {
  try {
    const [rows] = await pool.query(`SELECT * FROM company_members ORDER BY joined_at DESC`);
    res.json(rows);
  } catch (err) {
    console.error(err);
    res.status(500).json({ error: 'Failed to fetch members' });
  }
}

module.exports = { addMember, getMember, updateMember, listMembers };