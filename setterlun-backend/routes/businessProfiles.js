const express = require('express');
const router = express.Router();
const { createBusiness, getBusiness, updateBusiness, listBusinesses } = require('../controllers/businessProfilesController');

router.post('/', createBusiness);
router.get('/:user_id', getBusiness);
router.put('/:user_id', updateBusiness);
router.get('/', listBusinesses);

module.exports = router;