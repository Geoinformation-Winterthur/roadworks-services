// <copyright company="Vermessungsamt Winterthur">
//      Author: Edgar Butwilowski
//      Copyright (c) Vermessungsamt Winterthur. All rights reserved.
// </copyright>
using System.Numerics;

namespace roadwork_portal_service.Helper;

public class GuidConverter
{
    public static BigInteger GuidToInt128bit(string uuidString)
    {
        if(uuidString.Length != 36)
        {
            throw new ArgumentException("UUID has wrong number of digits.");
        }
        byte[] uuidBytes = new byte[16];
        uuidString = uuidString.Replace("-", "");
        int byteArrayPos = 0;
        for(int i = 0; i < uuidString.Length; i+=2)
        {
            string uuidStringPart = uuidString.Substring(i, 2);
            int uuidIntPart = Convert.ToInt32(uuidStringPart, 16);
            byte uuidByte = BitConverter.GetBytes(uuidIntPart)[0];
            uuidBytes[byteArrayPos++] = uuidByte;
        }
        return new BigInteger(uuidBytes);
    }

    public static string Int128bitToGuid(BigInteger uuidInt)
    {
        string result = Convert.ToHexString(uuidInt.ToByteArray());
        result = result.ToLower();
        result = result.Insert(8, "-");
        result = result.Insert(13, "-");
        result = result.Insert(18, "-");
        result = result.Insert(23, "-");
        return result;
    }

}

