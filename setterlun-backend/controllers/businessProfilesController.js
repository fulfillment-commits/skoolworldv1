const pool = require('../db');

async function createBusiness(req, res) {
  const {
    user_id, business_name, business_website, social_links,
    monthly_revenue, business_type, primary_model, products_services,
    lead_sources, sales_issues, sales_process_status, fulfillment_challenges,
    tools_used, authority_level, active_authority_building
  } = req.body;

  try {
    const [result] = await pool.query(
      `INSERT INTO business_profiles
      (user_id, business_name, business_website, social_links, monthly_revenue, business_type, primary_model, products_services,
      lead_sources, sales_issues, sales_process_status, fulfillment_challenges, tools_used, authority_level, active_authority_building)
      VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)`,
      [
        user_id, business_name, business_website, JSON.stringify(social_links || {}),
        monthly_revenue, business_type, primary_model, JSON.stringify(products_services || []),
        JSON.stringify(lead_sources || []), JSON.stringify(sales_issues || []),
        sales_process_status, JSON.stringify(fulfillment_challenges || []), JSON.stringify(tools_used || []),
        authority_level, active_authority_building || false
      ]
    );
    res.status(201).json({ businessId: result.insertId });
  } catch (err) {
    console.error(err);
    res.status(500).json({ error: 'Failed to create business profile' });
  }
}

async function getBusiness(req, res) {
  const user_id = req.params.user_id;
  try {
    const [rows] = await pool.query(`SELECT * FROM business_profiles WHERE user_id = ?`, [user_id]);
    if (!rows.length) return res.status(404).json({ error: 'Business profile not found' });
    res.json(rows[0]);
  } catch (err) {
    console.error(err);
    res.status(500).json({ error: 'Failed to fetch business profile' });
  }
}

async function updateBusiness(req, res) {
  const user_id = req.params.user_id;
  const updates = req.body;

  // Convert arrays/objects to JSON
  ['social_links', 'products_services', 'lead_sources', 'sales_issues', 'fulfillment_challenges', 'tools_used'].forEach(field => {
    if (updates[field]) updates[field] = JSON.stringify(updates[field]);
  });

  const fields = Object.keys(updates).map(k => `${k} = ?`).join(', ');
  const values = Object.values(updates);

  try {
    await pool.query(`UPDATE business_profiles SET ${fields}, updated_at = CURRENT_TIMESTAMP WHERE user_id = ?`, [...values, user_id]);
    res.json({ success: true });
  } catch (err) {
    console.error(err);
    res.status(500).json({ error: 'Failed to update business profile' });
  }
}

async function listBusinesses(req, res) {
  try {
    const [rows] = await pool.query(`SELECT * FROM business_profiles ORDER BY created_at DESC`);
    res.json(rows);
  } catch (err) {
    console.error(err);
    res.status(500).json({ error: 'Failed to fetch business profiles' });
  }
}

module.exports = { createBusiness, getBusiness, updateBusiness, listBusinesses };