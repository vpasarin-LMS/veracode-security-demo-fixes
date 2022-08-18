using System.Data.SqlClient;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Web.Mvc;
using VeraDemoNet.DataAccess;

namespace VeraDemoNet.Controllers
{
    public abstract class AuthControllerBase : Controller
    {
        protected BasicUser LoginUser(string userName, string passWord)
        {
            if (string.IsNullOrEmpty(userName))
            {
                return null;
            }

            using (var dbContext = new BlabberDB())
            {
                var getUserSaltAndKey = dbContext.Database.SqlQuery<User>(
                    "select username, password_salt as passwordSalt, password_key as passwordKey from users where" +
                    "username = @username;",
                    new SqlParameter("username", userName)).First();

                if (getUserSaltAndKey == null)
                {
                    return null;
                } else
                {
                    using (var deriveBytes = new Rfc2898DeriveBytes(passWord, getUserSaltAndKey.PasswordSalt))
                    {
                        byte[] newKey = deriveBytes.GetBytes(20);

                        if (!newKey.SequenceEqual(getUserSaltAndKey.PasswordKey))
                            return null;
                    }
                }

                var found = dbContext.Database.SqlQuery<BasicUser>(
                    "select username, real_name as realname, blab_name as blabname, is_admin as isadmin from users where "
                    + "username = @username and password = @password;",
                    new SqlParameter("username", userName),
                    new SqlParameter("password", Sha256Hash(passWord))).ToList();

                /*var found = dbContext.Database.SqlQuery<BasicUser>(
                    "select username, real_name as realname, blab_name as blabname, is_admin as isadmin from users where username ='"
                    + userName + "' and password='" + Md5Hash(passWord) + "';").ToList();*/

                if (found.Count != 0)
                {
                    Session["username"] = userName;
                    return found[0];
                }
            }

            return null;
        }

        protected string GetLoggedInUsername()
        {
            return Session["username"].ToString();
        }

        protected void LogoutUser()
        {
            Session["username"] = null;
        }

        protected bool IsUserLoggedIn()
        {
            return string.IsNullOrEmpty(Session["username"] as string) == false;

        }

        protected RedirectToRouteResult RedirectToLogin(string targetUrl)
        {
            return new RedirectToRouteResult(
                new System.Web.Routing.RouteValueDictionary
                (new
                {
                    controller = "Account",
                    action = "Login",
                    ReturnUrl = HttpContext.Request.RawUrl
                }));
        }

        protected static string Md5Hash(string input)
        {
            var sb = new StringBuilder();
            if (string.IsNullOrEmpty(input))
            {
                return sb.ToString();
            }

            using (MD5 md5 = MD5.Create())
            {
                var retVal = md5.ComputeHash(Encoding.Unicode.GetBytes(input));

                foreach (var t in retVal)
                {
                    sb.Append(t.ToString("x2"));
                }
            }

            return sb.ToString();
        }

        protected static string Sha256Hash(string input)
        {
            StringBuilder builder = new StringBuilder();
            if (string.IsNullOrEmpty(input))
            {
                return sb.ToString();
            }

            using (SHA256 sha256Hash = SHA256.Create())
            {
                byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(input));

                for (int i = 0; i < bytes.Length; i++)
                {
                    builder.Append(bytes[i].ToString("x2"));
                }
                return builder.ToString();
            }
        }
    }
}