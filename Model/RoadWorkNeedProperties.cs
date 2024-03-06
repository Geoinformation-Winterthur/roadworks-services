// <copyright company="Vermessungsamt Winterthur">
//      Author: Edgar Butwilowski
//      Copyright (c) Vermessungsamt Winterthur. All rights reserved.
// </copyright>
namespace roadwork_portal_service.Model;

public class RoadWorkNeedProperties
{
    public string uuid { get; set; } = "";
    public string name { get; set; } = "";
    public RoadWorkNeedEnum kind { get; set; } = new RoadWorkNeedEnum();
    public User orderer { get; set; } = new User();
    public DateTime created { get; set; } = DateTime.MinValue;
    public DateTime lastModified { get; set; } = DateTime.MinValue;
    public DateTime finishEarlyTo { get; set; } = DateTime.MinValue;
    public DateTime finishOptimumTo { get; set; } = DateTime.MinValue;
    public DateTime finishLateTo { get; set; } = DateTime.MinValue;
    public Priority priority { get; set; } = new Priority();
    public Status status { get; set; } = new Status();
    public decimal costs { get; set; } = 0m;
    public int relevance { get; set; } = 0;
    public string activityRelationType { get; set; } = "";    
    public string description { get; set; } = "";
    public string roadWorkActivityUuid { get; set; } = "";
    public bool isEditingAllowed { get; set; } = false;
    public string noteOfAreaManager { get; set; } = "";
    public DateTime areaManagerNoteDate { get; set; } = DateTime.MinValue;
    public User areaManagerOfNote { get; set; } = new User();
    public bool isPrivate { get; set; } = false;
}

