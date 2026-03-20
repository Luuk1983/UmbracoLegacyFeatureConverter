# Migration Guide: Upgrading to Umbraco.Community.LegacyFeatureConverter

This guide explains how to use the new converter-based architecture to migrate your legacy property editors to modern Umbraco 13 equivalents.

## Before You Begin

### ⚠️ Critical Prerequisites

1. **Backup your database** - Always create a full database backup before running conversions
2. **Test on a copy first** - Never run conversions on production without testing on a copy
3. **Use test runs** - Always run a test conversion first to validate it will succeed
4. **Check Umbraco version** - This package is for Umbraco 13 only

### ✅ Recommended Workflow

```
1. Create database backup
2. Clone production database to test environment
3. Install package on test environment
4. Run TEST conversion
5. Review test results and logs
6. If successful, run REAL conversion on test
7. Verify content displays correctly
8. Only then run on production (with fresh backup!)
```

---

## Converting Nested Content to Block List

### What Gets Converted

- **Document Types**: All document types with Nested Content properties
- **Compositions**: Properties defined in composition document types
- **Properties**: Updated in-place (no 'BL' suffix)
- **Content**: All content nodes using Nested Content
- **Nested NC**: Nested Content within Nested Content (recursive)

### Step-by-Step Process

#### 1. Access the Converter
- Navigate to **Settings** → **Legacy Feature Converter**

#### 2. Select Nested Content Converter
- Click on the **"Nested Content to Block List"** card
- Review the count of affected document types

#### 3. Select Document Types (Optional)
- By default, ALL document types with Nested Content will be converted
- Optionally, select specific document types using the checkbox list
- Click "Select All" / "Deselect All" as needed

#### 4. Run Test Conversion
- ✅ **Check "Test Run (Dry Run)"**
- Click **"Run Test Conversion"**
- Wait for completion
- Review results

#### 5. Review Test Logs
- Click **"View Detailed Logs"**
- Check for errors or warnings
- Verify element types exist
- Look for configuration issues

#### 6. Run Real Conversion
- Uncheck "Test Run"
- Click "Start Conversion"
- Wait for completion

#### 7. Verify Results
- Check content in backoffice
- Test editing and saving
- Verify front-end rendering

---

## Converting Legacy Media Picker to MediaPicker3

### What Gets Converted

- MediaPicker2 → MediaPicker3
- Multiple Media Picker → MediaPicker3

### Process

Follow the same steps as Nested Content, selecting the **"Legacy Media Picker to MediaPicker3"** converter instead.

---

## Best Practices

### ✅ DO
- Always backup database first
- Always run test conversion
- Review detailed logs
- Test on copy before production

### ❌ DON'T
- Don't skip test runs
- Don't ignore warnings
- Don't run on production without testing

---

## Troubleshooting

See detailed troubleshooting section in main README.md

---

## Post-Conversion

- Verify content works correctly
- Clear Umbraco caches
- Test front-end rendering
- Review conversion history
- Keep old data types for a while before deleting
