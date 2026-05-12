const express = require('express');
const router = express.Router();
const { createCapsule, getCapsulesByUser, listCapsules } = require('../controllers/timeCapsulesController');

router.post('/', createCapsule);
router.get('/:user_id', getCapsulesByUser);
router.get('/', listCapsules);

module.exports = router;