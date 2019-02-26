# CosmosDB Exploration

In this epository I explore the use of CosmosDB. I am researching this as an
alternative to trying to put together a geo replicated multi-master SQL Server
replication. CosmosDB puts forth an appealing offer with a global distributed
geo replication for free out of the box, however I do not like the non-SQL API
so I am going to focus on the SQL API it provides. Also I am going to see how
ready it is to be used from EF.

I am working from this announcement article posted here:
https://devblogs.microsoft.com/dotnet/announcing-entity-framework-core-2-2-preview-2/

There are a few more links I may or may not get to later:
- https://www.letsbuildit.com/first-look-cosmos-db-sql-provider-in-entity-framework-core/
- https://www.letsbuildit.com/first-look-cosmos-db-sql-provider-in-entity-framework-core-part-2-with-azure-functions/

The first thing to do is to create a new .NET Core Console application:

```powershell
dotnet new console
```

Next up we need to add the NuGet packages for EF and Cosmos:
https://www.nuget.org/packages/Microsoft.EntityFrameworkCore.Cosmos.Sql
There are no stable packages yet, so we need to specify the version for pre-release.

```powershell
dotnet add package Microsoft.EntityFrameworkCore.Cosmos.Sql -v 2.2.0-preview2-35157
```

Let's create a few demo model classes and the DB context class so that we can
configure to Cosmos next. I am going to go with a User entity with a set of Car
entities all of which have their own sets of Trip entities.

```csharp
public class AppDbContext: DbContext
{
    public DbSet<User> Users { get; set; }
    public DbSet<Car> Cars { get; set; }
    public DbSet<Trip> Trips { get; set; }
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        Console.WriteLine("Enter the primary key from https://localhost:8081/_explorer/index.html:");
        optionsBuilder.UseCosmosSql("https://localhost:8081", Console.ReadLine(), nameof(cosmos_db_exploration));
    }
}
public class User
{
    public int Id { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public ICollection<Car> Cars { get; set; }
}
public class Car
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User User { get; set; }
    public string Make { get; set; }
    public string Model { get; set; }
    public ICollection<Trip> Trips { get; set; }
}
public class Trip
{
    public int Id { get; set; }
    public int CarId { get; set; }
    public Car Car { get; set; }
    public int DistanceInKilometers { get; set; }
}
```

Ensure you have installed the CosmosDB emulator locally so that you don't have to
muck around with Azure for a simple test.
https://docs.microsoft.com/en-us/azure/cosmos-db/local-emulator

Once that is done we can try to connect to the database. Normally I would just use
`EnsureDeleted` and `EnsureCreated` in their sync variants but it looks like the
Cosmos provider requires the use of their async variants as the sync ones are not
implemented. This is a good chance to switch to C# 7.1+ to be able to use
`async Task Main`.

Add `LangVersion` to the CSPROJ file:

```xml
<PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>netcoreapp2.2</TargetFramework>
    <RootNamespace>cosmos_db_exploration</RootNamespace>
    <LangVersion>7.1</LangVersion>
</PropertyGroup>
```

After this change and your `Main` implementation like this:

```csharp
static async Task Main(string[] args)
{
    using (var appDbContext = new AppDbContext())
    {
        await appDbContext.Database.EnsureDeletedAsync();
        await appDbContext.Database.EnsureCreatedAsync();
        Console.WriteLine("The database has been reset.");
    }
}
```

An issue of `dotnet run` should greet you with a *The database has been reset*
message.

Next up is a test of seeding the database. This is straightforward through the
usual EF facilities, but note that Cosmos doesn't have a key generator, EF will
generate new key values for GUIDs, but not integers, so it's best to submit in
this case. Keeping track of distributed integers is basically as hard as setting
up geo distributed multi-master replication which is what we're trying to avoid
in the first place.
