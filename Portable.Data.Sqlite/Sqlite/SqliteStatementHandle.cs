﻿using System;
#if !PORTABLE
using Microsoft.Win32.SafeHandles;
#else
using System.Runtime.InteropServices;
#endif

namespace Portable.Data.Sqlite
{
	internal sealed class SqliteStatementHandle
#if PORTABLE
		: CriticalHandle
#else
		: SafeHandleZeroOrMinusOneIsInvalid
#endif
	{

        private SqliteLockContext _lockContext;
        public SqliteLockContext LockContext {
            get { return _lockContext; }
            internal set {
                if (value == null) { throw new ArgumentNullException(nameof(value)); }
                _lockContext = value;
            }
        }

        public SqliteStatementHandle()
#if PORTABLE
			: base((IntPtr) 0)
#else
            : base(ownsHandle: true)
#endif
        {
        }

        public SqliteStatementHandle(SqliteLockContext lockContext)
#if PORTABLE
			: base((IntPtr) 0)
#else
			: base(ownsHandle: true)
#endif
		{
            if (lockContext == null) { throw new ArgumentNullException(nameof(lockContext)); }
            _lockContext = lockContext;
        }

#if PORTABLE
		public override bool IsInvalid
		{
			get
			{
				return handle == new IntPtr(-1) || handle == (IntPtr) 0;
			}
		}
#endif

		protected override bool ReleaseHandle()
		{
			return _lockContext.sqlite3_finalize(handle) == SqliteErrorCode.Ok;
		}
	}
}
