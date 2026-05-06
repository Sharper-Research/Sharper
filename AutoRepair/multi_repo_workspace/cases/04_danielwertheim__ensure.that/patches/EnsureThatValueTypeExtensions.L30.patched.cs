using System;

namespace EnsureThat
{
    public static class EnsureThatValueTypeExtensions
    {
        public static Param<bool> IsTrue(this in Param<bool> param)
        {
            Ensure.Bool.IsTrue(param.Value, param.Name);

            return param;
        }

        public static Param<bool> IsFalse(this in Param<bool> param)
        {
            Ensure.Bool.IsFalse(param.Value, param.Name);

            return param;
        }

        public static Param<T> IsNotDefault<T>(this in Param<T> param) where T : struct
        {
            Ensure.Any.IsNotDefault(param.Value, param.Name);

            return param;
        }

        public static Param<T?> IsNotNull<T>(this in Param<T?> param) where T : struct
        {
            // [AutoRepair Step 3] Forward-propagated guard against System.ArgumentNullException
            try
            {
                Ensure.Any.IsNotNull(param.Value, param.Name);
            }
            catch (System.ArgumentNullException)
            {
            }

            return param;
        }

        public static Param<Guid> IsNotEmpty(this in Param<Guid> param)
        {
            Ensure.Guid.IsNotEmpty(param.Value, param.Name);

            return param;
        }
    }
}
