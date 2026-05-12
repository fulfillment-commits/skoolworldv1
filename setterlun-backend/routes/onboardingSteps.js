const express = require('express');
const router = express.Router();
const { createStep, getSteps, updateStep, listSteps } = require('../controllers/onboardingStepsController');

router.post('/', createStep);
router.get('/:user_id', getSteps);
router.put('/:user_id/:step_number', updateStep);
router.get('/', listSteps);

module.exports = router;