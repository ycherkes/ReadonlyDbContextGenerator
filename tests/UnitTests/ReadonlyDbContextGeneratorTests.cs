using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.EntityFrameworkCore;
using VerifyCS = UnitTests.CSharpSourceGeneratorVerifier<ReadonlyDbContextGenerator.ReadOnlyDbContextGenerator>;


namespace UnitTests;

public class ReadonlyDbContextGeneratorTests
{
    [Fact]
    public async Task GeneratesReadOnlyEntitiesAndDbContext()
    {
        // Input source code
        var inputSource = """
                          #nullable enable
                          using System.Collections.Generic;
                          using Microsoft.EntityFrameworkCore;

                          namespace MyApp.Entities
                          {
                              public class User
                              {
                                  public int Id { get; set; }
                                  public string Name { get; set; }
                                  public ICollection<Order> Orders { get; set; }
                                  
                                  public static User Create(int id, string name, ICollection<Order> orders)
                                  {
                                      return new User
                                      {
                                          Id = id,
                                          Name = name,
                                          Orders = orders
                                      };
                                  }
                              }
                          
                              public class Order
                              {
                                  public int Id { get; set; }
                                  public string Description { get; set; }
                              }
                          
                              public class MyDbContext : DbContext
                              {
                                  public DbSet<User> Users { get; set; }
                              }
                          }

                          """;

        // Expected generated output for the ReadOnly entity
        var expectedUserReadOnlySource = """
using Microsoft.EntityFrameworkCore;
using MyApp.Entities;
using System;
#nullable enable
using System.Collections.Generic;

namespace MyApp.Entities.Generated
{
    public class ReadOnlyUser
    {
        public int Id { get; init; }

        public string Name { get; init; }

        public IReadOnlyCollection<ReadOnlyOrder> Orders { get; init; }
    }
}
""";

        var expectedOrderReadOnlySource = """
using Microsoft.EntityFrameworkCore;
using MyApp.Entities;
using System;
#nullable enable
using System.Collections.Generic;

namespace MyApp.Entities.Generated
{
    public class ReadOnlyOrder
    {
        public int Id { get; init; }

        public string Description { get; init; }
    }
}
""";

        // Expected generated output for the ReadonlyDbContext
        var expectedReadonlyDbContextSource = """
using Microsoft.EntityFrameworkCore;
using MyApp.Entities;
using System;
#nullable enable
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MyApp.Entities.Generated
{
    public partial class ReadOnlyMyDbContext : DbContext, IReadOnlyMyDbContext
    {
        public DbSet<ReadOnlyUser> Users { get; set; }

        public sealed override int SaveChanges()
        {
            throw new NotImplementedException("Do not call SaveChanges on a readonly db context.");
        }

        public sealed override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException("Do not call SaveChangesAsync on a readonly db context.");
        }

        public sealed override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException("Do not call SaveChangesAsync on a readonly db context.");
        }

        IQueryable<ReadOnlyUser> IReadOnlyMyDbContext.Users => Users;
        IQueryable<TEntity> IReadOnlyMyDbContext.Set<TEntity>()
            where TEntity : class => Set<TEntity>();
    }
}
""";

        // Expected generated output for the IReadOnlyDbContext interface
        var expectedIReadOnlyDbContextSource = """
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using MyApp.Entities;
using System;
#nullable enable
using System.Collections.Generic;
using System.Linq;

namespace MyApp.Entities.Generated
{
    public partial interface IReadOnlyMyDbContext : IDisposable, IAsyncDisposable
    {
        IQueryable<ReadOnlyUser> Users { get; }

        IQueryable<TEntity> Set<TEntity>()
            where TEntity : class;
        DatabaseFacade Database { get; }
    }
}
""";

        // Configure the test
        var test = new VerifyCS.Test
        {
            TestState =
            {
                Sources = { inputSource },
                AdditionalReferences =
                {
                    MetadataReference.CreateFromFile(typeof(DbContext).Assembly.Location)
                },
                GeneratedSources =
                {
                    // Verify the generated sources
                    (typeof(ReadonlyDbContextGenerator.ReadOnlyDbContextGenerator), "ReadOnlyUser.g.cs", expectedUserReadOnlySource),
                    (typeof(ReadonlyDbContextGenerator.ReadOnlyDbContextGenerator), "ReadOnlyOrder.g.cs", expectedOrderReadOnlySource),
                    (typeof(ReadonlyDbContextGenerator.ReadOnlyDbContextGenerator), "ReadOnlyMyDbContext.g.cs", expectedReadonlyDbContextSource),
                    (typeof(ReadonlyDbContextGenerator.ReadOnlyDbContextGenerator), "IReadOnlyMyDbContext.g.cs", expectedIReadOnlyDbContextSource)
                }
            },
        };

        test.SolutionTransforms.Add((solution, projectId) =>
        {
            var project = solution.GetProject(projectId);
            var options = (CSharpCompilationOptions)project.CompilationOptions;
            options = options.WithSpecificDiagnosticOptions(options.SpecificDiagnosticOptions.SetItems(new Dictionary<string, ReportDiagnostic>
            {
                ["CS8618"] = ReportDiagnostic.Suppress
            }));
            return project.WithCompilationOptions(options).Solution;
        });

        // Run the test
        await test.RunAsync();
    }

