#!/usr/bin/env python3
# Insert dashboard endpoint into SelfServiceController.cs

# Read the new endpoint
with open('dashboard_endpoint.txt', 'r', encoding='utf-8') as f:
    new_endpoint = f.read()

# Read the controller
with open('SelfServiceController.cs', 'r', encoding='utf-8') as f:
    lines = f.readlines()

# Find insertion point and insert
result = []
inserted = False
for i, line in enumerate(lines):
    result.append(line)
    # Look for the pattern: return Ok after UpdateMyProfile
    if (not inserted and 
        'return Ok(new { message = "Profile updated successfully"' in line and 
        i + 1 < len(lines) and 
        lines[i + 1].strip() == '}'):
        # Add the closing brace
        result.append(lines[i + 1])
        # Add the new endpoint
        result.append(new_endpoint)
        # Skip the next line since we already added it
        lines[i + 1] = ''
        inserted = True

# Write back
with open('SelfServiceController.cs', 'w', encoding='utf-8') as f:
    f.writelines(result)

print("✅ Dashboard endpoint inserted successfully!" if inserted else "❌ Insertion point not found!")
