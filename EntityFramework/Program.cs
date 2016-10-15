using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CredoLab.Mobile;
using Reflector.ReflectionEmitLanguage;

namespace EntityFramework
{

    public class CredoAppConstants1
    {
        public class Contact
        {
            public const string CONSTANT = "1";

            public const string DataSourceType = "Contact";

            public const string CONSTANT1 = "21";

            public string _ID = "_id";

            public string CUSTOM_RINGTONE = "custom_ringtone";

            public string LAST_TIME_CONTACTED = "last_time_contacted";

            public static  readonly string CUSTOM_RINGTONE_1 = "custom_ringtone";

            public static readonly string LAST_TIME_CONTACTED_2 = "last_time_contacted";

        }
    }

    class Program
    {
        private static string _fullNameTemplate = "{0} {1}";

        static void Main(string[] args)
        {
            var officeContext = new OfficeContext();
            //officeContext.Users.AddRange(CreateUsers());
            //officeContext.SaveChanges();
            var filteringUser = FilteringUser(officeContext.Users, "vit");
            filteringUser.ToList().ForEach(u => Console.WriteLine(_fullNameTemplate, u.Name, u.LastName));
            var contactStatus = CredoAppConstants.AudioExternal.DATE_MODIFIED;

            Console.ReadKey();
        }
        public static IQueryable<User> FilteringUser(IQueryable<User> users, string filter)
        {
            var templateSql = new Regex("[{}]").Replace(_fullNameTemplate, string.Empty);
            return users.Where(user => templateSql.Replace("0", user.Name).Replace("1", user.LastName).ToLower().Contains(filter));
        }

        public static IEnumerable<User> CreateUsers()
        {
            yield return new User()
            {
                Name = "Vitek",
                LastName = "Ivanov"
            };

            yield return new User()
            {
                Name = "Anna",
                LastName = "Direza"
            };

            yield return new User()
            {
                Name = "Katya",
                LastName = "Tirevitova"
            };
        }

    }
}