    [Fact]
    public async Task RewritesValueComparerToReadonlyCollectionAndReadonlyElement()
    {
        var inputSource = """
                          using System.Collections.Generic;
                          using System.Text.Json;
                          using Microsoft.EntityFrameworkCore;
                          using Microsoft.EntityFrameworkCore.ChangeTracking;
                          using Microsoft.EntityFrameworkCore.Metadata.Builders;
                          
                          namespace MyApp.Entities
                          {
                              public class Translation
                              {
                                  public string LanguageCode { get; set; }
                                  public string Value { get; set; }
                              }
                          
                              public class ParameterSetting
                              {
                                  public List<Translation> NameTranslations { get; set; }
                              }
                          
                              public class MyDbContext : DbContext
                              {
                                  public DbSet<ParameterSetting> ParameterSettings { get; set; }
                              }
                          
                              public static class JsonOptions
                              {
                                  public static JsonSerializerOptions IgnoreNulls { get; } = new JsonSerializerOptions();
                              }
                          
                              public class ParameterSettingConfiguration : IEntityTypeConfiguration<ParameterSetting>
                              {
                                  public void Configure(EntityTypeBuilder<ParameterSetting> builder)
                                  {
                                      var valueComparer = new ValueComparer<List<Translation>>(true);
                                      builder.Property(e => e.NameTranslations).HasConversion(v => JsonSerializer.Serialize(v, JsonOptions.IgnoreNulls), v => JsonSerializer.Deserialize<List<Translation>>(v, JsonOptions.IgnoreNulls) ?? new()).Metadata.SetValueComparer(valueComparer);
                                  }
                              }
                          }
                          """;

        var expectedTranslation = """
                                  using MyApp.Entities;
                                  using System;
                                  using System.Collections.Generic;
                                  
                                  namespace MyApp.Entities.Generated
                                  {
                                      public class ReadOnlyTranslation
                                      {
                                          public string LanguageCode { get; init; }
                                  
                                          public string Value { get; init; }
                                      }
                                  }
                                  """;

        var expectedParameterSetting = """
                                       using MyApp.Entities;
                                       using System;
                                       using System.Collections.Generic;
                                       
                                       namespace MyApp.Entities.Generated
                                       {
                                           public class ReadOnlyParameterSetting
                                           {
                                               public IReadOnlyCollection<ReadOnlyTranslation> NameTranslations { get; init; }
                                           }
                                       }
                                       """;

        var expectedConfig = """
                             using Microsoft.EntityFrameworkCore;
                             using Microsoft.EntityFrameworkCore.ChangeTracking;
                             using Microsoft.EntityFrameworkCore.Metadata.Builders;
                             using MyApp.Entities;
                             using System;
                             using System.Collections.Generic;
                             using System.Text.Json;
                             
                             namespace MyApp.Entities.Generated
                             {
                                 public class ReadOnlyParameterSettingConfiguration : IEntityTypeConfiguration<ReadOnlyParameterSetting>
                                 {
                                     public void Configure(EntityTypeBuilder<ReadOnlyParameterSetting> builder)
                                     {
                                         var valueComparer = new ValueComparer<IReadOnlyCollection<ReadOnlyTranslation>>(true);
                                         builder.Property(e => e.NameTranslations).HasConversion(v => JsonSerializer.Serialize(v, JsonOptions.IgnoreNulls), v => JsonSerializer.Deserialize<List<ReadOnlyTranslation>>(v, JsonOptions.IgnoreNulls) ?? new()).Metadata.SetValueComparer(valueComparer);
                                     }
                                 }
                             }
                             """;

        var expectedDbContext = """
using Microsoft.EntityFrameworkCore;
using MyApp.Entities;
using System;
#nullable enable
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MyApp.Entities.Generated
{
    public partial class ReadOnlyMyDbContext : DbContext, IReadOnlyMyDbContext
    {
        public DbSet<ReadOnlyParameterSetting> ParameterSettings { get; set; }

        public sealed override int SaveChanges()
        {
            throw new NotImplementedException("Do not call SaveChanges on a readonly db context.");
        }

        public sealed override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException("Do not call SaveChangesAsync on a readonly db context.");
        }

        public sealed override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException("Do not call SaveChangesAsync on a readonly db context.");
        }

        IQueryable<ReadOnlyParameterSetting> IReadOnlyMyDbContext.ParameterSettings => ParameterSettings;
        IQueryable<TEntity> IReadOnlyMyDbContext.Set<TEntity>()
            where TEntity : class => Set<TEntity>();
    }
}
""";

        var expectedInterface = """
                                using Microsoft.EntityFrameworkCore;
                                using Microsoft.EntityFrameworkCore.Infrastructure;
                                using MyApp.Entities;
                                using System;
                                using System.Collections.Generic;
                                using System.Linq;
                                
                                namespace MyApp.Entities.Generated
                                {
                                    public partial interface IReadOnlyMyDbContext : IDisposable, IAsyncDisposable
                                    {
                                        IQueryable<ReadOnlyParameterSetting> ParameterSettings { get; }
                                
                                        IQueryable<TEntity> Set<TEntity>()
                                            where TEntity : class;
                                        DatabaseFacade Database { get; }
                                    }
                                }
                                """;

        var test = new VerifyCS.Test
        {
            TestState =
            {
                Sources = { inputSource },
                AdditionalReferences =
                {
                    MetadataReference.CreateFromFile(typeof(DbContext).Assembly.Location)
                },
                GeneratedSources =
                {
                    (typeof(ReadonlyDbContextGenerator.ReadOnlyDbContextGenerator), "ReadOnlyTranslation.g.cs", expectedTranslation),
                    (typeof(ReadonlyDbContextGenerator.ReadOnlyDbContextGenerator), "ReadOnlyParameterSetting.g.cs", expectedParameterSetting),
                    (typeof(ReadonlyDbContextGenerator.ReadOnlyDbContextGenerator), "ReadOnlyParameterSettingConfiguration.g.cs", expectedConfig),
                    (typeof(ReadonlyDbContextGenerator.ReadOnlyDbContextGenerator), "ReadOnlyMyDbContext.g.cs", expectedDbContext),
                    (typeof(ReadonlyDbContextGenerator.ReadOnlyDbContextGenerator), "IReadOnlyMyDbContext.g.cs", expectedInterface)
                }
            },
        };

        test.SolutionTransforms.Add((solution, projectId) =>
        {
            var project = solution.GetProject(projectId);
            var options = (CSharpCompilationOptions)project.CompilationOptions;
            options = options.WithSpecificDiagnosticOptions(options.SpecificDiagnosticOptions.SetItems(new Dictionary<string, ReportDiagnostic>
            {
                ["CS8618"] = ReportDiagnostic.Suppress
            }));
            return project.WithCompilationOptions(options).Solution;
        });

        test.TestBehaviors |= TestBehaviors.SkipGeneratedSourcesCheck;

        await test.RunAsync();
    }

