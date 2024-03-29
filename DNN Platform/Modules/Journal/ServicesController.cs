#region Copyright
// 
// DotNetNukeŽ - http://www.dotnetnuke.com
// Copyright (c) 2002-2018
// by DotNetNuke Corporation
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated 
// documentation files (the "Software"), to deal in the Software without restriction, including without limitation 
// the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and 
// to permit persons to whom the Software is furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all copies or substantial portions 
// of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED 
// TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL 
// THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF 
// CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER 
// DEALINGS IN THE SOFTWARE.
#endregion
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Http;

using DotNetNuke.Common;
using DotNetNuke.Common.Utilities;
using DotNetNuke.Entities.Users;
using DotNetNuke.Entities.Users.Social;
using DotNetNuke.Instrumentation;
using DotNetNuke.Modules.Journal.Components;
using DotNetNuke.Security;
using DotNetNuke.Security.Roles;
using DotNetNuke.Services.FileSystem;
using DotNetNuke.Services.Journal;
using DotNetNuke.Services.Journal.Internal;
using DotNetNuke.Services.Social.Notifications;
using DotNetNuke.Web.Api;

namespace DotNetNuke.Modules.Journal
{
    [DnnModuleAuthorize(AccessLevel = SecurityAccessLevel.View)]
    [SupportedModules("Journal")]
    public class ServicesController : DnnApiController
    {
    	private static readonly ILog Logger = LoggerSource.Instance.GetLogger(typeof (ServicesController));

        private const int MentionNotificationLength = 100;
        private const string MentionNotificationSuffix = "...";
        private const string MentionIdentityChar = "@";

        private static readonly string [] AcceptedFileExtensions = { "jpg", "png", "gif", "jpe", "jpeg", "tiff", "bmp" };

        #region Public Methods
        public class CreateDTO
        {
            public string Text { get; set; }
            public int ProfileId { get; set; }
            public string JournalType { get; set; }
            public string ItemData { get; set; }
            public string SecuritySet { get; set; }
            public int GroupId { get; set; }
            public IList<MentionDTO> Mentions { get; set; }
        }

        public class MentionDTO
        {
            public string DisplayName { get; set; }
            public int UserId { get; set; }
        }

        private static bool IsImageFile(string relativePath)
        {
            if (relativePath == null)
            {
                return false;
            }

            if (relativePath.Contains("?"))
	        {
		        relativePath = relativePath.Substring(0,
			        relativePath.IndexOf("?", StringComparison.InvariantCultureIgnoreCase));
	        }

            
            var extension = relativePath.Substring(relativePath.LastIndexOf(".",
            StringComparison.Ordinal) + 1).ToLowerInvariant();
            return AcceptedFileExtensions.Contains(extension);
        }

        private static bool IsAllowedLink(string url)
        {
            return !string.IsNullOrEmpty(url) && !url.Contains("//");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
		[DnnAuthorize(DenyRoles = "Unverified Users")]
        public HttpResponseMessage Create(CreateDTO postData)
        {
            try
            {
                int userId = UserInfo.UserID;
                IDictionary<string, UserInfo> mentionedUsers = new Dictionary<string, UserInfo>();

                if (postData.ProfileId == -1)
                {
                    postData.ProfileId = userId;
                }

                checkProfileAccess(postData.ProfileId, UserInfo);

                checkGroupAccess(postData);

                var journalItem = prepareJournalItem(postData, mentionedUsers);

                JournalController.Instance.SaveJournalItem(journalItem, ActiveModule);

                var originalSummary = journalItem.Summary;
                SendMentionNotifications(mentionedUsers, journalItem, originalSummary);

                return Request.CreateResponse(HttpStatusCode.OK, journalItem);
            }
            catch (Exception exc)
            {
                Logger.Error(exc);
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, exc);
            }
        }

        // Check if a user can post content on a specific profile's page
        private void checkProfileAccess(int profileId, UserInfo currentUser)
        {
            if (profileId != currentUser.UserID)
            {
                var profileUser = UserController.Instance.GetUser(PortalSettings.PortalId, profileId);
                if (profileUser == null || (!UserInfo.IsInRole(PortalSettings.AdministratorRoleName) && !Utilities.AreFriends(profileUser, currentUser)))
                {
                    throw new ArgumentException("you have no permission to post journal on current profile page.");
                }
            }
        }

