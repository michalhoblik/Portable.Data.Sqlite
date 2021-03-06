﻿/********************************************************
 * ADO.NET 2.0 Data Provider for SQLite Version 3.X
 * Written by Robert Simpson (robert@blackcastlesoft.com)
 * 
 * Released to the public domain, use at your own risk!
 ********************************************************/

//Was: Mono.Data.Sqlite {
namespace Portable.Data.Sqlite {
    using System;
    //using System.Data;
    using System.Linq;
    using Portable.Data;
    using System.Collections.Generic;
    using System.Globalization;
    //using MonoDataSqliteWrapper;
    using Portable.Data.Sqlite.Wrapper;

    /// <summary>
    /// Represents a single SQL statement in SQLite.
    /// </summary>
    internal sealed class SqliteStatement : IDisposable {

        private string _sqlStmt;

        private Dictionary<int, string> _paramList;

        public Dictionary<int, string> ParamList {
            get { return _paramList; }
        }

        /// <summary>
        /// The underlying SQLite object this statement is bound to
        /// </summary>
        internal SqliteBase _sql;
        /// <summary>
        /// The command text of this SQL statement
        /// </summary>
        internal string _sqlStatement {
            get { return _sqlStmt; }
            set {
                _sqlStmt = String.IsNullOrWhiteSpace(value) ? null : value.Trim();
                _paramList = _getParamList(_sqlStmt);
            }
        }

        private Dictionary<int, string> _getParamList(string sql) {
            var result = new Dictionary<int, string>();

            if (!String.IsNullOrWhiteSpace(sql)) {
                sql = sql.Replace("@@", "**").Trim().ToLower();  //take out .ToLower() if SQLite parameter matching is case sensitive
                if (sql.Contains("@")) {
                    int paramIndex = 0;
                    string param = "";
                    char current;
                    for (int i = 0; i < sql.Length; i++) {
                        current = sql.ToCharArray()[i];
                        if (("abcdefghijklmnopqrstuvwxyz0123456789_").Contains(current.ToString())) {
                            if (param != "") param += current;
                        }
                        else {
                            if (param != "") {
                                result.Add(paramIndex, param);
                                param = "";
                            }
                            if (current == '@') {
                                param += "@";
                                paramIndex++;
                            }
                        }
                    }
                    if (param != "") result.Add(paramIndex, param);
                }
            }

            return result;
        }

        /// <summary>
        /// The actual statement pointer
        /// </summary>
        internal SQLitePCL.ISQLiteStatement _sqlite_stmt;
        //Was: internal SqliteStatementHandle _sqlite_stmt;

        /// <summary>
        /// An index from which unnamed parameters begin
        /// </summary>
        internal int _unnamedParameters;
        /// <summary>
        /// Names of the parameters as SQLite understands them to be
        /// </summary>
        internal string[] _paramNames;
        /// <summary>
        /// Parameters for this statement
        /// </summary>
        internal SqliteParameter[] _paramValues;
        /// <summary>
        /// Command this statement belongs to (if any)
        /// </summary>
        internal SqliteCommand _command;

        private string[] _types;

        /// <summary>
        /// Initializes the statement and attempts to get all information about parameters in the statement
        /// </summary>
        /// <param name="sqlbase">The base SQLite object</param>
        /// <param name="stmt">The statement</param>
        /// <param name="strCommand">The command text for this statement</param>
        /// <param name="previous">The previous command in a multi-statement command</param>
        internal SqliteStatement(SqliteBase sqlbase, SQLitePCL.ISQLiteStatement stmt, string strCommand, SqliteStatement previous) {
            _sql = sqlbase;
            _sqlite_stmt = stmt;
            this._sqlStatement = strCommand;

            // Determine parameters for this statement (if any) and prepare space for them.
            int nCmdStart = 0;
            int n = _sql.Bind_ParamCount(this);
            int x;
            string s;

            if (n > 0) {
                if (previous != null)
                    nCmdStart = previous._unnamedParameters;

                _paramNames = new string[n];
                _paramValues = new SqliteParameter[n];

                for (x = 0; x < n; x++) {
                    s = _sql.Bind_ParamName(this, x + 1);
                    if (String.IsNullOrEmpty(s)) {
                        s = String.Format(CultureInfo.InvariantCulture, ";{0}", nCmdStart);
                        nCmdStart++;
                        _unnamedParameters++;
                    }
                    _paramNames[x] = s;
                    _paramValues[x] = null;
                }
            }
        }

        /// <summary>
        /// Called by SqliteParameterCollection, this function determines if the specified parameter name belongs to
        /// this statement, and if so, keeps a reference to the parameter so it can be bound later.
        /// </summary>
        /// <param name="s">The parameter name to map</param>
        /// <param name="p">The parameter to assign it</param>
        internal bool MapParameter(string s, SqliteParameter p) {
            if (_paramNames == null) return false;

            int startAt = 0;
            if (s.Length > 0) {
                if (":$@;".IndexOf(s[0]) == -1)
                    startAt = 1;
            }

            int x = _paramNames.Length;
            for (int n = 0; n < x; n++) {
                if (String.Compare(_paramNames[n], startAt, s, 0, Math.Max(_paramNames[n].Length - startAt, s.Length), StringComparison.OrdinalIgnoreCase) == 0) {
                    _paramValues[n] = p;
                    return true;
                }
            }
            return false;
        }