    [Fact]
    public async Task SelfReferencingCollectionsBecomeReadOnlyCollectionsWithReadonlyElement()
    {
        var inputSource = """
                          #nullable enable
                          using System.Collections.Generic;
                          using Microsoft.EntityFrameworkCore;

                          namespace MyApp.Entities
                          {
                              public class Department
                              {
                                  public int Id { get; set; }
                                  public string Name { get; set; }
                                  public Department? Parent { get; set; }
                                  public List<Department>? Children { get; set; }
                              }

                              public class MyDbContext : DbContext
                              {
                                  public DbSet<Department> Departments { get; set; }
                              }
                          }
                          """;

        var expectedDepartment = """
using Microsoft.EntityFrameworkCore;
using MyApp.Entities;
using System;
#nullable enable
using System.Collections.Generic;

namespace MyApp.Entities.Generated
{
    public class ReadOnlyDepartment
    {
        public int Id { get; init; }

        public string Name { get; init; }

        public ReadOnlyDepartment? Parent { get; init; }

        public IReadOnlyCollection<ReadOnlyDepartment>? Children { get; init; }
    }
}
""";

        var expectedDbContext = """
using Microsoft.EntityFrameworkCore;
using MyApp.Entities;
using System;
#nullable enable
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MyApp.Entities.Generated
{
    public partial class ReadOnlyMyDbContext : DbContext, IReadOnlyMyDbContext
    {
        public DbSet<ReadOnlyDepartment> Departments { get; set; }

        public sealed override int SaveChanges()
        {
            throw new NotImplementedException("Do not call SaveChanges on a readonly db context.");
        }

        public sealed override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException("Do not call SaveChangesAsync on a readonly db context.");
        }

        public sealed override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException("Do not call SaveChangesAsync on a readonly db context.");
        }

        IQueryable<ReadOnlyDepartment> IReadOnlyMyDbContext.Departments => Departments;
        IQueryable<TEntity> IReadOnlyMyDbContext.Set<TEntity>()
            where TEntity : class => Set<TEntity>();
    }
}
""";

        var expectedInterface = """
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using MyApp.Entities;
using System;
#nullable enable
using System.Collections.Generic;
using System.Linq;

namespace MyApp.Entities.Generated
{
    public partial interface IReadOnlyMyDbContext : IDisposable, IAsyncDisposable
    {
        IQueryable<ReadOnlyDepartment> Departments { get; }

        IQueryable<TEntity> Set<TEntity>()
            where TEntity : class;
        DatabaseFacade Database { get; }
    }
}
""";

        var test = new VerifyCS.Test
        {
            TestState =
            {
                Sources = { inputSource },
                AdditionalReferences =
                {
                    MetadataReference.CreateFromFile(typeof(DbContext).Assembly.Location)
                },
                GeneratedSources =
                {
                    (typeof(ReadonlyDbContextGenerator.ReadOnlyDbContextGenerator), "ReadOnlyDepartment.g.cs", expectedDepartment),
                    (typeof(ReadonlyDbContextGenerator.ReadOnlyDbContextGenerator), "ReadOnlyMyDbContext.g.cs", expectedDbContext),
                    (typeof(ReadonlyDbContextGenerator.ReadOnlyDbContextGenerator), "IReadOnlyMyDbContext.g.cs", expectedInterface)
                }
            },
        };

        test.SolutionTransforms.Add((solution, projectId) =>
        {
            var project = solution.GetProject(projectId);
            var options = (CSharpCompilationOptions)project.CompilationOptions;
            options = options.WithSpecificDiagnosticOptions(options.SpecificDiagnosticOptions.SetItems(new Dictionary<string, ReportDiagnostic>
            {
                ["CS8618"] = ReportDiagnostic.Suppress
            }));
            return project.WithCompilationOptions(options).Solution;
        });

        await test.RunAsync();
    }

