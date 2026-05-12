const express = require('express');
const router = express.Router();
const { createProfile, getProfile, updateProfile, listProfiles } = require('../controllers/personalProfilesController');

router.post('/', createProfile);
router.get('/:user_id', getProfile);
router.put('/:user_id', updateProfile);
router.get('/', listProfiles);

module.exports = router;