        private void checkGroupAccess(CreateDTO postData)
        {
            if (postData.GroupId > 0)
            {
                postData.ProfileId = -1;

                RoleInfo roleInfo = RoleController.Instance.GetRoleById(ActiveModule.OwnerPortalID, postData.GroupId);
                if (roleInfo != null)
                {
                    if (!UserInfo.IsInRole(PortalSettings.AdministratorRoleName) && !UserInfo.IsInRole(roleInfo.RoleName))
                    {
                        throw new ArgumentException("you have no permission to post journal on current group.");
                    }

                    if (!roleInfo.IsPublic)
                    {
                        postData.SecuritySet = "R";
                    }
                }
            }
        }

        private JournalItem prepareJournalItem(CreateDTO postData, IDictionary<string, UserInfo> mentionedUsers)
        {
            var journalTypeId = 1;
            switch (postData.JournalType)
            {
                case "link":
                    journalTypeId = 2;
                    break;
                case "photo":
                    journalTypeId = 3;
                    break;
                case "file":
                    journalTypeId = 4;
                    break;
            }

            var ji = new JournalItem
            {
                JournalId = -1,
                JournalTypeId = journalTypeId,
                PortalId = ActiveModule.OwnerPortalID,
                UserId = UserInfo.UserID,
                SocialGroupId = postData.GroupId,
                ProfileId = postData.ProfileId,
                Summary = postData.Text ?? "",
                SecuritySet = postData.SecuritySet
            };
            ji.Title = HttpUtility.HtmlDecode(HttpUtility.UrlDecode(ji.Title));
            ji.Summary = HttpUtility.HtmlDecode(HttpUtility.UrlDecode(ji.Summary));

            var ps = PortalSecurity.Instance;

            ji.Title = ps.InputFilter(ji.Title, PortalSecurity.FilterFlag.NoScripting);
            ji.Title = Utilities.RemoveHTML(ji.Title);
            ji.Title = ps.InputFilter(ji.Title, PortalSecurity.FilterFlag.NoMarkup);

            ji.Summary = ps.InputFilter(ji.Summary, PortalSecurity.FilterFlag.NoScripting);
            ji.Summary = Utilities.RemoveHTML(ji.Summary);
            ji.Summary = ps.InputFilter(ji.Summary, PortalSecurity.FilterFlag.NoMarkup);

            //parse the mentions context in post data
            var originalSummary = ji.Summary;

            ji.Summary = ParseMentions(ji.Summary, postData.Mentions, ref mentionedUsers);

            if (ji.Summary.Length > 2000)
            {
                ji.Body = ji.Summary;
                ji.Summary = null;
            }

            if (!string.IsNullOrEmpty(postData.ItemData))
            {
                ji.ItemData = postData.ItemData.FromJson<ItemData>();
                var originalImageUrl = ji.ItemData.ImageUrl;
                if (!IsImageFile(ji.ItemData.ImageUrl))
                {
                    ji.ItemData.ImageUrl = string.Empty;
                }

                ji.ItemData.Description = HttpUtility.UrlDecode(ji.ItemData.Description);

                if (!IsAllowedLink(ji.ItemData.Url))
                {
                    ji.ItemData.Url = string.Empty;
                }

                if (!string.IsNullOrEmpty(ji.ItemData.Url) && ji.ItemData.Url.StartsWith("fileid="))
                {
                    var fileId = Convert.ToInt32(ji.ItemData.Url.Replace("fileid=", string.Empty).Trim());
                    var file = FileManager.Instance.GetFile(fileId);

                    if (!IsCurrentUserFile(file))
                    {
                        throw new ArgumentException("you have no permission to attach files not belongs to you.");
                    }

                    ji.ItemData.Title = file.FileName;
                    ji.ItemData.Url = Globals.LinkClick(ji.ItemData.Url, Null.NullInteger, Null.NullInteger);

                    if (string.IsNullOrEmpty(ji.ItemData.ImageUrl) &&
                        originalImageUrl.ToLowerInvariant().StartsWith("/linkclick.aspx?") &&
                        AcceptedFileExtensions.Contains(file.Extension.ToLowerInvariant()))
                    {
                        ji.ItemData.ImageUrl = originalImageUrl;
                    }
                }
            }

            return ji;
        }

