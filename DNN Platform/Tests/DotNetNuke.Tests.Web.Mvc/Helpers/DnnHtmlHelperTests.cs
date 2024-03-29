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
using System.Web;
using System.Web.Mvc;
using DotNetNuke.Tests.Web.Mvc.Fakes;
using DotNetNuke.UI.Modules;
using DotNetNuke.Web.Mvc.Framework;
using DotNetNuke.Web.Mvc.Framework.Controllers;
using DotNetNuke.Web.Mvc.Helpers;
using Moq;
using NUnit.Framework;

// ReSharper disable ObjectCreationAsStatement

namespace DotNetNuke.Tests.Web.Mvc.Helpers
{
    [TestFixture]
    public class DnnHtmlHelperTests
    {
        [Test]
        public void Constructor_Throws_On_Null_ViewContext()
        {
            //Arrange
            var viewDataContainer = new Mock<IViewDataContainer>();

            //Act,Assert
            Assert.Throws<ArgumentNullException>(() => new DnnHtmlHelper(null, viewDataContainer.Object));
        }

        [Test]
        public void Strongly_Typed_Constructor_Throws_On_Null_ViewContext()
        {
            //Arrange
            var viewDataContainer = new Mock<IViewDataContainer>();

            //Act,Assert
            Assert.Throws<ArgumentNullException>(() => new DnnHtmlHelper<Dog>(null, viewDataContainer.Object));
        }

        [Test]
        public void Constructor_Throws_On_Null_ViewDataContainer()
        {
            //Act,Assert
            Assert.Throws<ArgumentNullException>(() => new DnnHtmlHelper(new ViewContext(), null));
        }

        [Test]
        public void Strongly_Typed_Constructor_Throws_On_Null_ViewDataContainer()
        {
            //Act,Assert
            Assert.Throws<ArgumentNullException>(() => new DnnHtmlHelper<Dog>(new ViewContext(), null));
        }

        [Test]
        public void Constructor_Throws_On_Null_RouteCollection()
        {
            //Arrange
            var viewDataContainer = new Mock<IViewDataContainer>();

            //Act,Assert
            Assert.Throws<ArgumentNullException>(() => new DnnHtmlHelper(new ViewContext(), viewDataContainer.Object, null));
        }

        [Test]
        public void Strongly_Typed_Constructor_Throws_On_Null_RouteCollection()
        {
            //Arrange
            var viewDataContainer = new Mock<IViewDataContainer>();

            //Act,Assert
            Assert.Throws<ArgumentNullException>(() => new DnnHtmlHelper<Dog>(new ViewContext(), viewDataContainer.Object, null));
        }

        [Test]
        public void Constructor_Throws_On_Invalid_Controller_Property()
        {
            //Arrange
            var viewDataContainer = new Mock<IViewDataContainer>();
            var mockController = new Mock<ControllerBase>();
            var viewContext = new ViewContext {Controller = mockController.Object};

            //Act,Assert
            Assert.Throws<InvalidOperationException>(() => new DnnHtmlHelper(viewContext, viewDataContainer.Object));
        }

        [Test]
        public void Strongly_Typed_Constructor_Throws_On_Invalid_Controller_Property()
        {
            //Arrange
            var mockViewDataContainer = new Mock<IViewDataContainer>();
            mockViewDataContainer.Setup(d => d.ViewData).Returns(new ViewDataDictionary());
            var mockController = new Mock<ControllerBase>();
            var viewContext = new ViewContext {Controller = mockController.Object};

            //Act,Assert
            Assert.Throws<InvalidOperationException>(() => new DnnHtmlHelper<Dog>(viewContext, mockViewDataContainer.Object));
        }

        [Test]
        public void Constructor_Sets_ModuleContext_Property()
        {
            //Arrange
            var viewDataContainer = new Mock<IViewDataContainer>();
            var mockController = new Mock<ControllerBase>();
            var mockDnnController = mockController.As<IDnnController>();
            var expectedContext = new ModuleInstanceContext();
            mockDnnController.Setup(c => c.ModuleContext).Returns(expectedContext);
            var viewContext = new ViewContext {Controller = mockController.Object};

            //Act
            var helper = new DnnHtmlHelper(viewContext, viewDataContainer.Object);

            //Assert
            Assert.AreEqual(expectedContext, helper.ModuleContext);
        }

