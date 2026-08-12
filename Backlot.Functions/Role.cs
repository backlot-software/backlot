using System.Diagnostics;
using Backlot.Core;
using Backlot.Core.DependencyInjection;
using Backlot.Core.Exceptions;
using Backlot.Core.Services;
using Backlot.Http;
using Backlot.Http.Media;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using HttpRequestData = Microsoft.Azure.Functions.Worker.Http.HttpRequestData;

namespace Backlot.Functions
{
    public class RoleApi
    {
        private readonly ILogger<RoleApi> _logger;
        private readonly IMediaFormatResolver _responseTypeResolver;
        private readonly IPersistedRoleRepository _roleRepository;

        public RoleApi(ILogger<RoleApi> logger, IMediaFormatResolver responseTypeResolver)
        {
            _logger = logger;
            _responseTypeResolver = responseTypeResolver;
            _roleRepository = ServiceLocator.Get<IPersistedRoleRepository>();
        }

        #region Request Handling
        
        [Function("role-play-get")]
        public async Task<HttpResponseData> PlayGet(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "role/{rolename}/{scenario}")]
            HttpRequestData req,
            string rolename,
            string scenario)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            try
            {
                if (req.Body.Length > 0)
                    throw new BadRequestException("GET requests should not contain a body.");
                
                var roleType = Loader.GetRoleByName(rolename);

                IRole role;

                if (typeof(IDirector).IsAssignableFrom(roleType))
                {
                    role = ServiceLocator.Get<IDirector>();
                }
                else
                {
                    if (req.Query["uid"] == null || req.Query.Count > 1)
                        throw new BadRequestException(
                            "The query string must contain only 'uid' when using roles other than director in GET requests.");
                    
                    if(!typeof(IPersist).IsAssignableFrom(roleType))
                        throw new BadRequestException(
                            $"{roleType} is not a persistable roletype inheriting from IPersist. Make sure you use the correct roletype for your request.");
                    
                    if (!_roleRepository.TryGet(req.Query["uid"], roleType, out var roleOut))
                    {
                        throw new NotFoundException($"Role with uid '{req.Query["uid"]}' not found.");
                    }

                    role = roleOut;
                }

                return await Execute(req, [role], scenario, stopwatch);
            }
            catch (Exception ex)
            {
                return await _responseTypeResolver.GetHttpResponseData(req, ex, stopwatch, _logger);
            }
        }

        [Function("role-play-post")]
        public async Task<HttpResponseData> PlayPost(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "role/{rolename}/{scenario}")]
            HttpRequestData req,
            string rolename,
            string scenario)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            try
            {
                if (req.Body.Length == 0)
                    throw new BadRequestException("POST requests must contain a body.");

                if (req.Query.Count > 0)
                    throw new BadRequestException("POST requests should not contain query strings.");

                var roles = await GetRoles.ForPostRequest(req.Body, rolename, _roleRepository);

                return await Execute(req, roles.ToArray(), scenario, stopwatch);
            }
            catch (Exception ex)
            {
                return await _responseTypeResolver.GetHttpResponseData(req, ex, stopwatch, _logger);
            }
        }

        #endregion

        private async Task<HttpResponseData> Execute(
            HttpRequestData req,
            IRole[] roles,
            string scenario,
            Stopwatch stopwatch)
        {
            var returnObj = await roles.PlayAuthAsync(scenario);

            return await _responseTypeResolver.GetHttpResponseData(req, returnObj, stopwatch, _logger);
        }
    }
}
