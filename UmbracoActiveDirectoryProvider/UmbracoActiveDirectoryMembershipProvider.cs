using System;
using System.Collections.Specialized;
using System.Configuration.Provider;
using System.Web.Security;
using Umbraco.Core.Logging;
using Umbraco.Web;
using Umbraco.Web.Security;

namespace UmbracoActiveDirectoryProvider
{
    public class UmbracoActiveDirectoryMembershipProvider : ActiveDirectoryMembershipProvider
    {
        private string m_DefaultMemberType;

        public override void Initialize(string name, NameValueCollection config)
        {
            if (config == null)
                return;

            m_DefaultMemberType = config["defaultMemberType"];

            if (String.IsNullOrEmpty(m_DefaultMemberType))
                throw new ProviderException("The attribute 'defaultMemberType' is missing or empty.");

            // Remove config attribute as the AD membership provider
            // doesn't like other attributes
            config.Remove("defaultMemberType");

            base.Initialize(name, config);
        }

        public override bool ValidateUser(string username, string password)
        {
            var retval = base.ValidateUser(username, password);

            // Find the Active Directory member
            var adMember = GetUser(username, false);
            if (adMember == null)
                return false;

            return true;

            //// Login has to be valid and we need to have Umbraco 
            //// member's membership provider present
            //if (retval && Membership.Providers[Umbraco.Core.Constants.Conventions.Member.UmbracoMemberProviderName] != null)
            //{
            //    var provider = Membership.Providers[Umbraco.Core.Constants.Conventions.Member.UmbracoMemberProviderName];

            //    // If we already have an Umbraco member for the valid log
            //    // credentials, do not create the member again
            //    if (provider.GetUser(username, false) != null)
            //        return true;

            //    try
            //    {
            //        var helper = new MembershipHelper(UmbracoContext.Current);

            //        var model = helper.CreateRegistrationModel(m_DefaultMemberType);
            //        model.Username = username;
            //        model.Password = password;
            //        model.Email = adMember.Email;
            //        model.UsernameIsEmail = false;
            //        model.Name = username;

            //        // Create the new ad member in umbraco
            //        MembershipCreateStatus status;
            //        helper.RegisterMember(model, out status);

            //        // If we haven't created the member correctly, return an
            //        // unsuccessful login
            //        return status == MembershipCreateStatus.Success;
            //    }
            //    catch (Exception ex)
            //    {
            //        LogHelper.Error<UmbracoActiveDirectoryMembershipProvider>("Error while creating AD User as an Umbraco Member.", ex);
            //    }
            //}

            //// No matter if retval is true or false, we have to return
            //// false because we can't create or make sure the ad user 
            //// exists as an Umbraco member
            //return false;
        }
    }
}
