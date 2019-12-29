using System;

namespace EnjentUnitTests
{
	public static class ArrayLittleEndianExtension
	{

        /// <summary>
        /// Fields within a WebSocket frame are to be interpreted as BIG ENDIAN, however, most client architectures use little endian,
        /// in which case it is necessary to reverse an array representing a datatype such as a uint before attempting to convert
        /// this array to the said datatype.
        /// </summary>
        /// <param name="that">The array to MAYBE reverse</param>
        /// <remark>
        /// If the current architecture uses little endian, the passed array IS MODIFIED IN PLACE, else, it is untouched.
        /// </remark>
        public static void ReverseIfLittleEndian(this Array that)
        {
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(that);
            }
        }
	}
}
