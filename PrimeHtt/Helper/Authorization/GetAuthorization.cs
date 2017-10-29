using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using PrimeHtt.Models;

namespace PrimeHtt.Helper.Authorization
{
    public class GetAuthorization
    {
        public static string GetUsername(string id)
        {
            using (var db = new PrimeTravelEntities())
            {
                var userId = Convert.ToInt64(id);
                var username = (from u in db.User
                    where u.UserId == userId
                    select u.Username).FirstOrDefault();
                return username;
            }
        }
    }
}