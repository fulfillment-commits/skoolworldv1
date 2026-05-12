const express = require('express');
const router = express.Router();
const { createCourse, getCourse, updateCourse, listCourses } = require('../controllers/coursesController');

router.post('/', createCourse);
router.get('/:id', getCourse);
router.put('/:id', updateCourse);
router.get('/', listCourses);

module.exports = router;