        public class JournalIdDTO
        {
            public int JournalId { get; set; }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [DnnAuthorize(DenyRoles = "Unverified Users")]
        public HttpResponseMessage Delete(JournalIdDTO postData)
        {
            try
            {
                var jc = JournalController.Instance;
                var ji = jc.GetJournalItem(ActiveModule.OwnerPortalID, UserInfo.UserID, postData.JournalId);

                if (ji == null)
                {
                    return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "invalid request");
                }

                if (ji.UserId == UserInfo.UserID || ji.ProfileId == UserInfo.UserID || UserInfo.IsInRole(PortalSettings.AdministratorRoleName))
                {
                    jc.DeleteJournalItem(PortalSettings.PortalId, UserInfo.UserID, postData.JournalId);
                    return Request.CreateResponse(HttpStatusCode.OK, new { Result = "success" });
                }

                return Request.CreateErrorResponse(HttpStatusCode.Unauthorized, "access denied");
            }
            catch (Exception exc)
            {
                Logger.Error(exc);
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, exc);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [DnnAuthorize(DenyRoles = "Unverified Users")]
        public HttpResponseMessage SoftDelete(JournalIdDTO postData)
        {
            try
            {
                var jc = JournalController.Instance;
                var ji = jc.GetJournalItem(ActiveModule.OwnerPortalID, UserInfo.UserID, postData.JournalId);

                if (ji == null)
                {
                    return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "invalid request");
                }

                if (ji.UserId == UserInfo.UserID || ji.ProfileId == UserInfo.UserID || UserInfo.IsInRole(PortalSettings.AdministratorRoleName))
                {
                    jc.SoftDeleteJournalItem(PortalSettings.PortalId, UserInfo.UserID, postData.JournalId);
                    return Request.CreateResponse(HttpStatusCode.OK, new {Result = "success"});
                }

                return Request.CreateErrorResponse(HttpStatusCode.Unauthorized, "access denied");
            }
            catch (Exception exc)
            {
                Logger.Error(exc);
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, exc);
            }
        }

        
        public class PreviewDTO
        {
            public string Url { get; set; }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
		[DnnAuthorize]
        public HttpResponseMessage PreviewUrl(PreviewDTO postData)
        {
            try
            {
                var link = Utilities.GetLinkData(postData.Url);
                return Request.CreateResponse(HttpStatusCode.OK, link);
            }
            catch (Exception exc)
            {
                Logger.Error(exc);
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, exc);
            }
        }
        
