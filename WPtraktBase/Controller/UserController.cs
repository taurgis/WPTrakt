using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WPtrakt.Model.Trakt;
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

        public async Task<Boolean> ValidateUser()
        {
            return await userDao.ValidateUser();
        }

        public async Task<RegistrationResult> CreateUser(String username, String password, String email)
        {
            return await userDao.CreateUser(username, password, email);
        }

        public async Task<TraktProfile> GetUserProfile()
        {
            return await userDao.GetUserProfile();
        }

        public async Task<Boolean> CancelActiveCheckin()
        {
            return await userDao.CancelActiveCheckin();
        }
    }
}
