using System;
using System.Collections.Specialized;
using System.Configuration.Provider;
using System.Web.Security;
using Umbraco.Core.Logging;
using Umbraco.Web.Security.Providers;

namespace UmbracoActiveDirectoryProvider
{
    public class UmbracoActiveDirectoryMembershipProvider : MembersMembershipProvider
    {
        private string adProviderName;

        public override void Initialize(string name, NameValueCollection config)
        {
            if (config == null)
                return;

            adProviderName = config["adProviderName"];

            if (String.IsNullOrEmpty(adProviderName))
                throw new ProviderException("The attribute 'adProviderName' is missing or empty.");

            config.Remove("adProviderName");

            base.Initialize(name, config);
        }

        public override bool ValidateUser(string username, string password)
        {
            MembershipProvider ADProvider = Membership.Providers[adProviderName];
            if(ADProvider == null)
                throw new ProviderException("The Active Directory provider was not load.");

            if (!ADProvider.ValidateUser(username, password))
                return false;

            MembershipUser adMember = ADProvider.GetUser(username, false);
            if (adMember == null)
                return false;

            if (base.GetUser(username, false) != null)
                return true;

            try
            {
                MembershipCreateStatus status;

                base.CreateUser(adMember.UserName, password, adMember.Email, adMember.PasswordQuestion, null, adMember.IsApproved, null, out status);

                return status == MembershipCreateStatus.Success;
            }
            catch (Exception ex)
            {
                LogHelper.Error<UmbracoActiveDirectoryMembershipProvider>("Error while creating AD User as an Umbraco Member.", ex);
            }

            return false;
        }

        public override MembershipUser CreateUser(string username, string password, string email, string passwordQuestion, string passwordAnswer, bool isApproved, object providerUserKey, out MembershipCreateStatus status)
        {
            status = MembershipCreateStatus.UserRejected;
            return null;
        }

        public override bool ChangePassword(string username, string oldPassword, string newPassword)
        {
            return false;
        }

        public override bool ChangePasswordQuestionAndAnswer(string username, string password, string newPasswordQuestion, string newPasswordAnswer)
        {
            return false;
        }

        public override string ResetPassword(string username, string answer)
        {
            return "It's not work for Active Directory provider.";
        }

        public override void UpdateUser(MembershipUser user)
        {
        }
    }
}