        public class GetListForProfileDTO
        {
            public int ProfileId { get; set; }
            public int GroupId { get; set; }
            public int RowIndex { get; set; }
            public int MaxRows { get; set; }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public HttpResponseMessage GetListForProfile(GetListForProfileDTO postData)
        {
            try
            {
                
                var jp = new JournalParser(PortalSettings, ActiveModule.ModuleID, postData.ProfileId, postData.GroupId, UserInfo);
                return Request.CreateResponse(HttpStatusCode.OK, jp.GetList(postData.RowIndex, postData.MaxRows), "text/html");
            }
            catch (Exception exc)
            {
                Logger.Error(exc);
                throw new HttpException(500, exc.Message);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [DnnAuthorize(DenyRoles = "Unverified Users")]
        public HttpResponseMessage Like(JournalIdDTO postData)
        {
            try
            {
                JournalController.Instance.LikeJournalItem(postData.JournalId, UserInfo.UserID, UserInfo.DisplayName);
                var ji = JournalController.Instance.GetJournalItem(ActiveModule.OwnerPortalID, UserInfo.UserID, postData.JournalId);
                var jp = new JournalParser(PortalSettings, ActiveModule.ModuleID, ji.ProfileId, -1, UserInfo);
                var isLiked = false;
                var likeList = jp.GetLikeListHTML(ji, ref isLiked);
                likeList = Utilities.LocalizeControl(likeList);
                return Request.CreateResponse(HttpStatusCode.OK, new { LikeList = likeList, Liked = isLiked });
            }
            catch (Exception exc)
            {
                Logger.Error(exc);
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, exc);
            }
        }

        public class CommentSaveDTO
        {
            public int JournalId { get; set; }
            public string Comment { get; set; }
            public IList<MentionDTO> Mentions { get; set; } 
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [DnnAuthorize(DenyRoles = "Unverified Users")]
        public HttpResponseMessage CommentSave(CommentSaveDTO postData)
        {
            try
            {
                var comment = Utilities.RemoveHTML(HttpUtility.UrlDecode(postData.Comment));

                IDictionary<string, UserInfo> mentionedUsers = new Dictionary<string, UserInfo>();
                var originalComment = comment;
                comment = ParseMentions(comment, postData.Mentions, ref mentionedUsers);
                var ci = new CommentInfo { JournalId = postData.JournalId, Comment = comment };
                ci.UserId = UserInfo.UserID;
                ci.DisplayName = UserInfo.DisplayName;
                JournalController.Instance.SaveComment(ci);

                var ji = JournalController.Instance.GetJournalItem(ActiveModule.OwnerPortalID, UserInfo.UserID, postData.JournalId);
                var jp = new JournalParser(PortalSettings, ActiveModule.ModuleID, ji.ProfileId, -1, UserInfo);

                SendMentionNotifications(mentionedUsers, ji, originalComment, "Comment");

                return Request.CreateResponse(HttpStatusCode.OK, jp.GetCommentRow(ji, ci), "text/html");
            }
            catch (Exception exc)
            {
                Logger.Error(exc);
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, exc);
            }
        }

        public class CommentDeleteDTO
        {
            public int JournalId { get; set; }
            public int CommentId { get; set; }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [DnnAuthorize(DenyRoles = "Unverified Users")]
        public HttpResponseMessage CommentDelete(CommentDeleteDTO postData)
        {
            try
            {
                var ci = JournalController.Instance.GetComment(postData.CommentId);
                if (ci == null)
                {
                    return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "delete failed");
                }

                var ji = JournalController.Instance.GetJournalItem(ActiveModule.OwnerPortalID, UserInfo.UserID, postData.JournalId);

                if (ji == null)
                {
                    return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "invalid request");
                }

                if (ci.UserId == UserInfo.UserID || ji.UserId == UserInfo.UserID || UserInfo.IsInRole(PortalSettings.AdministratorRoleName))
                {
                    JournalController.Instance.DeleteComment(postData.JournalId, postData.CommentId);
                    return Request.CreateResponse(HttpStatusCode.OK, new { Result = "success" });
                }

                return Request.CreateErrorResponse(HttpStatusCode.Unauthorized, "access denied");
            }
            catch (Exception exc)
            {
                Logger.Error(exc);
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, exc);
            }
        }

        public class SuggestDTO
        {
            public string displayName { get; set; }
            public int userId { get; set; }
            public string avatar { get; set; }
            public string key { get; set; }
        }

