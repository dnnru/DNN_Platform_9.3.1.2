﻿#region Copyright
// 
// DotNetNuke® - http://www.dotnetnuke.com
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
using System.Collections.Generic;
using System.Data;
using System.Threading;
using DotNetNuke.ComponentModel;
using DotNetNuke.Data;
using DotNetNuke.Entities.Controllers;
using DotNetNuke.Entities.Users;
using DotNetNuke.Services.Cache;
using DotNetNuke.Services.Localization;
using DotNetNuke.Services.Search.Entities;
using DotNetNuke.Services.Search.Internals;
using DotNetNuke.Tests.Utilities.Mocks;

using Moq;

using NUnit.Framework;
using System.IO;

namespace DotNetNuke.Tests.Core.Controllers.Search
{
    /// <summary>
    ///  Testing various aspects of SearchController
    /// </summary>
    [TestFixture]
    public class InternalSearchControllerTests
    {

        #region Constants

        public enum SearchTypeIds
        {
            ModuleSearchTypeId = 1,
            TabSearchTypeId,
            DocumentSearchTypeId,
            UrlSearchTypeId,
            OtherSearchTypeId,
            UnknownSearchTypeId
        }

        private const int ModuleSearchTypeId = (int) SearchTypeIds.ModuleSearchTypeId;
        private const int TabSearchTypeId = (int) SearchTypeIds.TabSearchTypeId;
        private const int DocumentSearchTypeId = (int) SearchTypeIds.DocumentSearchTypeId;
        private const int UrlSearchTypeId = (int) SearchTypeIds.UrlSearchTypeId;
        private const int OtherSearchTypeId = (int) SearchTypeIds.OtherSearchTypeId;
        private const int UnknownSearchTypeId = (int) SearchTypeIds.UnknownSearchTypeId;

        private const int PortalId0 = 0;
        private const int PortalId1 = 1;

        private const string TermDNN = "DNN";
        private const string TermDotNetNuke = "DotnetNuke";
        private const string TermLaptop = "Laptop";
        private const string TermNotebook = "Notebook";
        private const string TermJump = "Jump";
        private const string TermLeap = "Leap";
        private const string TermHop = "Hop";

        private const string ModuleSearchTypeName = "module";
        private const string OtherSearchTypeName = "other";
        private const string TabSearchTypeName = "tab";
        private const string DocumentSearchTypeName = "document";
        private const string UrlSearchTypeName = "url";
        private const string ModuleResultControllerClass = "DotNetNuke.Services.Search.Crawlers.ModuleResultController, DotNetNuke";
        private const string FakeResultControllerClass = "DotNetNuke.Tests.Core.Controllers.Search.FakeResultController, DotNetNuke.Tests.Core";

        private const string NoPermissionFakeResultControllerClass =
            "DotNetNuke.Tests.Core.Controllers.Search.NoPermissionFakeResultController, DotNetNuke.Tests.Core";

        private const string CultureEnUs = "en-US";
        private const string CultureEnCa = "en-CA";
        private const string CultureItIt = "it-IT";
        private const string CultureEsEs = "es-ES";
        private const int LanguageIdEnUs = 1;
        private const int LanguageIdEnFr = 2;
        private const int LanguageIdItIt = 3;
        private const int LanguageIdEsEs = 4;


        private const string SearchIndexFolder = @"App_Data\InternalSearchTests";
        private readonly double _readerStaleTimeSpan = TimeSpan.FromMilliseconds(100).TotalSeconds;

        #endregion

        #region Private Properties

        private Mock<IHostController> _mockHostController;
        private Mock<CachingProvider> _mockCachingProvider;
        private Mock<DataProvider> _mockDataProvider;
        private Mock<ILocaleController> _mockLocaleController;
        private Mock<ISearchHelper> _mockSearchHelper;
        private Mock<IUserController> _mockUserController;

        private IInternalSearchController _internalSearchController;
        private LuceneControllerImpl _luceneController;

