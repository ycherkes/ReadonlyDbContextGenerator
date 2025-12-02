using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
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

        test.TestBehaviors |= TestBehaviors.SkipGeneratedSourcesCheck;

        await test.RunAsync();
    }
}
