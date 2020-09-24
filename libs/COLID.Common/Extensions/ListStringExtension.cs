using System;
using System.Collections.Generic;
using System.Linq;

namespace COLID.Common.Extensions
{
    public static class ListStringExtension
    {
        public static int CompareVersionTo(this string thisVersion, string otherVersion)
        {
            if (thisVersion == null)
            {
                throw new ArgumentNullException(nameof(thisVersion));
            }

            if (otherVersion == null)
            {
                throw new ArgumentNullException(nameof(otherVersion));
            }

            return thisVersion.Split('.').ToList().CompareVersionTo(otherVersion.Split('.').ToList());
        }

        private static int CompareVersionTo(this IList<string> thisVersion, IList<string> otherVersion)
        {
            if (thisVersion == null)
            {
                throw new ArgumentNullException(nameof(thisVersion));
            }

            if (otherVersion == null)
            {
                throw new ArgumentNullException(nameof(otherVersion));
            }

            int versionSize = Math.Max(thisVersion.Count, otherVersion.Count);

            while (thisVersion.Count < versionSize)
            {
                thisVersion.Add("0");
            }

            while (otherVersion.Count < versionSize)
            {
                otherVersion.Add("0");
            }

            for (int i = 0; i < versionSize; i++)
            {
                int thisVersionIndexValue = int.Parse(thisVersion[i]);
                int otherVersionIndexValue = int.Parse(otherVersion[i]);

                if (thisVersionIndexValue < otherVersionIndexValue)
                {
                    return -1;
                }
                else if (thisVersionIndexValue > otherVersionIndexValue)
                {
                    return 1;
                }
                else
                {
                    continue;
                }
            }

            return 0;
        }
    }
}