        #region IDisposable Members
        /// <summary>
        /// Disposes and finalizes the statement
        /// </summary>
        public void Dispose() {
            if (_sqlite_stmt != null) {
                _sqlite_stmt.Dispose();
            }
            _sqlite_stmt = null;

            _paramNames = null;
            _paramValues = null;
            _sql = null;
            _sqlStmt = null;
            _paramList = null;
        }
        #endregion

        /// <summary>
        ///  Bind all parameters, reconcile parameters found with specified parameter values
        /// </summary>
        internal void BindParameters() {
            if (_paramNames == null) return;

            //creating a dictionary of parameter names - should not have two parameter values with the same parameter name
            var paramDictionary = new Dictionary<string, SqliteParameter>();
            foreach (SqliteParameter parameter in _paramValues) {
                if (parameter != null && (!String.IsNullOrWhiteSpace(parameter.ParameterName))) {
                    paramDictionary.Add(parameter.ParameterName.Trim().ToLower(), parameter);
                }
            }

            //check for duplicate parameters and only process unique ones - matches how SQLite processes parameters
            if (_paramNames.Length > 0) {
                var usedParams = new List<string>();
                int paramIndex = 0;
                for (int n = 0; n < _paramNames.Length; n++) {
                    if (String.IsNullOrWhiteSpace(_paramNames[n])) {
                        if (n < (_paramNames.Length - 1)) {
                            //ignoring a blank parameter if it is the last one
                            throw new Exception("Cannot have a blank or null parameter name.");
                        }
                    }
                    else {
                        string currentParam = _paramNames[n].Trim().ToLower();
                        if (!paramDictionary.ContainsKey(currentParam)) { throw new Exception("No value specified for SQLite parameter '" + _paramNames[n] + "'."); }
                        if (!usedParams.Contains(currentParam)) {
                            paramIndex++;
                            BindParameter(paramIndex, paramDictionary[currentParam]);
                            //BindParameter(n + 1, paramDictionary[currentParam]);
                            usedParams.Add(currentParam);
                        }
                    }
                }
            }

            //for (int n = 0; n < _paramNames.Length; n++) {
            //    if (String.IsNullOrWhiteSpace(_paramNames[n])) {  }
            //    if (!paramDictionary.ContainsKey(_paramNames[n].Trim().ToLower())) { throw new Exception("No value specified for SQLite parameter '" + _paramNames[n] + "'."); }

            //    BindParameter(n + 1, paramDictionary[_paramNames[n].Trim().ToLower()]);
            //}
        }

        /// <summary>
        /// Perform the bind operation for an individual parameter
        /// </summary>
        /// <param name="index">The index of the parameter to bind</param>
        /// <param name="param">The parameter we're binding</param>
        private void BindParameter(int index, SqliteParameter param) {
            if (param == null)
                throw new SqliteException((int)SqliteErrorCode.Error, "Insufficient parameters supplied to the command");

            object obj = param.Value;
            DbType objType = param.DbType;

            if (obj == DBNull.Value || obj == null) {
                _sql.Bind_Null(this, index);
                return;
            }

            if (objType == DbType.Object)
                objType = SqliteConvert.TypeToDbType(obj.GetType());

            switch (objType) {
                case DbType.Date:
                case DbType.Time:
                case DbType.DateTime:
                    _sql.Bind_DateTime(this, index, Convert.ToDateTime(obj, CultureInfo.CurrentCulture));
                    break;
                case DbType.Int64:
                case DbType.UInt64:
                case DbType.UInt32:
                    _sql.Bind_Int64(this, index, Convert.ToInt64(obj, CultureInfo.CurrentCulture));
                    break;
                case DbType.Boolean:
                case DbType.Int16:
                case DbType.Int32:
                case DbType.UInt16:
                case DbType.SByte:
                case DbType.Byte:
                    _sql.Bind_Int32(this, index, Convert.ToInt32(obj, CultureInfo.CurrentCulture));
                    break;
                case DbType.Single:
                case DbType.Double:
                case DbType.Currency:
                    //case DbType.Decimal: // Dont store decimal as double ... loses precision
                    _sql.Bind_Double(this, index, Convert.ToDouble(obj, CultureInfo.CurrentCulture));
                    break;
                case DbType.Binary:
                    _sql.Bind_Blob(this, index, (byte[])obj);
                    break;
                case DbType.Guid:
                    if (_command.Connection._binaryGuid == true)
                        _sql.Bind_Blob(this, index, ((Guid)obj).ToByteArray());
                    else
                        _sql.Bind_Text(this, index, obj.ToString());

                    break;
                case DbType.Decimal: // Dont store decimal as double ... loses precision
                    _sql.Bind_Text(this, index, Convert.ToDecimal(obj, CultureInfo.CurrentCulture).ToString(CultureInfo.InvariantCulture));
                    break;
                default:
                    _sql.Bind_Text(this, index, obj.ToString());
                    break;
            }
        }

        internal string[] TypeDefinitions {
            get { return _types; }
        }

        internal void SetTypes(string typedefs) {
            int pos = typedefs.IndexOf("TYPES", 0, StringComparison.OrdinalIgnoreCase);
            if (pos == -1) throw new ArgumentOutOfRangeException();

            string[] types = typedefs.Substring(pos + 6).Replace(" ", "").Replace(";", "").Replace("\"", "").Replace("[", "").Replace("]", "").Replace("`", "").Split(',', '\r', '\n', '\t');

            int n;
            for (n = 0; n < types.Length; n++) {
                if (String.IsNullOrEmpty(types[n]) == true)
                    types[n] = null;
            }
            _types = types;
        }
    }
}
