using System.Data.Entity;

namespace EntityFramework
{
    public class OfficeContext : DbContext
    {
        public DbSet<User> Users { get; set; }

        public OfficeContext():base("UsersDb")
        {
            
        }
    }

    public class User
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public string LastName { get; set; }
    }
}