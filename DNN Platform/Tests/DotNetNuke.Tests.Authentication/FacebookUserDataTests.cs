﻿#region Copyright
// 
// DotNetNuke® - http://www.dotnetnuke.com
// Copyright (c) 2002-2013
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

using DotNetNuke.Authentication.Facebook.Components;
using DotNetNuke.Common.Utilities;
using DotNetNuke.Services.Authentication.OAuth;

using NUnit.Framework;

namespace DotNetNuke.Tests.Authentication
{
    [TestFixture]
    public class FacebookUserDataTests
    {
        private const string SampleUserJson = @"{
    ""id"": ""220439"",
    ""name"": ""Bret the Taylor"",
    ""first_name"": ""Bret"",
    ""last_name"": ""Taylor"",
    ""link"": ""https://www.facebook.com/btaylor"",
    ""username"": ""btaylor"",
    ""gender"": ""male"",
    ""locale"": ""en_US""
}";

        [Test]
        public void FacebookUserData_Populates_Inherited_Name_Properties_When_Deserialized()
        {
            // Act
            UserData sampleUser = Json.Deserialize<FacebookUserData>(SampleUserJson);

            // Assert
            Assert.AreEqual("Bret", sampleUser.FirstName, "Should correctly pull first name from first_name field, not by parsing name");
            Assert.AreEqual("Taylor", sampleUser.LastName, "Should correctly pull first name from first_name field, not by parsing name");
        }

        [Test]
        public void FacebookUserData_Populates_Link_Property_When_Deserialized()
        {
            // Act
            var sampleUser = Json.Deserialize<FacebookUserData>(SampleUserJson);

            // Assert
            Assert.AreEqual(new Uri("https://www.facebook.com/btaylor", UriKind.Absolute), sampleUser.Link);
        }
    }
}