    [Fact]
    public async Task NullableNavigationCollectionBecomesNullableReadOnlyCollectionWithReadonlyElement()
    {
        var inputSource = """
                          #nullable enable
                          using System.Collections.Generic;
                          using Microsoft.EntityFrameworkCore;

                          namespace MyApp.Entities
                          {
                              public class Translation
                              {
                                  public string LanguageCode { get; set; }
                                  public string Value { get; set; }
                              }

                              public class SettingKey
                              {
                                  public IList<Translation>? Link { get; set; }
                              }

                              public class MyDbContext : DbContext
                              {
                                  public DbSet<SettingKey> SettingKeys { get; set; }
                              }
                          }
                          """;

        var expectedTranslation = """
using Microsoft.EntityFrameworkCore;
using MyApp.Entities;
using System;
#nullable enable
using System.Collections.Generic;

namespace MyApp.Entities.Generated
{
    public class ReadOnlyTranslation
    {
        public string LanguageCode { get; init; }

        public string Value { get; init; }
    }
}
""";

        var expectedSettingKey = """
using Microsoft.EntityFrameworkCore;
using MyApp.Entities;
using System;
#nullable enable
using System.Collections.Generic;

namespace MyApp.Entities.Generated
{
    public class ReadOnlySettingKey
    {
        public IReadOnlyCollection<ReadOnlyTranslation>? Link { get; init; }
    }
}
""";

        var expectedDbContext = """
using Microsoft.EntityFrameworkCore;
using MyApp.Entities;
using System;
#nullable enable
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MyApp.Entities.Generated
{
    public partial class ReadOnlyMyDbContext : DbContext, IReadOnlyMyDbContext
    {
        public DbSet<ReadOnlySettingKey> SettingKeys { get; set; }

        public sealed override int SaveChanges()
        {
            throw new NotImplementedException("Do not call SaveChanges on a readonly db context.");
        }

        public sealed override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException("Do not call SaveChangesAsync on a readonly db context.");
        }

        public sealed override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException("Do not call SaveChangesAsync on a readonly db context.");
        }

        IQueryable<ReadOnlySettingKey> IReadOnlyMyDbContext.SettingKeys => SettingKeys;
        IQueryable<TEntity> IReadOnlyMyDbContext.Set<TEntity>()
            where TEntity : class => Set<TEntity>();
    }
}
""";

        var expectedInterface = """
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using MyApp.Entities;
using System;
#nullable enable
using System.Collections.Generic;
using System.Linq;

namespace MyApp.Entities.Generated
{
    public partial interface IReadOnlyMyDbContext : IDisposable, IAsyncDisposable
    {
        IQueryable<ReadOnlySettingKey> SettingKeys { get; }

        IQueryable<TEntity> Set<TEntity>()
            where TEntity : class;
        DatabaseFacade Database { get; }
    }
}
""";

        var test = new VerifyCS.Test
        {
            TestState =
            {
                Sources = { inputSource },
                AdditionalReferences =
                {
                    MetadataReference.CreateFromFile(typeof(DbContext).Assembly.Location)
                },
                GeneratedSources =
                {
                    (typeof(ReadonlyDbContextGenerator.ReadOnlyDbContextGenerator), "ReadOnlySettingKey.g.cs", expectedSettingKey),
                    (typeof(ReadonlyDbContextGenerator.ReadOnlyDbContextGenerator), "ReadOnlyTranslation.g.cs", expectedTranslation),
                    (typeof(ReadonlyDbContextGenerator.ReadOnlyDbContextGenerator), "ReadOnlyMyDbContext.g.cs", expectedDbContext),
                    (typeof(ReadonlyDbContextGenerator.ReadOnlyDbContextGenerator), "IReadOnlyMyDbContext.g.cs", expectedInterface)
                }
            },
        };

        test.SolutionTransforms.Add((solution, projectId) =>
        {
            var project = solution.GetProject(projectId);
            var options = (CSharpCompilationOptions)project.CompilationOptions;
            options = options.WithSpecificDiagnosticOptions(options.SpecificDiagnosticOptions.SetItems(new Dictionary<string, ReportDiagnostic>
            {
                ["CS8618"] = ReportDiagnostic.Suppress
            }));
            return project.WithCompilationOptions(options).Solution;
        });

        await test.RunAsync();
    }

