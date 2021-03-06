//
// System.Data.Common.DbConnectionStringBuilder.cs
//
// Author:
//	Sureshkumar T (tsureshkumar@novell.com)
//	Gert Driesen (drieseng@users.sourceforge.net
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
    using System.Collections;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using System.Text;

    public class DbConnectionStringBuilder : IDictionary, ICollection, IEnumerable
    {
        #region Fields

        private readonly Dictionary<string, object> _dictionary;
        private readonly bool useOdbcRules;

        #endregion Fields

        #region Constructors

        public DbConnectionStringBuilder() : this(false)
        {
        }

        public DbConnectionStringBuilder(bool useOdbcRules)
        {
            this.useOdbcRules = useOdbcRules;
            this._dictionary = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        }

        #endregion // Constructors

        #region Properties

        [EditorBrowsable(EditorBrowsableState.Never)]
        public bool BrowsableConnectionString
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }

        public string ConnectionString
        {
            get
            {
                IDictionary<string, object> dictionary = this._dictionary;
                var sb = new StringBuilder();
                foreach (string key in this.Keys)
                {
                    object value = null;
                    if (!dictionary.TryGetValue(key, out value))
                    {
                        continue;
                    }
                    string val = value.ToString();
                    AppendKeyValuePair(sb, key, val, this.useOdbcRules);
                }
                return sb.ToString();
            }
            set
            {
                this.Clear();
                if (value == null)
                {
                    return;
                }
                if (value.Trim().Length == 0)
                {
                    return;
                }
                this.ParseConnectionString(value);
            }
        }

        public virtual object this[string keyword]
        {
            get
            {
                if (this.ContainsKey(keyword))
                {
                    return this._dictionary[keyword];
                }
                else
                {
                    throw new ArgumentException(string.Format(
                        "Keyword '{0}' does not exist",
                        keyword));
                }
            }
            set
            {
                if (value == null)
                {
                    this.Remove(keyword);
                    return;
                }

                if (keyword == null)
                {
                    throw new ArgumentNullException("keyword");
                }

                if (keyword.Length == 0)
                {
                    throw CreateInvalidKeywordException(keyword);
                }

                for (int i = 0; i < keyword.Length; i++)
                {
                    char c = keyword[i];
                    if (i == 0 && (Char.IsWhiteSpace(c) || c == ';'))
                    {
                        throw CreateInvalidKeywordException(keyword);
                    }
                    if (i == (keyword.Length - 1) && Char.IsWhiteSpace(c))
                    {
                        throw CreateInvalidKeywordException(keyword);
                    }
                    if (Char.IsControl(c))
                    {
                        throw CreateInvalidKeywordException(keyword);
                    }
                }

                if (this.ContainsKey(keyword))
                {
                    this._dictionary[keyword] = value;
                }
                else
                {
                    this._dictionary.Add(keyword, value);
                }
            }
        }

        public virtual int Count
        {
            get { return this._dictionary.Count; }
        }

        public virtual bool IsFixedSize
        {
            get { return false; }
        }

        public bool IsReadOnly
        {
            get { throw new NotImplementedException(); }
        }

        public virtual ICollection Keys
        {
            get
            {
                var keys = new string[this._dictionary.Keys.Count];
                (this._dictionary.Keys).CopyTo(keys, 0);
                var keyColl = new ReadOnlyCollection<string>(keys);
                return keyColl;
            }
        }

        bool ICollection.IsSynchronized
        {
            get { throw new NotImplementedException(); }
        }

        object ICollection.SyncRoot
        {
            get { throw new NotImplementedException(); }
        }

        object IDictionary.this[object keyword]
        {
            get { return this[(string) keyword]; }
            set { this[(string) keyword] = value; }
        }

        public virtual ICollection Values
        {
            get
            {
                var values = new object[this._dictionary.Values.Count];
                (this._dictionary.Values).CopyTo(values, 0);
                var valuesColl = new ReadOnlyCollection<object>(values);
                return valuesColl;
            }
        }

        #endregion // Properties

        #region Methods

        public virtual void Clear()
        {
            this._dictionary.Clear();
        }

        void ICollection.CopyTo(Array array, int index)
        {
            if (array == null)
            {
                throw new ArgumentNullException("array");
            }
            var arr = array as KeyValuePair<string, object>[];
            if (arr == null)
            {
                throw new ArgumentException(
                    "Target array type is not compatible with the type of items in the collection");
            }
            ((ICollection<KeyValuePair<string, object>>) this._dictionary).CopyTo(arr, index);
        }

        void IDictionary.Add(object keyword, object value)
        {
            this.Add((string) keyword, value);
        }

        bool IDictionary.Contains(object keyword)
        {
            return this.ContainsKey((string) keyword);
        }

        IDictionaryEnumerator IDictionary.GetEnumerator()
        {
            return this._dictionary.GetEnumerator();
        }

        void IDictionary.Remove(object keyword)
        {
            this.Remove((string) keyword);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this._dictionary.GetEnumerator();
        }

        public void Add(string keyword, object value)
        {
            this[keyword] = value;
        }

        public static void AppendKeyValuePair(StringBuilder builder, string keyword, string value,
                                              bool useOdbcRules)
        {
            if (builder == null)
            {
                throw new ArgumentNullException("builder");
            }
            if (keyword == null)
            {
                throw new ArgumentNullException("keyword");
            }
            if (keyword.Length == 0)
            {
                throw new ArgumentException("Empty keyword is not valid.");
            }

            if (builder.Length > 0)
            {
                builder.Append(';');
            }
            if (!useOdbcRules)
            {
                builder.Append(keyword.Replace("=", "=="));
            }
            else
            {
                builder.Append(keyword);
            }
            builder.Append('=');

            if (string.IsNullOrEmpty(value))
            {
                return;
            }

            if (!useOdbcRules)
            {
                bool dquoteFound = (value.IndexOf('\"') > -1);
                bool squoteFound = (value.IndexOf('\'') > -1);

                if (dquoteFound && squoteFound)
                {
                    builder.Append('\"');
                    builder.Append(value.Replace("\"", "\"\""));
                    builder.Append('\"');
                }
                else if (dquoteFound)
                {
                    builder.Append('\'');
                    builder.Append(value);
                    builder.Append('\'');
                }
                else if (squoteFound || value.IndexOf('=') > -1 || value.IndexOf(';') > -1)
                {
                    builder.Append('\"');
                    builder.Append(value);
                    builder.Append('\"');
                }
                else if (ValueNeedsQuoting(value))
                {
                    builder.Append('\"');
                    builder.Append(value);
                    builder.Append('\"');
                }
                else
                {
                    builder.Append(value);
                }
            }
            else
            {
                int braces = 0;
                bool semicolonFound = false;
                int len = value.Length;
                bool needBraces = false;

                int lastChar = -1;

                for (int i = 0; i < len; i++)
                {
                    int peek = 0;
                    if (i == (len - 1))
                    {
                        peek = -1;
                    }
                    else
                    {
                        peek = value[i + 1];
                    }

                    char c = value[i];
                    switch (c)
                    {
                        case '{':
                            braces++;
                            break;
                        case '}':
                            if (peek.Equals(c))
                            {
                                i++;
                                continue;
                            }
                            else
                            {
                                braces--;
                                if (peek != -1)
                                {
                                    needBraces = true;
                                }
                            }
                            break;
                        case ';':
                            semicolonFound = true;
                            break;
                        default:
                            break;
                    }
                    lastChar = c;
                }

                if (value[0] == '{' && (lastChar != '}' || (braces == 0 && needBraces)))
                {
                    builder.Append('{');
                    builder.Append(value.Replace("}", "}}"));
                    builder.Append('}');
                    return;
                }

                bool isDriver = (string.Compare(keyword, "Driver", StringComparison.OrdinalIgnoreCase) == 0);
                if (isDriver)
                {
                    if (value[0] == '{' && lastChar == '}' && !needBraces)
                    {
                        builder.Append(value);
                        return;
                    }
                    builder.Append('{');
                    builder.Append(value.Replace("}", "}}"));
                    builder.Append('}');
                    return;
                }

                if (value[0] == '{' && (braces != 0 || lastChar != '}') && needBraces)
                {
                    builder.Append('{');
                    builder.Append(value.Replace("}", "}}"));
                    builder.Append('}');
                    return;
                }

                if (value[0] != '{' && semicolonFound)
                {
                    builder.Append('{');
                    builder.Append(value.Replace("}", "}}"));
                    builder.Append('}');
                    return;
                }

                builder.Append(value);
            }
        }

        public static void AppendKeyValuePair(StringBuilder builder, string keyword, string value)
        {
            AppendKeyValuePair(builder, keyword, value, false);
        }

        public virtual bool ContainsKey(string keyword)
        {
            if (keyword == null)
            {
                throw new ArgumentNullException("keyword");
            }
            return this._dictionary.ContainsKey(keyword);
        }

        public virtual bool EquivalentTo(DbConnectionStringBuilder connectionStringBuilder)
        {
            bool ret = true;
            try
            {
                if (this.Count != connectionStringBuilder.Count)
                {
                    ret = false;
                }
                else
                {
                    foreach (string key in this.Keys)
                    {
                        if (!this[key].Equals(connectionStringBuilder[key]))
                        {
                            ret = false;
                            break;
                        }
                    }
                }
            }
            catch (ArgumentException)
            {
                ret = false;
            }
            return ret;
        }

        [MonoTODO]
        protected virtual void GetProperties(IDictionary propertyDescriptors)
        {
            throw new NotImplementedException();
        }

        [MonoTODO]
        protected internal void ClearPropertyDescriptors()
        {
            throw new NotImplementedException();
        }

        public virtual bool Remove(string keyword)
        {
            if (keyword == null)
            {
                throw new ArgumentNullException("keyword");
            }
            return this._dictionary.Remove(keyword);
        }

        public virtual bool ShouldSerialize(string keyword)
        {
            throw new NotImplementedException();
        }

        public override string ToString()
        {
            return this.ConnectionString;
        }

        public virtual bool TryGetValue(string keyword, out object value)
        {
            // FIXME : not sure, difference between this [keyword] and this method
            bool found = this.ContainsKey(keyword);
            if (found)
            {
                value = this[keyword];
            }
            else
            {
                value = null;
            }
            return found;
        }

        private static ArgumentException CreateInvalidKeywordException(string keyword)
        {
            return new ArgumentException("A keyword cannot contain "
                                         + "control characters, leading semicolons or "
                                         + "leading or trailing whitespace.", keyword);
        }

        private static ArgumentException CreateConnectionStringInvalidException(int index)
        {
            return new ArgumentException("Format of initialization "
                                         + "string does not conform to specifications at "
                                         + "index " + index + ".");
        }

        private static bool ValueNeedsQuoting(string value)
        {
            foreach (char c in value)
            {
                if (char.IsWhiteSpace(c))
                {
                    return true;
                }
            }
            return false;
        }

        private void ParseConnectionString(string connectionString)
        {
            if (this.useOdbcRules)
            {
                this.ParseConnectionStringOdbc(connectionString);
            }
            else
            {
                this.ParseConnectionStringNonOdbc(connectionString);
            }
        }

        private void ParseConnectionStringOdbc(string connectionString)
        {
            bool inQuote = false;
            bool inDQuote = false;
            bool inName = true;
            bool inBraces = false;

            string name = String.Empty;
            string val = String.Empty;
            var sb = new StringBuilder();
            int len = connectionString.Length;

            for (int i = 0; i < len; i++)
            {
                char c = connectionString[i];
                int peek = (i == (len - 1)) ? -1 : connectionString[i + 1];

                switch (c)
                {
                    case '{':
                        if (inName)
                        {
                            sb.Append(c);
                            continue;
                        }

                        if (sb.Length == 0)
                        {
                            inBraces = true;
                        }
                        sb.Append(c);
                        break;
                    case '}':
                        if (inName || !inBraces)
                        {
                            sb.Append(c);
                            continue;
                        }

                        if (peek == -1)
                        {
                            sb.Append(c);
                            inBraces = false;
                        }
                        else if (peek.Equals(c))
                        {
                            sb.Append(c);
                            sb.Append(c);
                            i++;
                        }
                        else
                        {
                            int next = NextNonWhitespaceChar(connectionString, i);
                            if (next != -1 && ((char) next) != ';')
                            {
                                throw CreateConnectionStringInvalidException(next);
                            }
                            sb.Append(c);
                            inBraces = false;
                        }
                        break;
                    case ';':
                        if (inName || inBraces)
                        {
                            sb.Append(c);
                            continue;
                        }

                        if (name.Length > 0 && sb.Length > 0)
                        {
                            val = sb.ToString();
                            name = name.ToLower().TrimEnd();
                            this[name] = val;
                        }
                        else if (sb.Length > 0)
                        {
                            throw CreateConnectionStringInvalidException(c);
                        }
                        inName = true;
                        name = String.Empty;
                        sb.Length = 0;
                        break;
                    case '=':
                        if (inBraces || !inName)
                        {
                            sb.Append(c);
                            continue;
                        }

                        name = sb.ToString();
                        if (name.Length == 0)
                        {
                            throw CreateConnectionStringInvalidException(c);
                        }
                        sb.Length = 0;
                        inName = false;
                        break;
                    default:
                        if (inDQuote || inQuote || inBraces)
                        {
                            sb.Append(c);
                        }
                        else if (char.IsWhiteSpace(c))
                        {
                            // ignore leading whitespace
                            if (sb.Length > 0)
                            {
                                int nextChar = SkipTrailingWhitespace(connectionString, i);
                                if (nextChar == -1)
                                {
                                    sb.Append(c);
                                }
                                else
                                {
                                    i = nextChar;
                                }
                            }
                        }
                        else
                        {
                            sb.Append(c);
                        }
                        break;
                }
            }

            if ((inName && sb.Length > 0) || inDQuote || inQuote || inBraces)
            {
                throw CreateConnectionStringInvalidException(len - 1);
            }

            if (name.Length > 0 && sb.Length > 0)
            {
                val = sb.ToString();
                name = name.ToLower().TrimEnd();
                this[name] = val;
            }
        }

        private void ParseConnectionStringNonOdbc(string connectionString)
        {
            bool inQuote = false;
            bool inDQuote = false;
            bool inName = true;

            string name = String.Empty;
            string val = String.Empty;
            var sb = new StringBuilder();
            int len = connectionString.Length;

            for (int i = 0; i < len; i++)
            {
                char c = connectionString[i];
                int peek = (i == (len - 1)) ? -1 : connectionString[i + 1];

                switch (c)
                {
                    case '\'':
                        if (inName)
                        {
                            sb.Append(c);
                            continue;
                        }

                        if (inDQuote)
                        {
                            sb.Append(c);
                        }
                        else if (inQuote)
                        {
                            if (peek == -1)
                            {
                                inQuote = false;
                            }
                            else if (peek.Equals(c))
                            {
                                sb.Append(c);
                                i++;
                            }
                            else
                            {
                                int next = NextNonWhitespaceChar(connectionString, i);
                                if (next != -1 && ((char) next) != ';')
                                {
                                    throw CreateConnectionStringInvalidException(next);
                                }
                                inQuote = false;
                            }

                            if (!inQuote)
                            {
                                val = sb.ToString();
                                name = name.ToLower().TrimEnd();
                                this[name] = val;
                                inName = true;
                                name = String.Empty;
                                sb.Length = 0;
                            }
                        }
                        else if (sb.Length == 0)
                        {
                            inQuote = true;
                        }
                        else
                        {
                            sb.Append(c);
                        }
                        break;
                    case '"':
                        if (inName)
                        {
                            sb.Append(c);
                            continue;
                        }

                        if (inQuote)
                        {
                            sb.Append(c);
                        }
                        else if (inDQuote)
                        {
                            if (peek == -1)
                            {
                                inDQuote = false;
                            }
                            else if (peek.Equals(c))
                            {
                                sb.Append(c);
                                i++;
                            }
                            else
                            {
                                int next = NextNonWhitespaceChar(connectionString, i);
                                if (next != -1 && ((char) next) != ';')
                                {
                                    throw CreateConnectionStringInvalidException(next);
                                }
                                inDQuote = false;
                            }
                        }
                        else if (sb.Length == 0)
                        {
                            inDQuote = true;
                        }
                        else
                        {
                            sb.Append(c);
                        }
                        break;
                    case ';':
                        if (inName)
                        {
                            sb.Append(c);
                            continue;
                        }

                        if (inDQuote || inQuote)
                        {
                            sb.Append(c);
                        }
                        else
                        {
                            if (name.Length > 0 && sb.Length > 0)
                            {
                                val = sb.ToString();
                                name = name.ToLower().TrimEnd();
                                this[name] = val;
                            }
                            else if (sb.Length > 0)
                            {
                                throw CreateConnectionStringInvalidException(c);
                            }
                            inName = true;
                            name = String.Empty;
                            sb.Length = 0;
                        }
                        break;
                    case '=':
                        if (inDQuote || inQuote || !inName)
                        {
                            sb.Append(c);
                        }
                        else if (peek != -1 && peek.Equals(c))
                        {
                            sb.Append(c);
                            i++;
                        }
                        else
                        {
                            name = sb.ToString();
                            if (name.Length == 0)
                            {
                                throw CreateConnectionStringInvalidException(c);
                            }
                            sb.Length = 0;
                            inName = false;
                        }
                        break;
                    default:
                        if (inDQuote || inQuote)
                        {
                            sb.Append(c);
                        }
                        else if (char.IsWhiteSpace(c))
                        {
                            // ignore leading whitespace
                            if (sb.Length > 0)
                            {
                                int nextChar = SkipTrailingWhitespace(connectionString, i);
                                if (nextChar == -1)
                                {
                                    sb.Append(c);
                                }
                                else
                                {
                                    i = nextChar;
                                }
                            }
                        }
                        else
                        {
                            sb.Append(c);
                        }
                        break;
                }
            }

            if ((inName && sb.Length > 0) || inDQuote || inQuote)
            {
                throw CreateConnectionStringInvalidException(len - 1);
            }

            if (name.Length > 0 && sb.Length > 0)
            {
                val = sb.ToString();
                name = name.ToLower().TrimEnd();
                this[name] = val;
            }
        }

        private static int SkipTrailingWhitespace(string value, int index)
        {
            int len = value.Length;
            for (int i = (index + 1); i < len; i++)
            {
                char c = value[i];
                if (c == ';')
                {
                    return (i - 1);
                }
                if (!char.IsWhiteSpace(c))
                {
                    return -1;
                }
            }
            return len - 1;
        }

        private static int NextNonWhitespaceChar(string value, int index)
        {
            int len = value.Length;
            for (int i = (index + 1); i < len; i++)
            {
                char c = value[i];
                if (!char.IsWhiteSpace(c))
                {
                    return c;
                }
            }
            return -1;
        }

        #endregion // Public Methods
    }
}
