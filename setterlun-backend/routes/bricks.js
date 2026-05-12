const express = require('express');
const router = express.Router();
const { createBrick, getBricksByUser, updateBrick, listBricks } = require('../controllers/bricksController');

router.post('/', createBrick);
router.get('/:user_id', getBricksByUser);
router.put('/:id', updateBrick);
router.get('/', listBricks);

module.exports = router;