using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace cosmos_db_exploration
{
    class Program
    {
        static async Task Main(string[] args)
        {
            string primaryKey;
            try
            {
                Console.WriteLine("Downloading the primary key from https://localhost:8081/_explorer/quickstart.html…");
                var html = await new HttpClient().GetStringAsync("https://localhost:8081/_explorer/quickstart.html");
                primaryKey = Regex.Match(html, "Primary Key</p>\\s+<input .* value=\"(?<primaryKey>.*)\"").Groups["primaryKey"].Value;
                Console.WriteLine("The primary key has been downloaded.");
            } catch {
                Console.WriteLine("Failed to download the primary key. Make sure to install and run the Cosmos emulator.");
                Console.WriteLine("The primary key gets downloaded from https://localhost:8081/_explorer/quickstart.html");
                return;
            }

            Guid userId;
            using (var appDbContext = new AppDbContext(primaryKey))
            {
                await appDbContext.Database.EnsureDeletedAsync();
                await appDbContext.Database.EnsureCreatedAsync();
                Console.WriteLine("The database has been reset.");
                var user = new User {
                    FirstName = "Tomas",
                    LastName = "Hubelbauer",
                    Cars = new List<Car>() {
                        new Car {
                            Make = "Tesla",
                            Model = "3",
                            Trips = new List<Trip>(),
                        }
                    },
                };

                await appDbContext.Users.AddAsync(user);
                await appDbContext.SaveChangesAsync();
                userId = user.Id;
                Console.WriteLine("The database has been seeded. See at https://localhost:8081/_explorer/index.html");
            }

            using (var appDbContext = new AppDbContext(primaryKey))
            {
                Console.WriteLine(userId);
                var user = await appDbContext.Users.Include(u => u.Cars).SingleAsync(u => u.Id == userId);
                Console.WriteLine(user);
            }
        }
    }
    public class AppDbContext: DbContext
    {
        private readonly string primaryKey;
        public AppDbContext(string primaryKey)
        {
            this.primaryKey = primaryKey;
        }
        public DbSet<User> Users { get; set; }
        public DbSet<Car> Cars { get; set; }
        public DbSet<Trip> Trips { get; set; }
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseCosmos("https://localhost:8081", primaryKey, nameof(cosmos_db_exploration));
        }
    }
    public class User
    {
        public Guid Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public ICollection<Car> Cars { get; set; }
    }
    public class Car
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public User User { get; set; }
        public string Make { get; set; }
        public string Model { get; set; }
        public ICollection<Trip> Trips { get; set; }
    }
    public class Trip
    {
        public Guid Id { get; set; }
        public Guid CarId { get; set; }
        public Car Car { get; set; }
        public int DistanceInKilometers { get; set; }
    }
}
