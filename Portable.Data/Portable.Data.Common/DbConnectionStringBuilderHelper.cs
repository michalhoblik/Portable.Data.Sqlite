//
// System.Data.Common.DbConnectionStringBuilderHelper.cs
//
// Author:
//   Sureshkumar T (tsureshkumar@novell.com)
//
// Copyright (C) 2004 Novell, Inc (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;

//Was: namespace  System.Data.Common
namespace Portable.Data.Common
{
    using System.Globalization;

    internal sealed class DbConnectionStringBuilderHelper
    {
        #region Methods

        public static int ConvertToInt32(object value)
        {
            return Int32.Parse(value.ToString(), CultureInfo.InvariantCulture);
        }

        public static bool ConvertToBoolean(object value)
        {
            if (value == null)
            {
                throw new ArgumentNullException("value", "null value cannot be converted to boolean");
            }
            string upper = value.ToString().ToUpper().Trim();
            if (upper == "YES" || upper == "TRUE")
            {
                return true;
            }
            if (upper == "NO" || upper == "FALSE")
            {
                return false;
            }
            throw new ArgumentException(String.Format("Invalid boolean value: {0}", value));
        }

        #endregion // Methods
    }
}
