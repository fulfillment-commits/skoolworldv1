const PDFDocument = require('pdfkit');
const fs = require('fs');
const path = require('path');

const doc = new PDFDocument({ margin: 50 });
doc.font('Helvetica');
const outputPath = path.join(__dirname, '..', 'Weekly_Progress_Report_2026-04-30.pdf');
doc.pipe(fs.createWriteStream(outputPath));

// Title
doc.fontSize(24).fillColor('#2c3e50').text('Weekly Progress Report', { align: 'center' });
doc.fontSize(18).text('Setterlun University', { align: 'center' });
doc.moveDown(0.5);
doc.fontSize(12).fillColor('#7f8c8d').text('Date: April 30, 2026', { align: 'center' });
doc.text('Project: Setterlun University Development', { align: 'center' });
doc.moveDown();

// Line separator
doc.moveTo(50, doc.y).lineTo(550, doc.y).strokeColor('#bdc3c7').stroke();
doc.moveDown();

function addHeading(text) {
    doc.moveDown();
    doc.fontSize(16).fillColor('#2980b9').text(text.toUpperCase(), { underline: true });
    doc.moveDown(0.5);
}

function addSubheading(text) {
    doc.moveDown(0.5);
    doc.fontSize(14).fillColor('#34495e').text(text);
    doc.moveDown(0.3);
}

function addParagraph(text) {
    doc.fontSize(11).fillColor('#000000').text(text, { align: 'justify', lineGap: 2 });
}

function addListItem(text) {
    doc.fontSize(11).fillColor('#000000').text('• ' + text, { indent: 20, lineGap: 2 });
}

// Executive Summary
addHeading('Executive Summary');
addParagraph('This week, we focused on enhancing user interactivity, optimizing visual transitions between key areas, and establishing the groundwork for a robust Firebase-backed infrastructure. Significant progress was made on the "Claim Brick" functionality and scene transition logic, ensuring a seamless and performant experience for users. We are currently in the process of establishing and testing the connection with Firebase.');

// Section 1
addHeading('1. Core Functionality Enhancements');
addSubheading('Claim Brick Logic & Interaction');
addListItem('Trigger-Based Highlighting: Bricks now dynamically highlight when a user enters their interaction trigger area, providing immediate visual feedback. Highlights are automatically disabled upon exiting the area.');
addListItem('Form Integration: The interaction flow now correctly handles the state before and after form completion.');
addListItem('Persistence Feedback: Upon submitting data and returning to the trigger area, the system now displays the specific brick details, confirming successful data capture and persistence.');

addSubheading('Lobby Transitions & Scene Management');
addListItem('Smooth Fade Transitions: Implemented a professional Fade-In/Fade-Out effect when transitioning between the University\'s main exterior and the Lobby interior.');
addListItem('Optimized Scene Loading: The transition logic handles scene changes at the backend, ensuring that assets are loaded efficiently without jarring jumps.');
addListItem('Lighting Optimization: Separated the light baking for exterior and interior (Lobby) scenes. This improves performance by reducing the lightmap overhead and allows for higher visual fidelity.');

// Section 2
addHeading('2. Visual & Performance Optimization');
addSubheading('Lighting Fidelity');
addListItem('Interior & Exterior Baking: Refined the light baking effects to ensure a consistent aesthetic while maintaining high performance.');
addListItem('Seamless Transitions: Fine-tuned the transition timing so that the shift between different lighting environments feels natural and integrated.');

// Section 3
addHeading('3. Quest & Onboarding Development');
addSubheading('Personal Information Quest');
addListItem('Phase 1 Implementation: Successfully added "Step A" of the Personal Information Quest.');
addListItem('Local Storage: Initial implementation is stored locally to facilitate rapid testing and iteration before final backend synchronization.');

// Section 4
addHeading('4. Infrastructure & Backend Integration');
addSubheading('Firebase Integration Settings');
addListItem('Backend Toggle: Introduced a BackendConfig option that allows the development team to switch seamlessly between a Custom API and Firebase backend environments.');

addSubheading('WebGL-Firebase Bridge (Technical Achievement)');
addParagraph('Since Unity does not provide a native Firebase plugin for WebGL, we have implemented a custom JavaScript Bridge. This architecture is critical for WebGL compatibility:');
addListItem('Unity to Browser: When an action (like Login) is triggered in C#, the bridge passes the request to the browser\'s native JavaScript environment.');
addListItem('Browser to Firebase: JavaScript handles the asynchronous communication with Firebase servers directly.');
addListItem('Browser back to C#: Once Firebase responds, the bridge passes the results back into Unity’s C# environment.');
doc.moveDown(0.5);
doc.fontSize(11).font('Helvetica-Oblique').text('Without this bridge, Firebase authentication and database features would be inaccessible in WebGL builds.');
doc.font('Helvetica');

// Section 5
addHeading('5. Next Week’s Roadmap');
addListItem('Finalize Connection: Complete the establishment and testing of the live connection with Firebase.');
addListItem('Data Synchronization: Once the connection is verified, we will proceed with saving and retrieving data, including User Credentials, Brick Ownership, Avatar Profiles, and Onboarding Quest Data.');

doc.moveDown(2);
doc.fontSize(10).fillColor('#7f8c8d').text('End of Report', { align: 'center' });

doc.end();
console.log('PDF generated at: ' + outputPath);
