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

using System;
using System.Web.UI;


#endregion

namespace DotNetNuke.Web.UI.WebControls
{
    public class DnnTabCollection : ControlCollection
    {
        public DnnTabCollection(Control owner) : base(owner)
        {
        }

        public new DnnTab this[int index]
        {
            get
            {
                return (DnnTab) base[index];
            }
        }

        public override void Add(Control child)
        {
            if (child is DnnTab)
            {
                base.Add(child);
            }
            else
            {
                throw new ArgumentException("DnnTabCollection must contain controls of type DnnTab");
            }
        }

        public override void AddAt(int index, Control child)
        {
            if (child is DnnTab)
            {
                base.AddAt(index, child);
            }
            else
            {
                throw new ArgumentException("DnnTabCollection must contain controls of type DnnTab");
            }
        }
    }
}