// <copyright company="Vermessungsamt Winterthur">
//      Author: Edgar Butwilowski
//      Copyright (c) Vermessungsamt Winterthur. All rights reserved.
// </copyright>
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using roadwork_portal_service.Configuration;
using NetTopologySuite.Geometries;
using Npgsql;

namespace roadwork_portal_service.Helper
{
    public class HelperFunctions
    {
        public static string hashPassphrase(string passphrase)
        {
            string hashedPassphrase = Convert.ToBase64String(KeyDerivation.Pbkdf2(
                password: passphrase,
                salt: AppConfig.salt,
                prf: KeyDerivationPrf.HMACSHA256,
                iterationCount: 100000,
                numBytesRequested: 256 / 8
            ));
            return hashedPassphrase;
        }

        public static string getAddressNames(Polygon area, NpgsqlConnection pgConn)
        {
            string result = "";
            int bufferSize = 0;
            List<(string, Point)> fromToNamesList;
            (string, Point)[] greatestDistanceTuple;
            Polygon bufferedPoly = area.Copy() as Polygon;

            do
            {
                if (bufferSize > 0)
                {
                    bufferedPoly = bufferedPoly.Buffer(bufferSize) as Polygon;
                }

                fromToNamesList = _getFromToListFromDb(bufferedPoly, pgConn);

                greatestDistanceTuple = _calcGreatestDistanceTuple(fromToNamesList);

                if (greatestDistanceTuple[0].Item1 != null && greatestDistanceTuple[1].Item1 != null)
                {
                    result = greatestDistanceTuple[0].Item1;
                    if(greatestDistanceTuple[0].Item1 != greatestDistanceTuple[1].Item1)
                        result  += " bis " + greatestDistanceTuple[1].Item1;
                }
                bufferSize += 10;
            } while (bufferSize < 100 && result == "");

            if(result == "")
                result = "Es konnte keine Bezeichnung gefunden werden";

            return result;
        }

        public static string translateStatusCodes(string code)
        {
            if(code == "requirement")
                return "Bedarf";
            else if(code == "review")
                return "Prüfung";
            else if(code == "verified")
                return "verifiziert";
            else if(code == "inconsult")
                return "Bedarfsklärung";
            else if(code == "reporting")
                return "Stellungnahme";
            else if(code == "coordinated")
                return "koordiniert";
            else if(code == "prestudy")
                return "Vorstudie";
            else if(code == "suspended")
                return "sistiert";
            else return "";
        }

        private static List<(string, Point)> _getFromToListFromDb(
                Polygon roadWorkNeedPoly, NpgsqlConnection pgConn)
        {
            List<(string, Point)> fromToNamesList = new List<(string, Point)>();
            NpgsqlCommand selectFromToNames = pgConn.CreateCommand();
            selectFromToNames.CommandText = @"SELECT address, geom
                                    FROM ""addresses""
                                    WHERE ST_Intersects(@geom, geom)";
            selectFromToNames.Parameters.AddWithValue("geom", roadWorkNeedPoly);

            using (NpgsqlDataReader reader = selectFromToNames.ExecuteReader())
            {
                while (reader.Read())
                {
                    string address = reader.IsDBNull(0) ? "" : reader.GetString(0);
                    Point p = reader.IsDBNull(1) ? Point.Empty : reader.GetValue(1) as Point;
                    fromToNamesList.Add((address, p));
                }
            }
            return fromToNamesList;
        }

        private static (string, Point)[] _calcGreatestDistanceTuple(List<(string, Point)> fromToNamesList)
        {

            double distance = 0d;
            double greatestDistance = 0d;
            (string, Point)[] greatestDistanceTuple = new (string, Point)[2];
            foreach ((string, Point) fromToNamesTuple1 in fromToNamesList)
            {
                foreach ((string, Point) fromToNamesTuple2 in fromToNamesList)
                {
                    distance = fromToNamesTuple1.Item2.Distance(fromToNamesTuple2.Item2);
                    if (distance >= greatestDistance)
                    {
                        greatestDistance = distance;
                        greatestDistanceTuple[0] = fromToNamesTuple1;
                        greatestDistanceTuple[1] = fromToNamesTuple2;
                    }
                }
            }
            return greatestDistanceTuple;

        }

    }
}
