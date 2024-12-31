using Microsoft.CodeAnalysis;
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
                                                  }
                                              }
                                              """;

        // Expected generated output for the IReadOnlyDbContext interface
        var expectedIReadOnlyDbContextSource = """
                                               using Microsoft.EntityFrameworkCore;
                                               using MyApp.Entities;
                                               using System;
                                               using System.Collections.Generic;
                                               
                                               namespace MyApp.Entities.Generated
                                               {
                                                   public partial interface IReadOnlyMyDbContext : IDisposable, IAsyncDisposable
                                                   {
                                                       DbSet<ReadOnlyUser> Users { get; }

                                                       DbSet<TEntity> Set<TEntity>()
                                                           where TEntity : class;
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
}