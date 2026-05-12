const db = require('../db');
const bcrypt = require('bcrypt');
const jwt = require('jsonwebtoken');

const JWT_SECRET = "your_secret_key"; // change later


// ====================== ADMIN LOGIN ======================
exports.adminLogin = async (req, res) => {
  try {
    const { username, password } = req.body;

    if (!username || !password) {
      return res.status(400).json({ message: 'Username and password are required' });
    }

    const [rows] = await db.query(
      'SELECT id, username, email, password, full_name, role FROM admins WHERE username = ? AND is_active = TRUE',
      [username]
    );

    if (rows.length === 0) {
      return res.status(401).json({ message: 'Invalid username or password' });
    }

    const admin = rows[0];
    const isMatch = await bcrypt.compare(password, admin.password);

    if (!isMatch) {
      return res.status(401).json({ message: 'Invalid username or password' });
    }

    // Update last login time
    await db.query('UPDATE admins SET last_login = CURRENT_TIMESTAMP WHERE id = ?', [admin.id]);

    const token = jwt.sign(
      { 
        id: admin.id, 
        username: admin.username, 
        email: admin.email,
        role: admin.role,
        isAdmin: true 
      },
      process.env.JWT_SECRET || "your_secret_key_2026",
      { expiresIn: '7d' }
    );

    res.json({
  message: 'Admin login successful',
  token,
  admin: {
    id: admin.id,
    username: admin.username,
    full_name: admin.full_name,
    email: admin.email,
    role: admin.role
  }
});

  } catch (err) {
    console.error("Admin Login Error:", err);
    res.status(500).json({ message: 'Server error during admin login' });
  }
};

// ✅ REGISTER - Improved duplicate handling
exports.register = async (req, res) => {
  try {
    const { full_name, username, email, password } = req.body;

    if (!full_name || !username || !email || !password) {
      return res.status(400).json({ message: 'Missing required fields' });
    }

    // Check if user exists
    const [existing] = await db.query(
      'SELECT id FROM users WHERE email = ? OR username = ?',
      [email, username]
    );

    if (existing.length > 0) {
      return res.status(400).json({ 
        message: 'Email or username already exists. Please use a different one or try logging in.' 
      });
    }

    const hashedPassword = await bcrypt.hash(password, 10);

    const [result] = await db.query(
      `INSERT INTO users (full_name, username, email, password)
       VALUES (?, ?, ?, ?)`,
      [full_name, username, email, hashedPassword]
    );

    res.json({
      message: 'User registered successfully',
      userId: result.insertId
    });

  } catch (err) {
    console.error("Register Error:", err);
    res.status(500).json({ message: 'Failed to register user' });
  }
};

// ✅ LOGIN
// ✅ LOGIN - Support BOTH Email OR Username
exports.login = async (req, res) => {
  try {
    const { login, password } = req.body;   // 'login' can be email or username

    if (!login || !password) {
      return res.status(400).json({ message: 'Email/Username and password are required' });
    }

    console.log("🔑 Login attempt with:", login);

    // Search by email OR username
    const [rows] = await db.query(
      'SELECT * FROM users WHERE email = ? OR username = ?',
      [login, login]
    );

    if (rows.length === 0) {
      return res.status(401).json({ message: 'Invalid email/username or password' });
    }

    const user = rows[0];

    // Compare password with bcrypt
    const isMatch = await bcrypt.compare(password, user.password);

    console.log("Password match result:", isMatch);   // For debugging

    if (!isMatch) {
      return res.status(401).json({ message: 'Invalid email/username or password' });
    }

    // Generate JWT token
    const token = jwt.sign(
      { id: user.id, email: user.email, username: user.username },
      JWT_SECRET,
      { expiresIn: '7d' }
    );

    res.json({
      message: 'Login successful',
      token,
      user: {
        id: user.id,
        full_name: user.full_name,
        username: user.username,
        email: user.email
      }
    });

  } catch (err) {
    console.error("Login Error:", err);
    res.status(500).json({ message: 'Server error during login' });
  }
};