const pool = require('../db');

async function createCompany(req, res) {
  const { name, logo_url, owner_id } = req.body;
  try {
    const [result] = await pool.query(
      `INSERT INTO companies (name, logo_url, owner_id) VALUES (?, ?, ?)`,
      [name, logo_url, owner_id]
    );
    res.status(201).json({ companyId: result.insertId });
  } catch (err) {
    console.error(err);
    res.status(500).json({ error: 'Failed to create company' });
  }
}

async function getCompany(req, res) {
  const id = req.params.id;
  try {
    const [rows] = await pool.query(`SELECT * FROM companies WHERE id = ?`, [id]);
    if (!rows.length) return res.status(404).json({ error: 'Company not found' });
    res.json(rows[0]);
  } catch (err) {
    console.error(err);
    res.status(500).json({ error: 'Failed to fetch company' });
  }
}

async function updateCompany(req, res) {
  const id = req.params.id;
  const updates = req.body;
  const fields = Object.keys(updates).map(k => `${k} = ?`).join(', ');
  const values = Object.values(updates);
  try {
    await pool.query(`UPDATE companies SET ${fields}, updated_at = CURRENT_TIMESTAMP WHERE id = ?`, [...values, id]);
    res.json({ success: true });
  } catch (err) {
    console.error(err);
    res.status(500).json({ error: 'Failed to update company' });
  }
}

async function listCompanies(req, res) {
  try {
    const [rows] = await pool.query(`SELECT * FROM companies ORDER BY created_at DESC`);
    res.json(rows);
  } catch (err) {
    console.error(err);
    res.status(500).json({ error: 'Failed to fetch companies' });
  }
}

module.exports = { createCompany, getCompany, updateCompany, listCompanies };