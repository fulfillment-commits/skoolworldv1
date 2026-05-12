const express = require('express');
const router = express.Router();
const authController = require('../controllers/authController');

// User routes
router.post('/register', authController.register);
router.post('/login', authController.login);

// ← ADD THIS LINE
router.post('/admin/login', authController.adminLogin);

module.exports = router;