        #endregion

        #region Set Up

        [SetUp]
        public void SetUp()
        {
            ComponentFactory.Container = new SimpleContainer();
            MockComponentProvider.ResetContainer();

            _mockDataProvider = MockComponentProvider.CreateDataProvider();
            _mockLocaleController = MockComponentProvider.CreateLocaleController();
            _mockCachingProvider = MockComponentProvider.CreateDataCacheProvider();

            _mockUserController = new Mock<IUserController>();
            _mockHostController = new Mock<IHostController>();
            _mockSearchHelper = new Mock<ISearchHelper>();

            SetupDataProvider();
            SetupHostController();
            SetupSearchHelper();
            SetupLocaleController();

            _mockUserController.Setup(c => c.GetUserById(It.IsAny<int>(), It.IsAny<int>()))
                .Returns((int portalId, int userId) => GetUserByIdCallback(portalId, userId));
            UserController.SetTestableInstance(_mockUserController.Object);


            CreateNewLuceneControllerInstance();
        }

        [TearDown]
        public void TearDown()
        {
            _luceneController.Dispose();
            DeleteIndexFolder();
            InternalSearchController.ClearInstance();
            UserController.ClearInstance();
            SearchHelper.ClearInstance();
            LuceneController.ClearInstance();
            _luceneController = null;
        }

        #endregion

        #region Private Methods

        private void CreateNewLuceneControllerInstance()
        {
            DeleteIndexFolder();
            InternalSearchController.SetTestableInstance(new InternalSearchControllerImpl());
            _internalSearchController = InternalSearchController.Instance;

            if (_luceneController != null)
            {
                LuceneController.ClearInstance();
                _luceneController.Dispose();
            }
            _luceneController = new LuceneControllerImpl();
            LuceneController.SetTestableInstance(_luceneController);
        }

        private void SetupHostController()
        {
            _mockHostController.Setup(c => c.GetString(Constants.SearchIndexFolderKey, It.IsAny<string>())).Returns(SearchIndexFolder);
            _mockHostController.Setup(c => c.GetDouble(Constants.SearchReaderRefreshTimeKey, It.IsAny<double>())).Returns(_readerStaleTimeSpan);
            _mockHostController.Setup(c => c.GetInteger(Constants.SearchTitleBoostSetting, It.IsAny<int>())).Returns(Constants.DefaultSearchTitleBoost);
            _mockHostController.Setup(c => c.GetInteger(Constants.SearchTagBoostSetting, It.IsAny<int>())).Returns(Constants.DefaultSearchTagBoost);
            _mockHostController.Setup(c => c.GetInteger(Constants.SearchContentBoostSetting, It.IsAny<int>())).Returns(Constants.DefaultSearchKeywordBoost);
            _mockHostController.Setup(c => c.GetInteger(Constants.SearchDescriptionBoostSetting, It.IsAny<int>()))
                .Returns(Constants.DefaultSearchDescriptionBoost);
            _mockHostController.Setup(c => c.GetInteger(Constants.SearchAuthorBoostSetting, It.IsAny<int>())).Returns(Constants.DefaultSearchAuthorBoost);
            _mockHostController.Setup(c => c.GetInteger(Constants.SearchMinLengthKey, It.IsAny<int>())).Returns(Constants.DefaultMinLen);
            _mockHostController.Setup(c => c.GetInteger(Constants.SearchMaxLengthKey, It.IsAny<int>())).Returns(Constants.DefaultMaxLen);
            HostController.RegisterInstance(_mockHostController.Object);
        }

