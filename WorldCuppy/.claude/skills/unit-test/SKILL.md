---
name: unit-test
description: Scaffolds xUnit unit tests for WorldCuppy validators and pure calculation logic using Bogus for fake data. Use this skill whenever the user asks to add unit tests, write tests for a validator, test pure logic, or says things like "unit test X", "test the validator", "add tests for the scoring logic". For integration tests that need a real database, use the integration-test skill instead.
---

# Create Unit Tests

You are writing unit tests for WorldCuppy using xUnit and Bogus. Unit tests cover **pure logic only** — no database, no HTTP, no MediatR pipeline. Everything that needs a real database belongs in an integration test (`integration-test` skill).

## What belongs in unit tests vs integration tests

| Target | Test type |
|---|---|
| FluentValidation validators | **Unit test** — call `validator.TestValidate(command)` directly |
| Pure calculation classes (`LeaderboardCalculator`, `AwardPointsHandler.CalculatePoints`) | **Unit test** — call the static method with crafted inputs |
| MediatR handlers that query EF Core | **Integration test** — needs a real PostgreSQL container |
| API endpoints | **Integration test** — needs `WebApplicationFactory` |

## Step 1: Check prerequisites

The test project must exist. Run:
```powershell
Test-Path WorldCuppy.Tests/WorldCuppy.Tests.csproj
```

If it does not exist, follow the `integration-test` skill's setup steps — the two test types share one project.

Note: `FluentValidation.TestHelper` (`TestValidate`, `ShouldHaveValidationErrorFor`) is part of the main `FluentValidation` package since v11 — no separate package needed. The project reference to the main project brings it in transitively.

## Step 2: Determine what to test

### For a new command with a validator

Always write tests for every validation rule in the `Abstract Validator`:
- **Happy path** — valid Bogus-generated input, no errors
- **One test per rule violation** — one invalid property at a time, assert the right property fails

### For a new pure calculation method

If a handler contains pure in-memory logic (no DB calls), extract it into an `internal static` class and write:
- **Correctness** — known inputs produce known outputs
- **Edge cases** — empty inputs, boundary values, ties
- **Negative** — inputs that should produce 0/null/empty

### Making internal code accessible to tests

If the logic is `internal`, add this to `WorldCuppy/Properties/AssemblyInfo.cs` (already present in the codebase):
```csharp
[assembly: InternalsVisibleTo("WorldCuppy.Tests")]
```
This is already done for the project. New `internal` helpers are automatically accessible.

## Step 3: Create the test file

Place test files mirroring the production folder structure:
- `WorldCuppy.Tests/Unit/Predictions/` for `Features/Predictions/`
- `WorldCuppy.Tests/Unit/Sync/` for `Features/Sync/`
- `WorldCuppy.Tests/Unit/Leaderboard/` for `Features/Leaderboard/`

### Validator test template

```csharp
using Bogus;
using FluentValidation.TestHelper;
using WorldCuppy.Features.<FeatureName>;

namespace WorldCuppy.Tests.Unit.<FeatureName>;

/// <summary>Unit tests for <see cref="<ValidatorName>" />.</summary>
public class <ValidatorName>Tests
{
    private readonly <ValidatorName> _validator = new();
    private readonly Faker _faker = new();

    /// <summary>A valid command built with Bogus — all rule variations start from this baseline.</summary>
    private <CommandName> ValidCommand() => new(
        Property1: _faker.Random.Guid(),
        Property2: _faker.Random.Int(0, 100),
        Property3: _faker.Lorem.Word());

    [Fact]
    public void Validate_ValidCommand_PassesAllRules()
    {
        var result = _validator.TestValidate(ValidCommand());
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_EmptyProperty1_FailsProperty1()
    {
        var cmd = ValidCommand() with { Property1 = Guid.Empty };
        var result = _validator.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor(x => x.Property1);
    }

    // One test per rule violation...

    [Theory]
    [InlineData(-1)]
    [InlineData(-99)]
    public void Validate_NegativeProperty2_FailsProperty2(int value)
    {
        var cmd = ValidCommand() with { Property2 = value };
        var result = _validator.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor(x => x.Property2);
    }
}
```

### Pure calculation test template

```csharp
using Bogus;
using WorldCuppy.Features.<FeatureName>;

namespace WorldCuppy.Tests.Unit.<FeatureName>;

/// <summary>Unit tests for <see cref="<CalculatorClass>" />.</summary>
public class <CalculatorClass>Tests
{
    private readonly Faker _faker = new();

    [Fact]
    public void <MethodName>_<Condition>_<ExpectedOutcome>()
    {
        // Arrange — craft precise inputs using Bogus where values don't matter,
        // and literal values where the exact number is the point of the test
        var input = new <InputType>(/* specific values */);

        // Act
        var result = <CalculatorClass>.<MethodName>(input);

        // Assert
        Assert.Equal(expectedValue, result);
    }

    [Theory]
    [InlineData("INPUT_A", ExpectedValueA)]
    [InlineData("INPUT_B", ExpectedValueB)]
    public void <MethodName>_KnownInputs_ReturnCorrectMapping(string input, <ReturnType> expected)
    {
        Assert.Equal(expected, <CalculatorClass>.<MethodName>(input));
    }
}
```

## Step 4: Run the tests

```powershell
dotnet test WorldCuppy.Tests/WorldCuppy.Tests.csproj --verbosity minimal
```

For unit tests only (filter out integration tests):
```powershell
dotnet test WorldCuppy.Tests/WorldCuppy.Tests.csproj --filter "FullyQualifiedName~Unit" --verbosity minimal
```

## Style rules — apply to every test file, no exceptions

- **Bogus for all generated data** — never use `new Guid()`, `"test"`, or magic literals unless the literal IS the thing being tested (e.g. `InlineData("IN_PLAY", MatchStatus.Live)`)
- **`ValidCommand() with { Prop = bad }`** — start from a valid command, mutate one property per test so failures are isolated
- **`ShouldHaveValidationErrorFor(x => x.Prop)`** — always use the strongly-typed lambda, not the string overload
- **One assertion concern per test** — assert exactly one thing; a test named `Validate_EmptyUserId_FailsUserId` checks only `UserId`
- **`[Theory]` + `[InlineData]` for exhaustive mapping tests** — avoids duplicating switch-arm tests
- **Method naming convention** — `UnitOfWork_StateUnderTest_ExpectedBehavior` (What_When_Should):
  - `UnitOfWork` = the class or method being tested (e.g. `CreatePredictionValidator`, `MapStatus`, `Calculate`)
  - `StateUnderTest` = the input condition, always starts with `When` (e.g. `WhenUserIdIsEmpty`, `WhenTeamWinsAsHome`)
  - `ExpectedBehavior` = what should happen, always starts with `Should` (e.g. `ShouldFailValidation`, `ShouldAward3Points`)
  - Example: `CreatePredictionValidator_WhenUserIdIsEmpty_ShouldFailValidation`
- Every test class and method gets an XML `<summary>` doc comment
- File-scoped namespace

## Bogus cheat sheet for WorldCuppy types

```csharp
var faker = new Faker();

faker.Random.Guid()           // Guid — for UserId, MatchId, etc.
faker.Random.Int(0, 10)       // int — for scores (0+ for valid)
faker.Random.Int(-10, -1)     // int — for invalid negative scores
faker.Lorem.Word()            // string — for names, codes
faker.Random.String2(3).ToUpper()  // string — for 3-letter team codes
faker.Date.RecentOffset()     // DateTimeOffset — for timestamps
faker.PickRandom<TEnum>()     // enum value — for status/round fields
```
