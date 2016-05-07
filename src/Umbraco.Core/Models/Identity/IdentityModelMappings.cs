using System;
using System.Linq;
using AutoMapper;
using Umbraco.Core.Models.Mapping;
using Umbraco.Core.Models.Membership;
using Umbraco.Core.Security;

namespace Umbraco.Core.Models.Identity
{
    public class IdentityModelMappings : ModelMapperConfiguration
    {
        public override void ConfigureMappings(IMapperConfiguration config, ApplicationContext applicationContext)
        {
            config.CreateMap<IUser, BackOfficeIdentityUser>()
                .ForMember(user => user.Email, expression => expression.MapFrom(user => user.Email))
                .ForMember(user => user.Id, expression => expression.MapFrom(user => user.Id))
                .ForMember(user => user.LockoutEnd, expression => expression.MapFrom(user => user.IsLockedOut ? DateTime.MaxValue.ToUniversalTime() : (DateTime?) null))
                .ForMember(user => user.UserName, expression => expression.MapFrom(user => user.Username))
                .ForMember(user => user.PasswordHash, expression => expression.MapFrom(user => GetPasswordHash(user.RawPasswordValue)))
                .ForMember(user => user.Culture, expression => expression.MapFrom(user => user.GetUserCulture(applicationContext.Services.TextService)))
                .ForMember(user => user.Name, expression => expression.MapFrom(user => user.Name))
                .ForMember(user => user.StartMediaId, expression => expression.MapFrom(user => user.StartMediaId))
                .ForMember(user => user.StartContentId, expression => expression.MapFrom(user => user.StartContentId))
                .ForMember(user => user.UserTypeAlias, expression => expression.MapFrom(user => user.UserType.Alias))
                .ForMember(user => user.AccessFailedCount, expression => expression.MapFrom(user => user.FailedPasswordAttempts))
                .ForMember(user => user.AllowedSections, expression => expression.MapFrom(user => user.AllowedSections.ToArray()))
                .ForMember(user => user.LockoutEnabled, expression => expression.Ignore())
                .ForMember(user => user.Logins, expression => expression.Ignore())
                .ForMember(user => user.LoginsChanged, expression => expression.Ignore())
                .ForMember(user => user.EmailConfirmed, expression => expression.Ignore())
                .ForMember(user => user.PhoneNumber, expression => expression.Ignore())
                .ForMember(user => user.PhoneNumberConfirmed, expression => expression.Ignore())
                .ForMember(user => user.TwoFactorEnabled, expression => expression.Ignore())
                .ForMember(user => user.Roles, expression => expression.Ignore())
                .ForMember(user => user.Claims, expression => expression.Ignore());

            config.CreateMap<BackOfficeIdentityUser, UserData>()
                .ConstructUsing((BackOfficeIdentityUser user) => new UserData(Guid.NewGuid().ToString("N"))) //this is the 'session id'
                .ForMember(detail => detail.Id, opt => opt.MapFrom(user => user.Id))
                .ForMember(detail => detail.AllowedApplications, opt => opt.MapFrom(user => user.AllowedSections))
                .ForMember(detail => detail.RealName, opt => opt.MapFrom(user => user.Name))
                .ForMember(detail => detail.Roles, opt => opt.MapFrom(user => new[] { user.UserTypeAlias }))
                .ForMember(detail => detail.StartContentNode, opt => opt.MapFrom(user => user.StartContentId))
                .ForMember(detail => detail.StartMediaNode, opt => opt.MapFrom(user => user.StartMediaId))
                .ForMember(detail => detail.Username, opt => opt.MapFrom(user => user.UserName))
                .ForMember(detail => detail.Culture, opt => opt.MapFrom(user => user.Culture))
                .ForMember(detail => detail.SessionId, opt => opt.MapFrom(user => user.SecurityStamp.IsNullOrWhiteSpace() ? Guid.NewGuid().ToString("N") : user.SecurityStamp));
        }

        private string GetPasswordHash(string storedPass)
        {
            return storedPass.StartsWith("___UIDEMPTYPWORD__") ? null : storedPass;
        }
    }
}