using System;
using Umbraco.Core.Logging;
using Umbraco.Core.Models;
using Umbraco.Core.Services;

namespace Umbraco.Core.Publishing
{
    /// <summary>
    /// Used to perform scheduled publishing/unpublishing
    /// </summary>
    internal class ScheduledPublisher
    {
        private readonly IContentService _contentService;
        private readonly IUserService _userService;

        public ScheduledPublisher(IContentService contentService, IUserService userService)
        {
            _contentService = contentService;
            _userService = userService;
        }

        public void CheckPendingAndProcess()
        {
            foreach (var d in _contentService.GetContentForRelease())
            {
                try
                {
                    d.ReleaseDate = null;
                    var result = _contentService.SaveAndPublishWithStatus(d, (int)d.GetWriterProfile(_userService).Id);
                    if (result.Success == false)
                    {
                        if (result.Exception != null)
                        {
                            LogHelper.Error<ScheduledPublisher>("Could not published the document (" + d.Id + ") based on it's scheduled release, status result: " + result.Result.StatusType, result.Exception);
                        }
                        else
                        {                            
                            LogHelper.Warn<ScheduledPublisher>("Could not published the document (" + d.Id + ") based on it's scheduled release. Status result: " + result.Result.StatusType);
                        }
                    }
                }
                catch (Exception ee)
                {
                    LogHelper.Error<ScheduledPublisher>(string.Format("Error publishing node {0}", d.Id), ee);
                    throw;
                }
            }
            foreach (var d in _contentService.GetContentForExpiration())
            {
                try
                {
                    d.ExpireDate = null;
                    _contentService.UnPublish(d, (int)d.GetWriterProfile(_userService).Id);
                }
                catch (Exception ee)
                {
                    LogHelper.Error<ScheduledPublisher>(string.Format("Error unpublishing node {0}", d.Id), ee);
                    throw;
                }
            }
        }
    }
}