// <copyright company="Geoinformation Winterthur">
//      Author: Edgar Butwilowski
//      Copyright (c) Geoinformation Winterthur. All rights reserved.
// </copyright>
namespace roadwork_portal_service.Model;

public class Address
{
    public int? egaid { get; set; }
    public string? address { get; set; }
    public double? x { get; set; }
    public double? y { get; set; }
}
