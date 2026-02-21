# Frontend Rules

## 1. Provider Layer (`lib/providers`)

### Base Provider Pattern

All data providers **MUST** inherit from `BaseProvider<T>`:

```dart
class MyEntityProvider extends BaseProvider<MyEntity> {
  MyEntityProvider() : super("MyEntity"); // Matches API Endpoint name

  @override
  MyEntity fromJson(data) {
    return MyEntity.fromJson(data);
  }
}
```

### responsibilities

- Handle all HTTP logical (Get, Insert, Update, Delete) via `BaseProvider` methods.
- Parse JSON into Models.
- Manage loading states if complex logic requires it (otherwise UI handles it).

## 2. Handling Errors

- `BaseProvider` automatically throws `UserFriendlyException` for API errors (400, 401, 404, 500).
- **UI Responsibility**: Wrap calls in `try/catch`:
  ```dart
  try {
    await provider.insert(request);
  } catch (e) {
    ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text(e.toString())));
  }
  ```

## 3. Screen Structure (`lib/screens`)

1.  **List Screen**: Uses `MasterScreenWidget` (if available) or standard `Scaffold`. Fetches data via `Provider.get()`.
2.  **Detail Screen**: Uses `FormBuilder` (if using `flutter_form_builder`) or `TextFormField`. Calls `Provider.insert()` or `Provider.update()`.

## 4. Utilities

- Use `utils.dart` for validation: `Authorization.createHeaders()`, `formatDate()`, `getStatusColor()`.
