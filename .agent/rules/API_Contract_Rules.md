# API Contract Rules

## 1. Request DTOs (`Model/Requests`)

### Naming

- **Insert**: `[Entity]InsertRequest`
- **Update**: `[Entity]UpdateRequest`
- **Search**: `[Entity]SearchObject`

### Validation

- Use `System.ComponentModel.DataAnnotations` attributes (`[Required]`, `[Range]`, `[EmailAddress]`).
- Validation is automatically enforced by `BaseController`.

## 2. Response DTOs (`Model/Models`)

### Structure

- Must be clean C# classes.
- Do **NOT** expose EF Core specific properties (like `virtual ICollection`).
- Flatten complex relationships where possible (e.g., `CityId` -> `CityName`).

## 3. Search Objects (`Model/SearchObjects`)

- Inherit from `BaseSearchObject`.
- Properties must be nullable (e.g., `int? Year`).
- Includes pagination fields (`Page`, `PageSize`) automatically via inheritance.
