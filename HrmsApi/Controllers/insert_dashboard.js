const fs = require('fs');

// Read files
const newEndpoint = fs.readFileSync('dashboard_endpoint.txt', 'utf8');
const content = fs.readFileSync('SelfServiceController.cs', 'utf8');

// Find insertion point - after "return Ok(new { message = "Profile updated successfully", employee });" and its closing brace
const searchPattern = /return Ok\(new \{ message = "Profile updated successfully", employee \}\);[\r\n]+    \}/;

// Check if pattern exists
if (searchPattern.test(content)) {
    // Insert the new endpoint after the closing brace
    const newContent = content.replace(searchPattern, (match) => {
        return match + newEndpoint;
    });
    
    // Write back
    fs.writeFileSync('SelfServiceController.cs', newContent, 'utf8');
    console.log('✅ Dashboard endpoint inserted successfully!');
} else {
    console.log('❌ Insertion point not found!');
    console.log('Searching for: return Ok(new { message = "Profile updated successfully"');
}
