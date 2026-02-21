# Backend Rules

## 1. Service Layer (`EasyPark.Services`)

### Base Service Pattern

All CRUD services **MUST** inherit from `BaseCRUDService`:

```csharp
public class MyEntityService : BaseCRUDService<Model.MyEntity, MyEntitySearchObject, Database.MyEntity, MyEntityInsertRequest, MyEntityUpdateRequest>, IMyEntityService
{
    public MyEntityService(EasyParkDbContext context, IMapper mapper) : base(context, mapper)
    {
    }
}
```

### Dependency Injection

- Register all services in `Program.cs` as `Transient`.
- Inject `EasyParkDbContext` and `IMapper` into every service.

### Database Access

- **Read**: Use `Context.MyEntities.AsQueryable()`.
- **Write**: Use `Context.MyEntities.Add(entity)` / `Context.SaveChanges()`.
- **Include**: Always use `.Include()` for navigation properties when mapping to DTOs.

## 2. Controller Layer (`EasyPark.API`)

### Base Controller Pattern

All CRUD controllers **MUST** inherit from `BaseCRUDController`:

```csharp
public class MyEntityController : BaseCRUDController<Model.MyEntity, MyEntitySearchObject, MyEntityInsertRequest, MyEntityUpdateRequest>
{
    public MyEntityController(IMyEntityService service) : base(service)
    {
    }
}
```

### Attributes

- Use `[Authorize]` by default.
- Use `[AllowAnonymous]` only for public endpoints (e.g. Login, Registration).

## 3. Background Jobs

- Use **Hangfire** for scheduled tasks.
- Register jobs in `Program.cs` using `RecurringJob.AddOrUpdate`.
- Jobs must be idempotent.
