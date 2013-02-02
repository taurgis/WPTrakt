using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WPtraktBase.DAO;
using WPtraktBase.Model.Trakt;

namespace WPtraktBase.Controller
{
    public class UserController
    {
        private UserDao userDao;

        public UserController()
        {
            userDao = new UserDao();
        }

        public async Task<TraktLastActivity> getLastActivityForUser()
        {
            return await userDao.FetchLastActivity();
        }
    }
}
