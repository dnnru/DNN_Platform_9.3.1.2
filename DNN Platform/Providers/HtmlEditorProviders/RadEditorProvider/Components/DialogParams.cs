﻿#region Copyright
// 
// DotNetNuke® - http://www.dotnetnuke.com
// Copyright (c) 2002-2017
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
#region Usings

using System;

#endregion

namespace DotNetNuke.Providers.RadEditorProvider
{
    public class DialogParams
    {
        public int PortalId { get; set; }

        public int TabId { get; set; }

        public int ModuleId { get; set; }

        public string HomeDirectory { get; set; }

        public string PortalGuid { get; set; }

        public string LinkUrl { get; set; }

        public string LinkClickUrl { get; set; }

        public bool Track { get; set; }

        public bool TrackUser { get; set; }

        public bool EnableUrlLanguage { get; set; }

        public string DateCreated { get; set; }

        public string Clicks { get; set; }

        public string LastClick { get; set; }

        public string LogStartDate { get; set; }

        public string LogEndDate { get; set; }

        public string TrackingLog { get; set; }

        public string LinkAction { get; set; }
    }
}