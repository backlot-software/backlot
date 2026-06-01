using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Backlot.Authentication.BuiltIn.Roles;
using Backlot.Authentication.BuiltIn.Services;
using Backlot.Core;
using Backlot.Core.Abstraction.Configuration;
using Backlot.Core.Abstraction.Scenarios;
using Backlot.Core.Services;
using Backlot.Defaults.Services;
using Microsoft.Extensions.Logging;

#pragma warning disable CS8620 // Argument cannot be used for parameter due to differences in the nullability of reference types.

namespace Backlot.Authentication.BuiltIn.Scenarios
{
	[Scenario(typeof(Login), access: [Core.Security.Access.Open])]
	[SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
	[SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Global")]
	public class Login : Scenario<IAuth, bool> // for backwards compatibility we define this with the IAuth instead of IAuthUser
	{
		public event AsyncEventHandler<EventArgs> Authenticated;

		/// <summary>
		/// the "final jwt token", for the request IAuth request.
		/// </summary>
		public string Token { get; set; }

		private string UiUrl => !string.IsNullOrWhiteSpace(Role.UiUrl) ? Role.UiUrl : _configurationManager.Get<Settings>(s => s.UiUrl);
		private string RootUrl => _configurationManager.Get<Settings>(s => s.RootUrl);
		// ReSharper disable once UnusedMember.Global : Used by mustach templates.
		public string UrlWithToken => $"{UiUrl}?token={Token}&env={RootUrl}";
		
		[Configurable]
		public int TokenDurationInMinutes { get; set; } = 15;

		private readonly JwtTokenService _tokenService;
		private readonly IUserRepository _userRepository;
		private readonly ITokenRepository _tokenRepository;
		private readonly IConfigurationManager _configurationManager;
		private bool ManageTokenRevocation => Convert.ToBoolean(_configurationManager.Get<Settings>(s => s.ManageTokenRevocation));
		

#pragma warning disable CS8618 //properties don't need to be declared in the constructor when they are configurable & Token gets set when playing the scenario
		public Login(IAuth role, JwtTokenService tokenService, IUserRepository userRepository, ITokenRepository tokenRepository, IConfigurationManager configurationManager) : base(role)
		{
#pragma warning restore CS8618
			_tokenService = tokenService;
			_userRepository = userRepository;
			_tokenRepository = tokenRepository;
			_configurationManager = configurationManager;
		}

		public override bool Validate()
		{
			if (!string.IsNullOrEmpty(Role.UserName))
			{
				return true;
			}

			ValidationResults.Add(new ValidationResult("Given credentials are not matching criteria", [nameof(IAuth.UserName)
			]));

			return false;
		}

		protected override async Task<bool> ExecAsync()
		{
			try
			{
				Token = string.Empty;

				var tryGetUser = await _userRepository.TryGetUser(Role.UserName);

				if (tryGetUser.success)
				{
					var ttl = DateTimeOffset.Now.AddMinutes(TokenDurationInMinutes);
					var ctx = _tokenService.CreateRefreshToken(new Dictionary<string, string>
						{
							{ _tokenService.ClaimNames.Username, tryGetUser.username },
							// we do not add groups to the claim of a "REFRESH" token.
						}, ttl);

					if (ManageTokenRevocation)
					{
						// add as valid token to an authentication database
						await _tokenRepository.AddAsync(ctx.id, ttl);
					}
					
					Token = ctx.token;
					
					await FireAsync(Authenticated, nameof(Authenticated));
				}
			}
			catch (Exception ex)
			{
				Token = string.Empty;
				Logger.LogError("Exception occured at {Scenario} --> {ExceptionMessage}", Reference.Name, ex.Message);
				return false; // an unexpected exception occurred.
			}

			return true; // always return true when no exceptions are thrown.
		}
	}
}

