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
        public static DateTime LastModified(Delegate @delegate)
        {
            if (@delegate.Target != null)
            {
                var targetAssembly = @delegate.Target.GetType().Assembly;
                return LastModified(targetAssembly);
            }
            var declaringAssembly = @delegate.Method.DeclaringType!.Assembly;
            return LastModified(declaringAssembly);
        }

        public static DateTime LastModified(Assembly assembly)
        {
            return File.GetLastWriteTime(assembly.Location);
        }

        public static DateTime LastModified<T>()
        {
            return File.GetLastWriteTime(typeof(T).Assembly.Location);
        }
    }
    #endregion
}