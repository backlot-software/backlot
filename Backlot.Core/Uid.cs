using System;

namespace Backlot.Core
{
    public static class Uid
    {
        private const string IdFormat = "N";

        /// <summary>
        /// Generates a new guid in the globaly defined format.
        /// </summary>
        /// <returns></returns>
        public static string New()
        {
            return Guid.NewGuid().ToString(IdFormat);
        }

        public static string Empty()
        {
            return Guid.Empty.ToString(IdFormat);
        }
    }
}
