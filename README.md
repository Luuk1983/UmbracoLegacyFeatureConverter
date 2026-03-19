# Umbraco Community Legacy Feature Converter

![Version](https://img.shields.io/nuget/v/AutoBlockList?label=version)
[![Nuget](https://img.shields.io/nuget/dt/AutoBlockList?color=2346c018&logo=Nuget)](https://www.nuget.org/packages/AutoBlockList/)
[![Umbraco](https://img.shields.io/badge/marketplace-umbraco-283a97)](https://marketplace.umbraco.com/package/autoblocklist)

> [!NOTE]  
> Version 3.0.0 introduces a completely redesigned converter architecture with support for media picker conversion and comprehensive conversion history!

**Umbraco Community Legacy Feature Converter** is a package for Umbraco 13+ designed to help automate the migration of legacy property editors to their modern equivalents. With the removal of Nested Content in Umbraco 13, this tool makes upgrading safer and easier.

## Features

This package provides **three independent converter types**, each handling a specific migration scenario:

### 🔄 Nested Content → Block List Converter
Converts Nested Content properties to Block List properties with a **document-type-first approach**:

1. **Scans all document types** for Nested Content properties (not just content with data)
2. **Creates Block List data types** with matching configuration (min/max items, element types)
3. **Updates properties in-place** - replaces the data type without creating duplicate properties
4. **Converts all content** node data to Block List format
5. **Handles compositions** - updates properties in composition document types
6. **Supports nested NC** - recursively converts Nested Content within Nested Content

**Benefits over previous approach:**
- ✅ No 'BL' suffix clutter - properties updated in-place
- ✅ Complete coverage - converts all document types, even those without content
- ✅ Composition support - handles inherited properties
- ✅ Clean result - ready to delete old properties after conversion

### 📷 Legacy Media Picker → MediaPicker3 Converter
Converts legacy media picker properties to the modern MediaPicker3:

**Supported source property editors:**
- MediaPicker2 (`Umbraco.MediaPicker2`)
- Multiple Media Picker (`Umbraco.MultipleMediaPicker`)

**Conversion process:**
1. Scans all document types for legacy media picker properties
2. Creates MediaPicker3 data types with sensible defaults
3. Updates properties in-place to use MediaPicker3
4. Converts UDI string data to MediaPicker3 JSON format
5. Handles both single and multiple media selections
6. Preserves media references while adding support for crops and focal points

**Data format conversion:**
```
From: "umb://media/abc-123-guid"
To:   [{"key": "...", "mediaKey": "abc-123-guid", "crops": [], "focalPoint": null}]
```

### 🎯 Macro Converter (Legacy)
Converts macros to Block List components (existing functionality):
1. Scans content for TinyMCE properties containing macros
2. Creates element types based on macro parameters
3. Converts macro instances to block list components
4. Migrates partial view macros to regular partial views

*Note: Macro converter uses the original logic and is not yet refactored to the new architecture.*

---

## Installation

```bash
dotnet add package Umbraco.Community.LegacyFeatureConverter
```

Or install via NuGet Package Manager in Visual Studio.

---

## Usage

### 1. Navigate to Settings
After installation, you'll find **"Legacy Feature Converter"** in the Settings section of the Umbraco backoffice.

### 2. Select Converter Type
Choose which type of legacy property editor you want to convert:
- Nested Content to Block List
- Legacy Media Picker to MediaPicker3
- Macro to Block (if applicable)

### 3. Select Document Types
The converter will show all document types with properties using the selected legacy property editor. You can:
- Select specific document types to convert
- Select All to convert everything
- Leave unselected to convert all affected document types

### 4. Configure Options
- **Test Run (Dry Run)**: Simulates the conversion without saving changes. Perfect for validating the conversion will succeed before committing.

### 5. Start Conversion
Click "Start Conversion" or "Run Test Conversion" to begin. The converter will:
- Create/update data types
- Update document type properties
- Convert content node data
- Log everything to the conversion history

### 6. Review Results
After conversion, you'll see:
- Success/failure/skipped counts
- Duration
- Link to detailed logs

### 7. View Conversion History
Access "View Conversion History" to see all past conversions, including:
- Conversion summary (success/failure counts)
- Detailed logs with timestamps
- Error messages and stack traces
- Test run indicators

---

## Configuration

Configure in `appsettings.json`:

```json
"AutoBlockList": {
    "BlockListEditorSize": "medium",
    "SaveAndPublish": true,
    "NameFormatting": "[Block List] - {0}",
    "AliasFormatting": "{0}BL",
    "FolderNameForContentTypes": "[Rich text editor] - Components"
}
```

**Settings:**
- `BlockListEditorSize`: Default block size (`small`, `medium`, `large`)
- `SaveAndPublish`: Whether to publish content after conversion (applies to old macro converter)
- `NameFormatting`: Format for new data type names (`{0}` = original name)
- `AliasFormatting`: Format for property aliases (only used by legacy macro converter)
- `FolderNameForContentTypes`: Folder name for macro-based document types

---

## How It Works

### Document-Type-First Approach

Unlike many migration tools that scan content first, this package uses a **document-type-first strategy**:

1. **Phase 1: Scan Document Types**
   - Finds all document types with properties using the source property editor
   - Includes composition properties
   - Works even if no content exists yet

2. **Phase 2: Create Data Types**
   - Creates target data types (Block List or MediaPicker3)
   - Maps configuration from source to target
   - Reuses existing data types if already created

3. **Phase 3: Update Properties**
   - Updates property `DataTypeId` in-place
   - No duplicate properties with 'BL' suffix
   - Handles both direct and composition properties

4. **Phase 4: Convert Content**
   - Processes all content nodes using the affected document types
   - Converts property values from source format to target format
   - Continues on error to maximize conversion success

### Test Run (Dry Run)

Test runs execute the **entire conversion workflow** including:
- ✅ Document type scanning
- ✅ Data type creation (in memory)
- ✅ Property updates (in memory)
- ✅ Content data conversion (in memory)
- ✅ Validation and error detection

**But without saving:**
- ❌ No database changes committed
- ✅ Transaction rolled back automatically
- ✅ Logged to history as "TEST RUN"

Perfect for verifying the conversion will succeed before running it for real!

### Error Handling

The converter uses a **continue-on-error** approach:
- ✅ Individual failures are logged but don't stop the process
- ✅ Each item is wrapped in try-catch
- ✅ Detailed error messages and stack traces saved to history
- ✅ Final status shows success/failure/skipped counts

**Only aborts for critical failures:**
- Service dependencies unavailable
- Database connection lost
- User cancels operation

---

## Conversion History & Logging

Every conversion run (including test runs) is logged to the database with:

**Summary Information:**
- Converter type used
- Start and completion times
- Selected document types
- Success/failure/skipped counts
- Overall status

**Detailed Logs:**
- Timestamp for each operation
- Item type (Document Type, Property, Content, DataType)
- Item name and key for reference
- Log message
- Additional details (JSON)
- Stack traces for errors

**Access via UI:**
- List view: All past conversions
- Detail view: Expandable log entries with filtering
- Filter by: Info, Warning, Error levels

---

## Requirements

- **Umbraco 13+** (This package is designed specifically for Umbraco 13)
- **.NET 8.0**

---

## Architecture

The package uses a modular converter architecture:

```
IPropertyConverter (Interface)
    ↓
BasePropertyConverter (Abstract)
    ↓
├── NestedContentConverter
├── MediaPickerConverter
└── (Future converters...)
```

Each converter implements:
- Source property editor aliases
- Target property editor alias
- Data type creation logic
- Property value conversion logic

The base class provides:
- Document type scanning
- Composition handling
- Dry-run support
- Logging integration
- Error resilience

---

## Advanced Usage

### Programmatic Conversion

You can execute conversions programmatically:

```csharp
public class MyService
{
    private readonly IPropertyConverter _nestedContentConverter;

    public MyService(IEnumerable<IPropertyConverter> converters)
    {
        _nestedContentConverter = converters
            .First(c => c.ConverterName == "Nested Content to Block List");
    }

    public async Task ConvertAsync()
    {
        var options = new ConversionOptions
        {
            ConverterType = "Nested Content to Block List",
            SelectedDocumentTypeKeys = null, // Convert all
            IsTestRun = false,
            PerformingUserKey = currentUserKey
        };

        var result = await _nestedContentConverter.ExecuteConversionAsync(options);

        // Check result.Status, result.SuccessCount, etc.
    }
}
```

### Custom Converters

You can create your own property converters:

```csharp
public class CustomConverter : BasePropertyConverter
{
    public override string ConverterName => "My Custom Converter";

    public override string[] SourcePropertyEditorAliases => 
        new[] { "My.Legacy.PropertyEditor" };

    public override string TargetPropertyEditorAlias => 
        "My.Modern.PropertyEditor";

    protected override async Task<IDataType?> CreateTargetDataTypeAsync(IDataType sourceDataType)
    {
        // Create and configure target data type
    }

    protected override async Task<object?> ConvertPropertyValueAsync(object sourceValue, IProperty property)
    {
        // Convert property value format
    }
}

// Register in composer
builder.Services.AddScoped<IPropertyConverter, CustomConverter>();
```

---

## Contributing

Contributions are welcome! Please:
1. Fork the repository
2. Create a feature branch
3. Add tests for new functionality
4. Submit a pull request

---

## Issues

Found a bug or have a feature request? [Create an issue](https://github.com/Luuk1983/UmbracoLegacyFeatureConverter/issues) on GitHub.

---

## License

This project is licensed under the MIT License.