const express = require('express');
const cors = require('cors');
const path = require('path');
const jwt = require('jsonwebtoken');
const cookieParser = require('cookie-parser');

const app = express();

app.use(cors());
app.use(express.json());
app.use(cookieParser());

const JWT_SECRET = process.env.JWT_SECRET || "your_secret_key_2026";

// Simple Token Checker
const isAdminAuthenticated = (req) => {
  const token = req.cookies.adminToken;   // Read from cookie

  console.log("Token from cookie:", token ? "YES" : "NO");

  if (!token) return false;

  try {
    const decoded = jwt.verify(token, JWT_SECRET);
    return decoded.isAdmin === true;
  } catch (err) {
    console.log("Token verify failed:", err.message);
    return false;
  }
};

// API Routes
console.log("🛠️ Registering API Routes...");
app.use('/auth', require('./routes/auth'));
app.use('/users', require('./routes/users'));
app.use('/onboarding-steps', require('./routes/onboardingSteps'));
app.use('/personal-profiles', require('./routes/personalProfiles'));
app.use('/business-profiles', require('./routes/businessProfiles'));
app.use('/companies', require('./routes/companies'));
app.use('/company-members', require('./routes/companyMembers'));
app.use('/bricks', require('./routes/bricks'));
app.use('/courses', require('./routes/courses'));
app.use('/user-course-assignments', require('./routes/userCourseAssignments'));
app.use('/time-capsules', require('./routes/timeCapsules'));
console.log("✅ API Routes Registered: /auth, /users, /bricks, etc.");

// Public Login Page
app.get('/admin-login.html', (req, res) => {
  res.sendFile(path.join(__dirname, 'admin-panel', 'admin-login.html'));
});

// Protected Dashboard
app.get('/', (req, res) => {
  if (isAdminAuthenticated(req)) {
    console.log("✅ Admin authenticated - serving dashboard");
    return res.sendFile(path.join(__dirname, 'admin-panel', 'index.html'));
  } else {
    console.log("❌ No valid token - redirecting to login");
    return res.redirect('/admin-login.html');
  }
});

// Static files
app.use(express.static(path.join(__dirname, 'admin-panel')));
app.use('/js', express.static(path.join(__dirname, 'admin-panel/js')));

const PORT = process.env.PORT || 5000;
app.listen(PORT, () => {
  console.log(`✅ Server running on http://localhost:${PORT}`);
});