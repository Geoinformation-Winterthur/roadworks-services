// <copyright company="Vermessungsamt Winterthur">
//      Author: Edgar Butwilowski
//      Copyright (c) Vermessungsamt Winterthur. All rights reserved.
// </copyright>
using System.Numerics;

namespace roadwork_portal_service.Model
{
    public class User
    {
        public string uuid { get; set; } = "";
        public string mailAddress { get; set; } = "";
        public string passPhrase { get; set;} = "";
        public string lastName { get; set;} = "";
        public string firstName { get; set;} = "";
        public DateTime? lastLoginAttempt { get; set; }
        public DateTime? databaseTime { get; set; }
        public string role { get; set;} = "";
    }
}