        [Test]
        public void Strongly_Typed_Constructor_Sets_ModuleContext_Property()
        {
            //Arrange
            var mockViewDataContainer = new Mock<IViewDataContainer>();
            mockViewDataContainer.Setup(d => d.ViewData).Returns(new ViewDataDictionary());
            var mockController = new Mock<ControllerBase>();

            var mockDnnController = mockController.As<IDnnController>();
            var expectedContext = new ModuleInstanceContext();
            mockDnnController.Setup(c => c.ModuleContext).Returns(expectedContext);
            var viewContext = new ViewContext {Controller = mockController.Object};

            //Act
            var helper = new DnnHtmlHelper<Dog>(viewContext, mockViewDataContainer.Object);

            //Assert
            Assert.AreEqual(expectedContext, helper.ModuleContext);
        }

        [Test]
        public void ViewContext_Property_Returns_Passed_in_ViewContext()
        {
            //Arrange
            var mockViewDataContainer = new Mock<IViewDataContainer>();
            mockViewDataContainer.Setup(d => d.ViewData).Returns(new ViewDataDictionary());
            var mockController = new Mock<ControllerBase>();
            var mockDnnController = mockController.As<IDnnController>();
            mockDnnController.Setup(c => c.ModuleContext).Returns(new ModuleInstanceContext());
            var viewContext = new ViewContext {Controller = mockController.Object};

            //Act
            var helper = new DnnHtmlHelper(viewContext, mockViewDataContainer.Object);

            //Assert
            Assert.AreEqual(viewContext, helper.ViewContext);
        }

        [Test]
        public void ViewDataContainer_Property_Returns_Passed_in_ViewDataContainer()
        {
            //Arrange
            var mockViewDataContainer = new Mock<IViewDataContainer>();
            var expectedViewData = new ViewDataDictionary();
            mockViewDataContainer.Setup(d => d.ViewData).Returns(expectedViewData);

            var mockController = new Mock<ControllerBase>();
            var mockDnnController = mockController.As<IDnnController>();
            mockDnnController.Setup(c => c.ModuleContext).Returns(new ModuleInstanceContext());
            var viewContext = new ViewContext {Controller = mockController.Object};

            //Act
            var helper = new DnnHtmlHelper(viewContext, mockViewDataContainer.Object);

            //Assert
            Assert.AreEqual(expectedViewData, helper.ViewData);
        }

        [Test]
        public void Strongly_Typed_ViewDataContainer_Property_Returns_Passed_in_Model()
        {
            //Arrange
            var mockViewDataContainer = new Mock<IViewDataContainer>();
            var expectedViewData = new ViewDataDictionary<Dog>();
            var expectedModel = new Dog();
            expectedViewData.Model = expectedModel;
            mockViewDataContainer.Setup(d => d.ViewData).Returns(expectedViewData);

            var mockController = new Mock<ControllerBase>();
            var mockDnnController = mockController.As<IDnnController>();
            mockDnnController.Setup(c => c.ModuleContext).Returns(new ModuleInstanceContext());
            var viewContext = new ViewContext {Controller = mockController.Object};

            //Act
            var helper = new DnnHtmlHelper<Dog>(viewContext, mockViewDataContainer.Object);

            //Assert
            Assert.AreEqual(expectedModel, helper.ViewData.Model);
        }

        [Test]
        public void EnableClientValidation_Updates_Context()
        {
            //Arrange
            var mockViewPage = new Mock<DnnWebViewPage>() { CallBase = true };
            var mockController = new Mock<ControllerBase>();
            var mockDnnController = mockController.As<IDnnController>();
            var mockViewContext = new Mock<ViewContext>();
            mockViewContext.SetupProperty(x => x.ClientValidationEnabled);
            mockViewContext.Setup(c => c.Controller).Returns(mockController.Object);
            mockViewPage.Object.ViewContext = mockViewContext.Object;
            mockViewPage.Object.InitHelpers();
            
            //Act
            mockViewPage.Object.Html.EnableClientValidation(true);

            //Assert
            Assert.IsTrue(mockViewPage.Object.ViewContext.ClientValidationEnabled);
        }

        [Test]
        public void EnableUnobtrusiveJavaScript_Updates_Context()
        {
            //Arrange
            var mockViewPage = new Mock<DnnWebViewPage>() { CallBase = true };
            var mockController = new Mock<ControllerBase>();
            var mockDnnController = mockController.As<IDnnController>();
            var mockViewContext = new Mock<ViewContext>();
            mockViewContext.SetupProperty(x => x.UnobtrusiveJavaScriptEnabled);
            mockViewContext.Setup(c => c.Controller).Returns(mockController.Object);
            mockViewPage.Object.ViewContext = mockViewContext.Object;
            mockViewPage.Object.InitHelpers();

            //Act
            mockViewPage.Object.Html.EnableUnobtrusiveJavaScript(true);

            //Assert
            Assert.IsTrue(mockViewPage.Object.ViewContext.UnobtrusiveJavaScriptEnabled);
        }
    }
}
