#!/usr/bin/env python3
# Insert the two new methods into SelfServiceController.cs

# Read the new methods
with open('Controllers/SelfService_NewMethods.txt', 'r', encoding='utf-8') as f:
    new_methods = f.read()

# Read the controller file
with open('Controllers/SelfServiceController.cs', 'r', encoding='utf-8') as f:
    content = f.read()

# Find the insertion point: after "return Ok(payslips);" and before "GET /api/selfservice/my-attendance"
import re
pattern = r'(        return Ok\(payslips\);\r?\n    }\r?\n\r?\n)(    /// <summary>\r?\n    /// GET /api/selfservice/my-attendance)'
replacement = r'\1' + new_methods + '\n\n\\2'

new_content = re.sub(pattern, replacement, content)

# Write back
with open('Controllers/SelfServiceController.cs', 'w', encoding='utf-8', newline='\r\n') as f:
    f.write(new_content)

print("✅ Methods inserted successfully!")
