using System.Threading.Tasks;
using Backlot.Authentication.BuiltIn.Services;
using Backlot.Core;
using Backlot.Core.Abstraction.Scenarios;

#pragma warning disable CS8620 // Argument cannot be used for parameter due to differences in the nullability of reference types.

namespace Backlot.Authentication.BuiltIn.Scenarios
{
	[Scenario(typeof(Logout), access: [Core.Security.Access.Everyone])]
	public class Logout : Scenario<IDirector, bool> // for backwards compatibility we define this with the IAuth instead of IAuthUser
	{
		private readonly JwtTokenService _tokenService;
		private readonly ITokenRepository _tokenRepository;
		
		public Logout(IDirector role, JwtTokenService tokenService, ITokenRepository tokenRepository) : base(role)
		{
			_tokenService = tokenService;
			_tokenRepository = tokenRepository;
		}

		protected override async Task<bool> ExecAsync()
		{
			await _tokenRepository.RevokeAsync(Core.Security.UserContext.Current.Claims[_tokenService.ClaimNames.Id]);
			return true; // always return true when no exceptions are thrown.
		}
	}
}

