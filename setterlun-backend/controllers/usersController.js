const pool = require('../db');
const bcrypt = require('bcrypt');

console.log("✅ Users Controller Loaded");



// ✅ CREATE USER - Improved with clear duplicate handling
async function createUser(req, res) {
  const {
    full_name,
    username,
    email,
    phone,
    timezone,
    discovery_source,
    referral_code,
    referred_by,
    password
  } = req.body;

  try {
    console.log("=== [USER CREATION] REQUEST RECEIVED ===");
    console.log("Body:", { full_name, username, email, discovery_source });

    // Basic validation
    if (!full_name || !username || !email || !password) {
      return res.status(400).json({ 
        error: 'Missing required fields: full_name, username, email, password' 
      });
    }

    // Hash password
    let hashedPassword = null;
    if (password) {
      hashedPassword = await bcrypt.hash(password, 10);
    }

    const [result] = await pool.query(
      `INSERT INTO users 
       (full_name, username, email, phone, timezone, discovery_source, 
        referral_code, referred_by, password)
       VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?)`,
      [
        full_name,
        username,
        email,
        phone || null,
        timezone || null,
        discovery_source || null,
        referral_code || null,
        referred_by || null,
        hashedPassword
      ]
    );

    console.log(`✅ User created successfully! ID: ${result.insertId}`);

    res.status(201).json({
      message: 'User created successfully',
      userId: result.insertId
    });

  } catch (err) {
    console.error("=== [CREATE USER ERROR] ===");
    console.error("Error Code:", err.code);
    console.error("SQL Message:", err.sqlMessage);

    // Handle duplicate entry (most important for Register Screen)
    if (err.code === 'ER_DUP_ENTRY') {
      return res.status(400).json({ 
        error: 'Email or username already exists. Please use a different one or try logging in.' 
      });
    }

    // Other errors
    res.status(500).json({ 
      error: 'Failed to create user',
      details: err.sqlMessage || err.message 
    });
  }
}



// ================================
// GET SINGLE USER (NO PASSWORD)
// ================================
async function getUser(req, res) {
  const id = req.params.id;

  try {
    const [rows] = await pool.query(
      `SELECT 
        id, full_name, username, email, phone, timezone, discovery_source,
        referral_code, referred_by, avatar_json, joined_date,
        current_step, status, created_at, updated_at
       FROM users WHERE id = ?`,
      [id]
    );

    if (!rows.length) {
      return res.status(404).json({ error: 'User not found' });
    }

    res.json(rows[0]);

  } catch (err) {
    console.error('GetUser Error:', err);
    res.status(500).json({ error: 'Failed to fetch user' });
  }
}

// ================================
// UPDATE USER (SAFE + PASSWORD SUPPORT)
// ================================
async function updateUser(req, res) {
  const id = req.params.id;
  const updates = { ...req.body };

  try {
    // Hash password if updating
    if (updates.password) {
      updates.password = await bcrypt.hash(updates.password, 10);
    }

    // Prevent updating restricted fields
    delete updates.id;
    delete updates.created_at;

    const fields = Object.keys(updates).map(key => `${key} = ?`).join(', ');
    const values = Object.values(updates);

    if (!fields) {
      return res.status(400).json({ error: 'No fields to update' });
    }

    await pool.query(
      `UPDATE users 
       SET ${fields}, updated_at = CURRENT_TIMESTAMP 
       WHERE id = ?`,
      [...values, id]
    );

    res.json({ success: true });

  } catch (err) {
    console.error('UpdateUser Error:', err);
    res.status(500).json({ error: 'Failed to update user' });
  }
}

// ================================
// LIST ALL USERS (NO PASSWORD)
// ================================
async function listUsers(req, res) {
  try {
    const [rows] = await pool.query(
      `SELECT 
        id, full_name, username, email, phone, timezone,
        discovery_source, joined_date, status, created_at, updated_at
       FROM users
       ORDER BY created_at DESC`
    );

    res.json(rows);

  } catch (err) {
    console.error('ListUsers Error:', err);
    res.status(500).json({ error: 'Failed to fetch users' });
  }
}

// ====================== DELETE USER ======================
async function deleteUser(req, res) {
  const id = req.params.id;

  try {
    // Optional: Prevent deleting the last admin or yourself (if needed)
    const [user] = await pool.query('SELECT * FROM users WHERE id = ?', [id]);
    if (!user.length) {
      return res.status(404).json({ error: 'User not found' });
    }

    await pool.query('DELETE FROM users WHERE id = ?', [id]);

    console.log(`User deleted: ID ${id}`);
    res.json({ success: true, message: 'User deleted successfully' });

  } catch (err) {
    console.error('DeleteUser Error:', err);
    res.status(500).json({ error: 'Failed to delete user' });
  }
}


//// ================================
// UPDATE AVATAR INDEX (Using user_avatars table)
// ================================
async function updateAvatar(req, res) {
  const { id } = req.params;
  const { avatar_index, hair_color_index, hair_style_index, outfit_index } = req.body;

  console.log(`\n--- [AVATAR UPDATE REQUEST] ---`);
  console.log(`User ID: ${id}`);
  console.log(`Body:`, req.body);

  if (avatar_index === undefined) {
    console.log(`❌ Missing avatar_index`);
    return res.status(400).json({ error: 'avatar_index is required' });
  }

  try {
    // 1. Verify user exists first (to avoid FK errors)
    const [userExists] = await pool.query('SELECT id FROM users WHERE id = ?', [id]);
    if (userExists.length === 0) {
      console.log(`❌ User ${id} not found in users table`);
      return res.status(404).json({ error: 'User not found in database' });
    }

    // 2. UPSERT pattern for user_avatars
    const [result] = await pool.query(
      `INSERT INTO user_avatars (user_id, avatar_index, hair_color_index, hair_style_index, outfit_index)
       VALUES (?, ?, ?, ?, ?)
       ON DUPLICATE KEY UPDATE 
       avatar_index = VALUES(avatar_index),
       hair_color_index = COALESCE(VALUES(hair_color_index), hair_color_index),
       hair_style_index = COALESCE(VALUES(hair_style_index), hair_style_index),
       outfit_index = COALESCE(VALUES(outfit_index), outfit_index),
       updated_at = CURRENT_TIMESTAMP`,
      [
        id, 
        avatar_index, 
        hair_color_index || 0, 
        hair_style_index || 0, 
        outfit_index || 0
      ]
    );

    console.log(`✅ Avatar record updated/created for User ID ${id}: Index ${avatar_index}`);
    res.json({ success: true, message: 'Avatar updated successfully in separate table' });

  } catch (err) {
    console.error('UpdateAvatar Error:', err);
    res.status(500).json({ 
      error: 'Failed to update avatar in separate table',
      details: err.message 
    });
  }
}

// ================================
// EXPORT ALL CONTROLLERS
// ================================
module.exports = {
  createUser,
  getUser,
  updateUser,
  listUsers,
  deleteUser,
  updateAvatar
};