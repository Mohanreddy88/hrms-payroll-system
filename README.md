# HRMS Automated Testing

## 🚀 Quick Start

### Prerequisites
1. **Node.js installed** (already done)
2. **Dependencies installed** (already done - node_modules folder)
3. **Angular app running:**
   ```bash
   cd hrms-ui
   ng serve
   ```
   Wait for: `✔ Compiled successfully`
   Keep this running in a separate terminal!

### Run Tests

**Employee Workflow:**
```
run-employee-test-now.bat
```

**Admin Workflow:**
```
run-admin-test-now.bat
```

**Both Tests:**
```
run-all-tests.bat
```

## 🔑 Login Credentials

**Employee:** mohanreddys77@gmail.com / admin123  
**Admin:** mohan.net88@gmail.com / admin123

## 📊 View Results

After tests complete:
```bash
npx playwright show-report test-reports/html
```

## 📁 Project Structure

```
walnut/
├── hrms-ui/                    # Angular frontend
├── HrmsApi/                    # .NET backend
├── tests/                      # Playwright tests
│   ├── employee-workflow.spec.ts
│   └── admin-workflow.spec.ts
├── node_modules/               # Dependencies
├── run-employee-test-now.bat   # Run employee test
├── run-admin-test-now.bat      # Run admin test
├── run-all-tests.bat           # Run all tests
├── package.json                # Dependencies config
└── playwright.config.ts        # Test config
```

## 🎬 What Tests Do

**Employee Test (16 steps):**
Login → Dashboard → Leave Management → Attendance → Payroll → Reports → Master Data → Logout

**Admin Test (21 steps):**
Login → Dashboard → Employees → Attendance → Payroll → Timesheets → Leave → Reports → Master Data → Logout

## 💡 Important Notes

1. **Keep Angular running** - Don't close the `ng serve` terminal
2. **Use separate terminals** - One for `ng serve`, one for tests
3. **Tests run with visible browser** - You can watch the automation
4. **Takes 5-10 minutes** - Tests include waits for smooth viewing

---

**Ready? Start `ng serve` first, then run a test!** 🚀