    [Fact]
    public async Task GeneratesReadOnlyOwnedTypeConfiguredInOnModelCreating()
    {
        var inputSource = """
                          #nullable enable
                          using Microsoft.EntityFrameworkCore;

                          namespace MyApp.Entities
                          {
                              public class Order
                              {
                                  public int Id { get; set; }
                              }

                              public class ShippingAddress
                              {
                                  public string Street { get; set; }
                              }

                              public class MyDbContext : DbContext
                              {
                                  public DbSet<Order> Orders { get; set; }

                                  protected override void OnModelCreating(ModelBuilder modelBuilder)
                                  {
                                      modelBuilder.Entity<Order>().OwnsOne<ShippingAddress>("ShippingAddress");
                                  }
                              }
                          }
                          """;

        var expectedOrder = """
#nullable enable
using Microsoft.EntityFrameworkCore;
using MyApp.Entities;
using System;
using System.Collections.Generic;

namespace MyApp.Entities.Generated
{
    public class ReadOnlyOrder
    {
        public int Id { get; init; }
    }
}
""";

        var expectedShippingAddress = """
#nullable enable
using Microsoft.EntityFrameworkCore;
using MyApp.Entities;
using System;
using System.Collections.Generic;

namespace MyApp.Entities.Generated
{
    public class ReadOnlyShippingAddress
    {
        public string Street { get; init; }
    }
}
""";

        var expectedDbContext = """
#nullable enable
using Microsoft.EntityFrameworkCore;
using MyApp.Entities;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MyApp.Entities.Generated
{
    public partial class ReadOnlyMyDbContext : DbContext, IReadOnlyMyDbContext
    {
        public DbSet<ReadOnlyOrder> Orders { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ReadOnlyOrder>().OwnsOne<ReadOnlyShippingAddress>("ShippingAddress");
        }

        public sealed override int SaveChanges()
        {
            throw new NotImplementedException("Do not call SaveChanges on a readonly db context.");
        }

        public sealed override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException("Do not call SaveChangesAsync on a readonly db context.");
        }

        public sealed override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException("Do not call SaveChangesAsync on a readonly db context.");
        }

        IQueryable<ReadOnlyOrder> IReadOnlyMyDbContext.Orders => Orders;
        IQueryable<TEntity> IReadOnlyMyDbContext.Set<TEntity>()
            where TEntity : class => Set<TEntity>();
    }
}
""";

        var expectedInterface = """
#nullable enable
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using MyApp.Entities;
using System;
using System.Linq;

namespace MyApp.Entities.Generated
{
    public partial interface IReadOnlyMyDbContext : IDisposable, IAsyncDisposable
    {
        IQueryable<ReadOnlyOrder> Orders { get; }

        IQueryable<TEntity> Set<TEntity>()
            where TEntity : class;
        DatabaseFacade Database { get; }
    }
}
""";

        static string ToCrLf(string source) => source.Replace("\r\n", "\n").Replace("\n", "\r\n");

        var test = new VerifyCS.Test
        {
            TestState =
            {
                Sources = { inputSource },
                AdditionalReferences =
                {
                    MetadataReference.CreateFromFile(typeof(DbContext).Assembly.Location)
                },
                GeneratedSources =
                {
                    (typeof(ReadonlyDbContextGenerator.ReadOnlyDbContextGenerator), "ReadOnlyOrder.g.cs", ToCrLf(expectedOrder)),
                    (typeof(ReadonlyDbContextGenerator.ReadOnlyDbContextGenerator), "ReadOnlyShippingAddress.g.cs", ToCrLf(expectedShippingAddress)),
                    (typeof(ReadonlyDbContextGenerator.ReadOnlyDbContextGenerator), "ReadOnlyMyDbContext.g.cs", ToCrLf(expectedDbContext)),
                    (typeof(ReadonlyDbContextGenerator.ReadOnlyDbContextGenerator), "IReadOnlyMyDbContext.g.cs", ToCrLf(expectedInterface))
                }
            },
        };

        test.SolutionTransforms.Add((solution, projectId) =>
        {
            var project = solution.GetProject(projectId);
            var options = (CSharpCompilationOptions)project.CompilationOptions;
            options = options.WithSpecificDiagnosticOptions(options.SpecificDiagnosticOptions.SetItems(new Dictionary<string, ReportDiagnostic>
            {
                ["CS8618"] = ReportDiagnostic.Suppress
            }));
            return project.WithCompilationOptions(options).Solution;
        });

        await test.RunAsync();
    }

