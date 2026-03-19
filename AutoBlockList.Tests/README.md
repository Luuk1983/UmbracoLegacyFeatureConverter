# Umbraco.Community.LegacyFeatureConverter Tests

This test project uses MSTest for testing the LegacyFeatureConverter package.

## Test Organization

- **Models/**: Tests for data models and DTOs
- **Services/**: Tests for service layer logic
- **Converters/**: Tests for property converter implementations
- **Integration/**: Integration tests (future)

## Running Tests

```bash
dotnet test
```

## Test Coverage Goals

The goal is to test the important business logic in the code. We focus on:
- Conversion logic correctness
- Data transformation accuracy
- Error handling behavior
- Edge cases and boundary conditions

We do NOT aim for 100% code coverage as a goal by itself. The focus is on meaningful tests that catch real bugs and ensure correct behavior.

## Integration Tests

Integration tests that require a real Umbraco database will be added in future phases. These would test:
- Full conversion workflows
- Database migrations
- Actual content type and property updates

For now, we use mocked dependencies for unit tests to verify individual components work correctly.
