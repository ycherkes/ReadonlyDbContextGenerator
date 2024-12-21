[![Stand With Ukraine](https://raw.githubusercontent.com/vshymanskyy/StandWithUkraine/main/banner2-direct.svg)](https://stand-with-ukraine.pp.ua)

# ReadOnly DbContext Source Generator

## Overview

The `ReadOnlyDbContextGenerator` is a C# source generator that creates read-only versions of EF Core DbContext and entities. It ensures that the generated DbContext and entities prevent modifications, making them suitable for read-only operations in applications.

## How It Works

1. **DbContext Analysis**
   - Identifies classes inheriting from `Microsoft.EntityFrameworkCore.DbContext`.
   - Extracts the DbSet properties and their entity types.

2. **Entity Processing**
   - Analyzes the entity types referenced in the DbSet properties.
   - Converts their properties to `init`-only.
   - Modifies navigation properties to reference read-only entities or collections.

3. **Entity Configuration**
   - Identifies and processes `IEntityTypeConfiguration` implementations.
   - Generates read-only configuration classes for the entities.

4. **Code Generation**
   - Produces source files for:
     - Read-only DbContext.
     - Read-only entity classes.
     - Entity configuration classes.
     - `IReadOnlyDbContext` interface.

## Usage

1. Add the source generator to your project:

   ```bash
   dotnet add package ReadOnlyEfCoreGenerator
   ```

2. Build your project. The generator will create source files for the read-only components.

3. Access the generated read-only DbContext and entities:

   ```csharp
   var readOnlyDbContext = new ReadOnlyMyDbContext();
   ```

## Example

### Input

#### DbContext
```csharp
public class MyDbContext : DbContext
{
    public DbSet<User> Users { get; set; }
    public DbSet<Order> Orders { get; set; }
}
```

#### Entity
```csharp
public class User
{
    public int Id { get; set; }
    public string Name { get; set; }
    public ICollection<Order> Orders { get; set; }
}

public class Order
{
    public int Id { get; set; }
    public string Product { get; set; }
    public User User { get; set; }
}
```

### Generated Output

#### ReadOnlyMyDbContext
```csharp
public class ReadOnlyMyDbContext : IReadOnlyMyDbContext
{
    public IReadOnlyCollection<User> Users { get; }
    public IReadOnlyCollection<Order> Orders { get; }

    public int SaveChanges()
    {
        throw new NotImplementedException("Read-only context");
    }
}
```

#### ReadOnlyUser
```csharp
public class ReadOnlyUser
{
    public int Id { get; init; }
    public string Name { get; init; }
    public IReadOnlyCollection<ReadOnlyOrder> Orders { get; init; }
}
```

#### ReadOnlyOrder
```csharp
public class ReadOnlyOrder
{
    public int Id { get; init; }
    public string Product { get; init; }
    public ReadOnlyUser User { get; init; }
}
```

#### IReadOnlyMyDbContext
```csharp
public interface IReadOnlyMyDbContext : IDisposable, IAsyncDisposable
{
    IReadOnlyCollection<ReadOnlyUser> Users { get; }
    IReadOnlyCollection<ReadOnlyOrder> Orders { get; }
}
```
