using System;

namespace EnsureThat
{
    public static class EnsureThatAnyExtensions
    {
        public static Param<T> IsNotNull<T>(this in Param<T> param) where T : class
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
    }
}