		[HttpGet]
		[DnnAuthorize(DenyRoles = "Unverified Users")]
		public HttpResponseMessage GetSuggestions(string keyword)
		{
			try
			{
                var findedUsers = new List<SuggestDTO>();
				var relations = RelationshipController.Instance.GetUserRelationships(UserInfo);
				foreach (var ur in relations)
				{
					var targetUserId = ur.UserId == UserInfo.UserID ? ur.RelatedUserId : ur.UserId;
					var targetUser = UserController.GetUserById(PortalSettings.PortalId, targetUserId);
					var relationship = RelationshipController.Instance.GetRelationship(ur.RelationshipId);
					if (ur.Status == RelationshipStatus.Accepted && targetUser != null
						&& ((relationship.RelationshipTypeId == (int)DefaultRelationshipTypes.Followers && ur.RelatedUserId == UserInfo.UserID)
								|| relationship.RelationshipTypeId == (int)DefaultRelationshipTypes.Friends
							)
						&& (targetUser.DisplayName.ToLowerInvariant().Contains(keyword.ToLowerInvariant())
                                || targetUser.DisplayName.ToLowerInvariant().Contains(keyword.Replace("-", " ").ToLowerInvariant())
							)
                        && findedUsers.All(s => s.userId != targetUser.UserID)
						)
					{
						findedUsers.Add(new SuggestDTO
							                {
                                                displayName = targetUser.DisplayName.Replace(" ", "-"),
											    userId = targetUser.UserID,
											    avatar = targetUser.Profile.PhotoURL,
                                                key = keyword
							                });
					}
				}

				return Request.CreateResponse(HttpStatusCode.OK, findedUsers.Cast<object>().Take(5));
			}
			catch (Exception exc)
			{
				Logger.Error(exc);
				return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, exc);
			}
        }

        #endregion

        #region Private Methods
        private string ParseMentions(string content, IList<MentionDTO> mentions, ref IDictionary<string, UserInfo> mentionedUsers)
        {
            if (mentions == null || mentions.Count == 0)
            {
                return content;
            }

            foreach (var mention in mentions)
            {
                var user = UserController.GetUserById(PortalSettings.PortalId, mention.UserId);

                if (user != null)
                {
                    var relationship = RelationshipController.Instance.GetFollowingRelationship(UserInfo, user) ??
                                       RelationshipController.Instance.GetFriendRelationship(UserInfo, user);
                    if (relationship != null && relationship.Status == RelationshipStatus.Accepted)
                    {
                        var userLink = string.Format("<a href=\"{0}\" class=\"userLink\" target=\"_blank\">{1}</a>",
                                                     Globals.UserProfileURL(user.UserID),
                                                     MentionIdentityChar + user.DisplayName);
                        content = content.Replace(MentionIdentityChar + mention.DisplayName, userLink);

                        mentionedUsers.Add(mention.DisplayName, user);
                    }
                }
            }
            return content;
        }

        private void SendMentionNotifications(IDictionary<string, UserInfo> mentionedUsers, JournalItem item, string originalSummary, string type = "Post")
        {
            //send notification to the mention users
            var subjectTemplate = Utilities.GetSharedResource("Notification_Mention.Subject");
            var bodyTemplate = Utilities.GetSharedResource("Notification_Mention.Body");
            var mentionType = Utilities.GetSharedResource("Notification_MentionType_" + type);
            var notificationType = DotNetNuke.Services.Social.Notifications.NotificationsController.Instance.GetNotificationType("JournalMention");

            foreach (var key in mentionedUsers.Keys)
            {
                var mentionUser = mentionedUsers[key];
                var mentionText = originalSummary.Substring(originalSummary.IndexOf(MentionIdentityChar + key, StringComparison.InvariantCultureIgnoreCase));
                if (mentionText.Length > MentionNotificationLength)
                {
                    mentionText = mentionText.Substring(0, MentionNotificationLength) + MentionNotificationSuffix;
                }
                var notification = new Notification
                {
                    Subject = string.Format(subjectTemplate, UserInfo.DisplayName, mentionType),
                    Body = string.Format(bodyTemplate, mentionText),
                    NotificationTypeID = notificationType.NotificationTypeId,
                    SenderUserID = UserInfo.UserID,
                    IncludeDismissAction = true,
                    Context = string.Format("{0}_{1}", UserInfo.UserID, item.JournalId)
                };

                Services.Social.Notifications.NotificationsController.Instance.SendNotification(notification, PortalSettings.PortalId, null, new List<UserInfo> { mentionUser });
            }
        }

        private bool IsCurrentUserFile(IFileInfo file)
        {
            if (file == null)
            {
                return false;
            }

            var userFolders = GetUserFolders();

            return userFolders.Any(f => file.FolderId == f.FolderID);
        }

        private IList<IFolderInfo> GetUserFolders()
        {
            var folders = new List<IFolderInfo>();

            var userFolder = FolderManager.Instance.GetUserFolder(UserInfo);
            folders.Add(userFolder);
            folders.AddRange(GetSubFolders(userFolder));

            return folders;
        }

        private IList<IFolderInfo> GetSubFolders(IFolderInfo parentFolder)
        {
            var folders = new List<IFolderInfo>();
            foreach (var folder in FolderManager.Instance.GetFolders(parentFolder))
            {
                folders.Add(folder);
                folders.AddRange(GetSubFolders(folder));
            }

            return folders;
        }

        #endregion
    }
}