    [Fact]
    public async Task GeneratesReadOnlyEntityForRecordDbSetEntity()
    {
        var inputSource = """
                          using Microsoft.EntityFrameworkCore;

                          namespace MyApp.Entities
                          {
                              public record Reservation
                              {
                                  public int Id { get; set; }

                                  public Reservation(int id)
                                  {
                                      Id = id;
                                  }
                              }

                              public class MyDbContext : DbContext
                              {
                                  public DbSet<Reservation> Reservations { get; set; }

                                  protected override void OnModelCreating(ModelBuilder modelBuilder)
                                  {
                                      modelBuilder.Entity<Reservation>().HasData(new Reservation(7) { Id = 1 });
                                  }
                              }
                          }
                          """;

        var expectedReservation = """
using Microsoft.EntityFrameworkCore;
using MyApp.Entities;
using System;
using System.Collections.Generic;

namespace MyApp.Entities.Generated
{
    public record ReadOnlyReservation
    {
        public int Id { get; init; }

        public ReadOnlyReservation(int id)
        {
            Id = id;
        }
    }
}
""";

        var expectedDbContext = """
using Microsoft.EntityFrameworkCore;
using MyApp.Entities;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MyApp.Entities.Generated
{
    public partial class ReadOnlyMyDbContext : DbContext, IReadOnlyMyDbContext
    {
        public DbSet<ReadOnlyReservation> Reservations { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ReadOnlyReservation>().HasData(new ReadOnlyReservation(7) { Id = 1 });
        }

        public sealed override int SaveChanges()
        {
            throw new NotImplementedException("Do not call SaveChanges on a readonly db context.");
        }

        public sealed override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException("Do not call SaveChangesAsync on a readonly db context.");
        }

        public sealed override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException("Do not call SaveChangesAsync on a readonly db context.");
        }

        IQueryable<ReadOnlyReservation> IReadOnlyMyDbContext.Reservations => Reservations;
        IQueryable<TEntity> IReadOnlyMyDbContext.Set<TEntity>()
            where TEntity : class => Set<TEntity>();
    }
}
""";

        var expectedInterface = """
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using MyApp.Entities;
using System;
using System.Linq;

namespace MyApp.Entities.Generated
{
    public partial interface IReadOnlyMyDbContext : IDisposable, IAsyncDisposable
    {
        IQueryable<ReadOnlyReservation> Reservations { get; }

        IQueryable<TEntity> Set<TEntity>()
            where TEntity : class;
        DatabaseFacade Database { get; }
    }
}
""";

        static string ToCrLf(string source) => source.Replace("\r\n", "\n").Replace("\n", "\r\n");

        var test = new VerifyCS.Test
        {
            TestState =
            {
                Sources = { inputSource },
                AdditionalReferences =
                {
                    MetadataReference.CreateFromFile(typeof(DbContext).Assembly.Location)
                },
                GeneratedSources =
                {
                    (typeof(ReadonlyDbContextGenerator.ReadOnlyDbContextGenerator), "ReadOnlyReservation.g.cs", ToCrLf(expectedReservation)),
                    (typeof(ReadonlyDbContextGenerator.ReadOnlyDbContextGenerator), "ReadOnlyMyDbContext.g.cs", ToCrLf(expectedDbContext)),
                    (typeof(ReadonlyDbContextGenerator.ReadOnlyDbContextGenerator), "IReadOnlyMyDbContext.g.cs", ToCrLf(expectedInterface))
                }
            },
        };

        test.SolutionTransforms.Add((solution, projectId) =>
        {
            var project = solution.GetProject(projectId);
            var options = (CSharpCompilationOptions)project.CompilationOptions;
            options = options.WithSpecificDiagnosticOptions(options.SpecificDiagnosticOptions.SetItems(new Dictionary<string, ReportDiagnostic>
            {
                ["CS8618"] = ReportDiagnostic.Suppress
            }));
            return project.WithCompilationOptions(options).Solution;
        });

        await test.RunAsync();
    }
}
