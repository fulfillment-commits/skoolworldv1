const express = require('express');
const router = express.Router();
const { assignCourse, getUserCourses, updateUserCourse, listAssignments } = require('../controllers/userCourseAssignmentsController');

router.post('/', assignCourse);
router.get('/:user_id', getUserCourses);
router.put('/:user_id/:course_id', updateUserCourse);
router.get('/', listAssignments);

module.exports = router;