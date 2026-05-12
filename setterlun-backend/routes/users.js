const express = require('express');
const router = express.Router();
const { createUser, getUser, updateUser, listUsers, deleteUser, updateAvatar } = require('../controllers/usersController');

// NEW: Avatar Update
router.put('/:id/avatar', updateAvatar);

// Existing routes
router.post('/', createUser);
router.get('/:id', getUser);
router.put('/:id', updateUser);
router.get('/', listUsers);

// NEW: Delete User
router.delete('/:id', deleteUser);

module.exports = router;