        private void SetupLocaleController()
        {
            _mockLocaleController.Setup(l => l.GetLocale(It.IsAny<string>())).Returns(new Locale {LanguageId = -1, Code = string.Empty});
            _mockLocaleController.Setup(l => l.GetLocale(CultureEnUs)).Returns(new Locale {LanguageId = LanguageIdEnUs, Code = CultureEnUs});
            _mockLocaleController.Setup(l => l.GetLocale(CultureEnCa)).Returns(new Locale {LanguageId = LanguageIdEnFr, Code = CultureEnCa});
            _mockLocaleController.Setup(l => l.GetLocale(CultureItIt)).Returns(new Locale {LanguageId = LanguageIdItIt, Code = CultureItIt});
            _mockLocaleController.Setup(l => l.GetLocale(CultureEsEs)).Returns(new Locale {LanguageId = LanguageIdEsEs, Code = CultureEsEs});

            _mockLocaleController.Setup(l => l.GetLocale(It.IsAny<int>())).Returns(new Locale {LanguageId = LanguageIdEnUs, Code = CultureEnUs});
            _mockLocaleController.Setup(l => l.GetLocale(LanguageIdEnUs)).Returns(new Locale {LanguageId = LanguageIdEnUs, Code = CultureEnUs});
            _mockLocaleController.Setup(l => l.GetLocale(LanguageIdEnFr)).Returns(new Locale {LanguageId = LanguageIdEnFr, Code = CultureEnCa});
            _mockLocaleController.Setup(l => l.GetLocale(LanguageIdItIt)).Returns(new Locale {LanguageId = LanguageIdItIt, Code = CultureItIt});
            _mockLocaleController.Setup(l => l.GetLocale(LanguageIdEsEs)).Returns(new Locale {LanguageId = LanguageIdEsEs, Code = CultureEsEs});
        }

        private void SetupDataProvider()
        {
            //Standard DataProvider Path for Logging
            _mockDataProvider.Setup(d => d.GetProviderPath()).Returns("");

            DataTableReader searchTypes = null;
            _mockDataProvider.Setup(ds => ds.GetAllSearchTypes())
                .Callback(() => searchTypes = GetAllSearchTypes().CreateDataReader())
                .Returns(() => searchTypes);

            _mockDataProvider.Setup(d => d.GetPortals(It.IsAny<string>())).Returns<string>(GetPortalsCallBack);
        }

        private IDataReader GetPortalsCallBack(string culture)
        {
            return GetPortalCallBack(PortalId0, CultureEnUs);
        }

        private IDataReader GetPortalCallBack(int portalId, string culture)
        {
            var table = new DataTable("Portal");

            var cols = new[]
            {
                "PortalID", "PortalGroupID", "PortalName", "LogoFile", "FooterText", "ExpiryDate", "UserRegistration", "BannerAdvertising", "AdministratorId",
                "Currency", "HostFee",
                "HostSpace", "PageQuota", "UserQuota", "AdministratorRoleId", "RegisteredRoleId", "Description", "KeyWords", "BackgroundFile", "GUID",
                "PaymentProcessor", "ProcessorUserId",
                "ProcessorPassword", "SiteLogHistory", "Email", "DefaultLanguage", "TimezoneOffset", "AdminTabId", "HomeDirectory", "SplashTabId", "HomeTabId",
                "LoginTabId", "RegisterTabId",
                "UserTabId", "SearchTabId", "Custom404TabId", "Custom500TabId", "TermsTabId", "PrivacyTabId", "SuperTabId", "CreatedByUserID", "CreatedOnDate", "LastModifiedByUserID",
                "LastModifiedOnDate", "CultureCode"
            };

            foreach (var col in cols)
            {
                table.Columns.Add(col);
            }

            const int homePage = 1;
            table.Rows.Add(portalId, null, "My Website", "Logo.png", "Copyright 2011 by DotNetNuke Corporation", null,
                "2", "0", "2", "USD", "0", "0", "0", "0", "0", "1", "My Website", "DotNetNuke, DNN, Content, Management, CMS", null,
                "1057AC7A-3C08-4849-A3A6-3D2AB4662020", null, null, null, "0", "admin@change.me", "en-US", "-8", "58", "Portals/0",
                null, homePage.ToString("D"), null, null, "57", "56", "-1", "-1", "7", null, null, "-1", "2011-08-25 07:34:11", "-1", "2011-08-25 07:34:29", culture);

            return table.CreateDataReader();
        }

