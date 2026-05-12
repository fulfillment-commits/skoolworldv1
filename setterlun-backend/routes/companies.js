const express = require('express');
const router = express.Router();
const { createCompany, getCompany, updateCompany, listCompanies } = require('../controllers/companiesController');

router.post('/', createCompany);
router.get('/:id', getCompany);
router.put('/:id', updateCompany);
router.get('/', listCompanies);

module.exports = router;