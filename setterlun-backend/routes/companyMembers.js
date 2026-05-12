const express = require('express');
const router = express.Router();
const { addMember, getMember, updateMember, listMembers } = require('../controllers/companyMembersController');

router.post('/', addMember);
router.get('/:id', getMember);
router.put('/:id', updateMember);
router.get('/', listMembers);

module.exports = router;