using System;
using System.IO;
using System.Reflection;

#if EF
namespace EfLocalDb
#else
namespace LocalDb
#endif
{
    #region Timestamp
    public static class Timestamp
    {
        static string Path(this Assembly assembly)
        {
            return assembly.CodeBase
                .Replace("file:///", "")
                .Replace("file://", "")
                .Replace(@"file:\\\", "")
                .Replace(@"file:\\", "");
        }

        public static DateTime LastModified(Delegate @delegate)
        {
            Guard.AgainstNull(nameof(@delegate), @delegate);
            if (@delegate.Target != null)
            {
                var targetAssembly = @delegate.Target.GetType().Assembly;
                return LastModified(targetAssembly);
            }
            var declaringAssembly = @delegate.Method.DeclaringType.Assembly;
            return LastModified(declaringAssembly);
        }

        public static DateTime LastModified(Assembly assembly)
        {
            Guard.AgainstNull(nameof(assembly), assembly);
            return File.GetLastWriteTime(assembly.Path());
        }

        public static DateTime LastModified<T>()
        {
            return File.GetLastWriteTime(typeof(T).Assembly.Path());
        }
    }
    #endregion
}