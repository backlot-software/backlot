using System.Diagnostics;
using System.Net;
using System.Reflection;
using Backlot.Core.Exceptions;
using Backlot.Http.Media.Formatters;
using Microsoft.Extensions.Logging;

namespace Backlot.Http.Media
{
    public class MediaFormatResolver(IEnumerable<IMediaFormatter> mediaFormatters) : IMediaFormatResolver
    {
        private IMediaFormatter GetMediaFormatter(RequestData request)
        {
            if (request.Message.Headers.TryGetValues("accept", out var acceptHeaders))
            {
                var headerArr = acceptHeaders.ToArray();
                
                if (headerArr.Any())
                {
                    var acceptedMediaType = headerArr.First();
                    var formatter = mediaFormatters.FirstOrDefault(x =>
                        acceptedMediaType.StartsWith(x.MediaType, StringComparison.InvariantCultureIgnoreCase));

                    return formatter ?? new JsonFormatter();
                }
            }

            return new JsonFormatter();
        }


        public async Task<ResponseData> GetMediaResponseData<T>(
            RequestData request, 
            T returnObj, 
            Stopwatch stopwatch,
            ILogger logger,
            HttpStatusCode statusCode = HttpStatusCode.OK)
        {
            if(returnObj is Exception ex)
            {
                // when exceptions
                return await GetExceptionResponseData(request, ex, logger, stopwatch);
            }
            
            var mediaFormatter = GetMediaFormatter(request);
                return await mediaFormatter.GetResponse(request, returnObj, stopwatch, statusCode);
        }

        private async Task<ResponseData> GetExceptionResponseData(
            RequestData request,
            Exception exception,
            ILogger logger,
            Stopwatch sw)
        {
            #if DEBUG
            var debugMode = request.Message.RequestUri != null && request.Message.RequestUri.Query.Contains("debug=on", StringComparison.InvariantCultureIgnoreCase);
            #else // by default and for production code, debugMode is always compliled as being false for security purpose
            var debugMode = false;
            #endif
            
            var jsonFormatter = new JsonFormatter();
            var excId = Guid.NewGuid().ToString("N");

            async Task<ResponseData> DebugResponse(Exception drex, HttpStatusCode code)
            {
                return await jsonFormatter.GetResponse(
                    request,
                    $"{drex.GetType().FullName} | {drex.Message} | ExceptionMode = 'debug' | StackTrace: {drex.StackTrace}",
                    sw,
                    code);
            }
            
            async Task<ResponseData> ValidationException(ValidationException vex)
            {
                #region log
                // no extra warnings needed for validation handled.
                // information is already added while throwning the exception.
                #endregion

                return await jsonFormatter.GetResponse(
                    request,
                    vex.Validations,
                    sw,
                    HttpStatusCode.BadRequest);
            }
            
            async Task<ResponseData> UnAuthorizedException(UnauthorizedAccessException uaex)
            {
                #region log
                
                logger.LogWarning(uaex, "An unauthorized exception occured, during {Uri}",
                    request.Message.RequestUri?.AbsolutePath);
                #endregion

                if (debugMode) return await DebugResponse(uaex, HttpStatusCode.Unauthorized);
                else
                {
                    return await jsonFormatter.GetResponse(
                        request,
                        $"An unauthorized exception occured and is logged with ID: {excId}. Or use ExceptionMode debug to get more information.",
                        sw,
                        HttpStatusCode.Unauthorized);
                }
            }
            
            async Task<ResponseData> PermissionException(PermissionControlException pce)
            {
                #region log
                logger.LogWarning(pce, "A permission control exception occured, within {Uri}",
                    request.Message.RequestUri?.AbsolutePath);
                #endregion

                if (debugMode) return await DebugResponse(pce, HttpStatusCode.Unauthorized);
                else
                {
                    return await jsonFormatter.GetResponse(
                        request,
                        $"An permission control exception occured and is logged with ID: {excId}. Or use ExceptionMode debug to get more information.",
                        sw,
                        HttpStatusCode.Unauthorized);
                }
            }

            async Task<ResponseData> DefaultException(Exception dex)
            {
                #region log
                logger.LogError(dex, "{ExcId}: An unexpected '{ExcType}' occured '{ExcMessage}', within {Uri}",
                    excId,
                    dex.GetType().FullName,
                    dex.Message,
                    request.Message.RequestUri?.AbsolutePath);
                #endregion


                if (debugMode) return await DebugResponse(dex, HttpStatusCode.InternalServerError);
                else
                {
                    return await jsonFormatter.GetResponse(
                        request,
                        $"An unhandled exception occured and is logged with ID: {excId}. Or use ExceptionMode debug to get more information.",
                        sw,
                        HttpStatusCode.InternalServerError);
                }
            }
            
            return exception switch
            {     
                NotFoundException nfe => await GetMediaResponseData(request,
                    nfe.Message, sw, logger, HttpStatusCode.NotFound),
                
                BadRequestException bre => await GetMediaResponseData(request,
                    bre.Message, sw, logger, HttpStatusCode.BadRequest),
                
                PermissionControlException pce => await PermissionException(pce),
                UnauthorizedAccessException uae => await UnAuthorizedException(uae),
                ValidationException vex => await ValidationException(vex),
                
                #region TargetInvocationException -->
                TargetInvocationException tiex when tiex.InnerException is UnauthorizedAccessException uaex =>
                    await UnAuthorizedException(uaex),
                TargetInvocationException tiex when tiex.InnerException is ValidationException tiexVex =>
                    await ValidationException(tiexVex),
                TargetInvocationException  => await DefaultException(exception.InnerException ?? exception),
                _ => await DefaultException(exception)
                #endregion
            };
        }
    }
}
