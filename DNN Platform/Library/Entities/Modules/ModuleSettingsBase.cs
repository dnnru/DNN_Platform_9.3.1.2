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
#region Usings

using System.Collections;
using System.ComponentModel;

using DotNetNuke.UI.Modules;

#endregion

namespace DotNetNuke.Entities.Modules
{
    public class ModuleSettingsBase : PortalModuleBase, ISettingsControl
    {
        public Hashtable ModuleSettings
        {
            get
            {
                return ModuleContext.Configuration.ModuleSettings;
            }
        }

        public Hashtable TabModuleSettings
        {
            get
            {
                return ModuleContext.Configuration.TabModuleSettings;
            }
        }

        #region ISettingsControl Members

        public virtual void LoadSettings()
        {
        }

        public virtual void UpdateSettings()
        {
        }

        #endregion
    }
}