        private void SetupSearchHelper()
        {
            _mockSearchHelper.Setup(c => c.GetSearchMinMaxLength()).Returns(new Tuple<int, int>(Constants.DefaultMinLen, Constants.DefaultMaxLen));
            _mockSearchHelper.Setup(c => c.GetSynonyms(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns<int, string, string>(GetSynonymsCallBack);
            _mockSearchHelper.Setup(x => x.GetSearchTypeByName(It.IsAny<string>()))
                .Returns((string name) => new SearchType {SearchTypeId = 0, SearchTypeName = name});
            _mockSearchHelper.Setup(x => x.GetSearchTypeByName(It.IsAny<string>())).Returns<string>(GetSearchTypeByNameCallback);
            _mockSearchHelper.Setup(x => x.GetSearchTypes()).Returns(GetSearchTypes());
            _mockSearchHelper.Setup(c => c.GetSynonymsGroups(It.IsAny<int>(), It.IsAny<string>())).Returns(GetSynonymsGroupsCallBack);
            _mockSearchHelper.Setup(x => x.GetSearchStopWords(0, CultureEsEs)).Returns(
                new SearchStopWords
                {
                    PortalId = 0,
                    CultureCode = CultureEsEs,
                    StopWords = "los,de,el",
                });
			_mockSearchHelper.Setup(x => x.RephraseSearchText(It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>()))
                .Returns<string, bool, bool>(new SearchHelperImpl().RephraseSearchText);
            _mockSearchHelper.Setup(x => x.StripTagsNoAttributes(It.IsAny<string>(), It.IsAny<bool>())).Returns((string html, bool retainSpace) => html);
            SearchHelper.SetTestableInstance(_mockSearchHelper.Object);
        }

        private IList<SynonymsGroup> GetSynonymsGroupsCallBack()
        {
            var groups = new List<SynonymsGroup>
            {
                new SynonymsGroup {PortalId = 0, SynonymsGroupId = 1, SynonymsTags = string.Join(",", TermDNN, TermDotNetNuke)},
                new SynonymsGroup {PortalId = 0, SynonymsGroupId = 2, SynonymsTags = string.Join(",", TermLaptop, TermNotebook)},
                new SynonymsGroup {PortalId = 0, SynonymsGroupId = 3, SynonymsTags = string.Join(",", TermJump, TermLeap, TermHop)}
            };

            return groups;
        }

        private SearchType GetSearchTypeByNameCallback(string searchTypeName)
        {
            var searchType = new SearchType {SearchTypeName = searchTypeName, SearchTypeId = 0};
            switch (searchTypeName)
            {
                case ModuleSearchTypeName:
                    searchType.SearchTypeId = ModuleSearchTypeId;
                    break;
                case TabSearchTypeName:
                    searchType.SearchTypeId = TabSearchTypeId;
                    break;
                case OtherSearchTypeName:
                    searchType.SearchTypeId = OtherSearchTypeId;
                    break;
                case DocumentSearchTypeName:
                    searchType.SearchTypeId = DocumentSearchTypeId;
                    break;
                case UrlSearchTypeName:
                    searchType.SearchTypeId = UrlSearchTypeId;
                    break;
            }

            return searchType;
        }

        private IList<string> GetSynonymsCallBack(int portalId, string cultureCode, string term)
        {
            var synonyms = new List<string>();
            if (term == "fox")
                synonyms.Add("wolf");

            return synonyms;
        }

        private UserInfo GetUserByIdCallback(int portalId, int userId)
        {
            return new UserInfo {UserID = userId, DisplayName = "User" + userId, PortalID = portalId};
        }

        private DataTable GetAllSearchTypes()
        {
            var dtSearchTypes = new DataTable("SearchTypes");
            var pkId = dtSearchTypes.Columns.Add("SearchTypeId", typeof (int));
            dtSearchTypes.Columns.Add("SearchTypeName", typeof (string));
            dtSearchTypes.Columns.Add("SearchResultClass", typeof (string));
            dtSearchTypes.PrimaryKey = new[] {pkId};

            //Create default Crawler
            dtSearchTypes.Rows.Add(ModuleSearchTypeId, ModuleSearchTypeName, FakeResultControllerClass);
            dtSearchTypes.Rows.Add(TabSearchTypeId, TabSearchTypeName, FakeResultControllerClass);
            dtSearchTypes.Rows.Add(OtherSearchTypeId, OtherSearchTypeName, FakeResultControllerClass);
            dtSearchTypes.Rows.Add(DocumentSearchTypeId, DocumentSearchTypeName, NoPermissionFakeResultControllerClass);
            dtSearchTypes.Rows.Add(UrlSearchTypeId, UrlSearchTypeName, FakeResultControllerClass);

            return dtSearchTypes;
        }

        private IEnumerable<SearchType> GetSearchTypes()
        {
            var searchTypes = new List<SearchType>
            {
                new SearchType {SearchTypeId = ModuleSearchTypeId, SearchTypeName = ModuleSearchTypeName, SearchResultClass = FakeResultControllerClass},
                new SearchType {SearchTypeId = TabSearchTypeId, SearchTypeName = TabSearchTypeName, SearchResultClass = FakeResultControllerClass},
                new SearchType {SearchTypeId = OtherSearchTypeId, SearchTypeName = OtherSearchTypeName, SearchResultClass = FakeResultControllerClass},
                new SearchType
                {
                    SearchTypeId = DocumentSearchTypeId,
                    SearchTypeName = DocumentSearchTypeName,
                    SearchResultClass = NoPermissionFakeResultControllerClass
                },
                new SearchType {SearchTypeId = UrlSearchTypeId, SearchTypeName = UrlSearchTypeName, SearchResultClass = FakeResultControllerClass}
            };

            return searchTypes;
        }

        private void DeleteIndexFolder()
        {
            try
            {
                if (Directory.Exists(SearchIndexFolder))
                    Directory.Delete(SearchIndexFolder, true);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        #endregion

        #region Add Tests

        [Test]
        public void SearchController_Add_Throws_On_Null_SearchDocument()
        {
            //Arrange            

            //Act, Assert
            Assert.Throws<ArgumentNullException>(() => _internalSearchController.AddSearchDocument(null));
        }

        [Test]
        public void SearchController_Add_Throws_On_Null_Or_Empty_UniqueuKey()
        {
            //Arrange            

            //Act, Assert
            Assert.Throws<ArgumentException>(() => _internalSearchController.AddSearchDocument(new SearchDocument()));
        }

        [Test]
        public void SearchController_Add_Throws_On_Null_OrEmpty_Title()
        {
            //Arrange            

            //Act, Assert
            Assert.Throws<ArgumentOutOfRangeException>(
                () => _internalSearchController.AddSearchDocument(new SearchDocument {UniqueKey = Guid.NewGuid().ToString()}));
        }


        [Test]
        public void SearchController_AddSearchDcoumets_Does_Not_Throw_On_Null_OrEmpty_Title()
        {
            //Arrange            
            var documents = new List<SearchDocument> {new SearchDocument {UniqueKey = Guid.NewGuid().ToString()}};

            //Act, Assert
            _internalSearchController.AddSearchDocuments(documents);
        }

        [Test]
        public void SearchController_AddSearchDcoumets_Does_Not_Throw_On_Empty_Search_Document()
        {
            //Arrange            
            var documents = new List<SearchDocument> {new SearchDocument()};

            //Act, Assert
            _internalSearchController.AddSearchDocuments(documents);
        }

        [Test]
        public void SearchController_Add_Throws_On_Zero_SearchTypeId()
        {
            //Arrange            

            //Act, Assert
            Assert.Throws<ArgumentOutOfRangeException>(
                () => _internalSearchController.AddSearchDocument(new SearchDocument {UniqueKey = Guid.NewGuid().ToString()}));
        }

        [Test]
        public void SearchController_Add_Throws_On_Negative_SearchTypeId()
        {
            //Arrange            

            //Act, Assert
            Assert.Throws<ArgumentOutOfRangeException>(
                () =>
                    _internalSearchController.AddSearchDocument(new SearchDocument {UniqueKey = Guid.NewGuid().ToString(), Title = "title", SearchTypeId = -1}));
        }

        [Test]
        public void SearchController_Add_Throws_On_DateTimeMin_ModifiedTimeUtc()
        {
            //Arrange            

            //Act, Assert
            Assert.Throws<ArgumentException>(
                () => _internalSearchController.AddSearchDocument(new SearchDocument {UniqueKey = Guid.NewGuid().ToString(), Title = "title", SearchTypeId = 1}));
        }

        #endregion

        #region Delete Tests

        [Test]
        public void SearchController_Add_Then_Delete_ModuleDefinition_WorksAsExpected()
        {
            //Arrange
            const int totalDocs = 10;
            var now = DateTime.UtcNow;

            //Act
            for (var i = 1; i <= totalDocs; i++)
            {
                var doc = new SearchDocument
                {
                    ModuleDefId = i,
                    ModuleId = 100,
                    SearchTypeId = ModuleSearchTypeId,
                    PortalId = PortalId0,
                    UniqueKey = Guid.NewGuid().ToString(),
                    ModifiedTimeUtc = now,
                };

                _internalSearchController.AddSearchDocument(doc);
            }

            //Assert
            var stats = GetSearchStatistics();
            Assert.AreEqual(totalDocs, stats.TotalActiveDocuments);

            //Act - delete last item
            var searchDoc = new SearchDocument {ModuleDefId = totalDocs};
            _internalSearchController.DeleteSearchDocument(searchDoc);

            //Assert
            stats = GetSearchStatistics();
            Assert.AreEqual(totalDocs - 1, stats.TotalActiveDocuments);
            Assert.AreEqual(1, stats.TotalDeletedDocuments);

            //Act - delete first item
            searchDoc = new SearchDocument {ModuleDefId = 1};
            _internalSearchController.DeleteSearchDocument(searchDoc);

            //Assert
            stats = GetSearchStatistics();
            Assert.AreEqual(totalDocs - 2, stats.TotalActiveDocuments);
            Assert.AreEqual(2, stats.TotalDeletedDocuments);
        }

        [Test]
        public void SearchController_Add_Then_Delete_Module_WorksAsExpected()
        {
            //Arrange
            const int totalDocs = 10;
            var now = DateTime.UtcNow;

            //Act
            for (var i = 1; i <= totalDocs; i++)
            {
                var doc = new SearchDocument
                {
                    ModuleId = i,
                    ModuleDefId = 10,
                    PortalId = PortalId0,
                    UniqueKey = Guid.NewGuid().ToString(),
                    SearchTypeId = ModuleSearchTypeId,
                    ModifiedTimeUtc = now,
                };

                _internalSearchController.AddSearchDocument(doc);
            }

            //Assert
            var stats = GetSearchStatistics();
            Assert.AreEqual(totalDocs, stats.TotalActiveDocuments);

            //Act - delete last item
            var searchDoc = new SearchDocument {ModuleId = totalDocs};
            _internalSearchController.DeleteSearchDocument(searchDoc);

            //Assert
            stats = GetSearchStatistics();
            Assert.AreEqual(totalDocs - 1, stats.TotalActiveDocuments);
            Assert.AreEqual(1, stats.TotalDeletedDocuments);

            //Act - delete first item
            searchDoc = new SearchDocument {ModuleId = 1};
            _internalSearchController.DeleteSearchDocument(searchDoc);

            //Assert
            stats = GetSearchStatistics();
            Assert.AreEqual(totalDocs - 2, stats.TotalActiveDocuments);
            Assert.AreEqual(2, stats.TotalDeletedDocuments);
        }

        [Test]
        public void SearchController_Add_Then_Delete_Portals_WorksAsExpected()
        {
            //Arrange
            const int totalDocs = 10; // must be even
            var now = DateTime.UtcNow;

            //Act
            for (var i = 1; i <= totalDocs; i++)
            {
                var doc = new SearchDocument
                {
                    PortalId = i <= (totalDocs/2) ? PortalId0 : PortalId1,
                    UniqueKey = Guid.NewGuid().ToString(),
                    SearchTypeId = ModuleSearchTypeId,
                    ModifiedTimeUtc = now,
                    ModuleId = 100,
                    ModuleDefId = 10,
                };

                _internalSearchController.AddSearchDocument(doc);
            }

            //Assert
            var stats = GetSearchStatistics();
            Assert.AreEqual(totalDocs, stats.TotalActiveDocuments);

            //Act - delete all portal 1 items
            var searchDoc = new SearchDocument {PortalId = PortalId1};
            _internalSearchController.DeleteSearchDocument(searchDoc);

            //Assert - delete all portal 1
            stats = GetSearchStatistics();
            Assert.AreEqual(totalDocs/2, stats.TotalActiveDocuments);
            Assert.AreEqual(totalDocs/2, stats.TotalDeletedDocuments);
        }

        [Test]
        public void SearchController_Add_Then_Delete_Roles_WorksAsExpected()
        {
            //Arrange
            const int totalDocs = 10;
            var now = DateTime.UtcNow;

            //Act
            for (var i = 1; i <= totalDocs; i++)
            {
                var doc = new SearchDocument
                {
                    RoleId = i,
                    PortalId = PortalId0,
                    UniqueKey = Guid.NewGuid().ToString(),
                    SearchTypeId = ModuleSearchTypeId,
                    ModifiedTimeUtc = now,
                    ModuleId = 100,
                    ModuleDefId = 10,
                };

                _internalSearchController.AddSearchDocument(doc);
            }

            //Assert
            var stats = GetSearchStatistics();
            Assert.AreEqual(totalDocs, stats.TotalActiveDocuments);

            //Act - delete last item
            var searchDoc = new SearchDocument {RoleId = totalDocs};
            _internalSearchController.DeleteSearchDocument(searchDoc);

            //Assert
            stats = GetSearchStatistics();
            Assert.AreEqual(totalDocs - 1, stats.TotalActiveDocuments);
            Assert.AreEqual(1, stats.TotalDeletedDocuments);

            //Act - delete first item
            searchDoc = new SearchDocument {RoleId = 1};
            _internalSearchController.DeleteSearchDocument(searchDoc);

            //Assert
            stats = GetSearchStatistics();
            Assert.AreEqual(totalDocs - 2, stats.TotalActiveDocuments);
            Assert.AreEqual(2, stats.TotalDeletedDocuments);
        }

        [Test]
        public void SearchController_Add_Then_Delete_Tabs_WorksAsExpected()
        {
            //Arrange
            const int totalDocs = 10;
            var now = DateTime.UtcNow;

            //Act
            for (var i = 1; i <= totalDocs; i++)
            {
                var doc = new SearchDocument
                {
                    TabId = i,
                    PortalId = PortalId0,
                    UniqueKey = Guid.NewGuid().ToString(),
                    SearchTypeId = TabSearchTypeId,
                    ModifiedTimeUtc = now,
                };

                _internalSearchController.AddSearchDocument(doc);
            }

            //Assert
            var stats = GetSearchStatistics();
            Assert.AreEqual(totalDocs, stats.TotalActiveDocuments);

            //Act - delete last item
            var searchDoc = new SearchDocument {TabId = totalDocs};
            _internalSearchController.DeleteSearchDocument(searchDoc);

            //Assert
            stats = GetSearchStatistics();
            Assert.AreEqual(totalDocs - 1, stats.TotalActiveDocuments);
            Assert.AreEqual(1, stats.TotalDeletedDocuments);

            //Act - delete first item
            searchDoc = new SearchDocument {TabId = 1};
            _internalSearchController.DeleteSearchDocument(searchDoc);

            //Assert
            stats = GetSearchStatistics();
            Assert.AreEqual(totalDocs - 2, stats.TotalActiveDocuments);
            Assert.AreEqual(2, stats.TotalDeletedDocuments);
        }

        [Test]
        public void SearchController_Add_Then_Delete_Users_WorksAsExpected()
        {
            //Arrange
            const int totalDocs = 10;
            var now = DateTime.UtcNow;

            //Act
            for (var i = 1; i <= totalDocs; i++)
            {
                var doc = new SearchDocument
                {
                    AuthorUserId = i,
                    PortalId = PortalId0,
                    UniqueKey = Guid.NewGuid().ToString(),
                    SearchTypeId = ModuleSearchTypeId,
                    ModifiedTimeUtc = now,
                    ModuleId = 100,
                    ModuleDefId = 10,
                };

                _internalSearchController.AddSearchDocument(doc);
            }

            //Assert
            var stats = GetSearchStatistics();
            Assert.AreEqual(totalDocs, stats.TotalActiveDocuments);

            //Act - delete last item
            var searchDoc = new SearchDocument {AuthorUserId = totalDocs};
            _internalSearchController.DeleteSearchDocument(searchDoc);

            //Assert
            stats = GetSearchStatistics();
            Assert.AreEqual(totalDocs - 1, stats.TotalActiveDocuments);
            Assert.AreEqual(1, stats.TotalDeletedDocuments);

            //Act - delete first item
            searchDoc = new SearchDocument {AuthorUserId = 1};
            _internalSearchController.DeleteSearchDocument(searchDoc);

            //Assert
            stats = GetSearchStatistics();
            Assert.AreEqual(totalDocs - 2, stats.TotalActiveDocuments);
            Assert.AreEqual(2, stats.TotalDeletedDocuments);
        }

        private SearchStatistics GetSearchStatistics()
        {
            _internalSearchController.Commit();
            Thread.Sleep((int)(_readerStaleTimeSpan * 1000)); // time to flush data to Lucene
            return _internalSearchController.GetSearchStatistics();
        }

#if false // the rules have changed and these are invalid tests now
        [Test]
        public void SearchController_Delete_Throws_On_Null_Or_Empty_UniqueuKey()
        {
            //Arrange            

            //Act, Assert
            var searchDoc = new SearchDocument() { UniqueKey = null, PortalId = 0, SearchTypeId = 1 };
            Assert.Throws<ArgumentException>(() => _internalSearchController.DeleteSearchDocument(searchDoc));
        }

        [Test]
        public void SearchController_Delete_Throws_On_Zero_SearchTypeId()
        {
            //Arrange            

            //Act, Assert
            var searchDoc = new SearchDocument() { UniqueKey = "key", PortalId = 0, SearchTypeId = 0 };
            Assert.Throws<ArgumentException>(() => _internalSearchController.DeleteSearchDocument(searchDoc));
        }

        [Test]
        public void SearchController_Delete_Throws_On_Negative_SearchTypeId()
        {
            //Arrange            

            //Act, Assert
            var searchDoc = new SearchDocument() { UniqueKey = "key", PortalId = 0, SearchTypeId = -1 };
            Assert.Throws<ArgumentOutOfRangeException>(() => _internalSearchController.DeleteSearchDocument(searchDoc));
        }
#endif

        #endregion

    }
}
