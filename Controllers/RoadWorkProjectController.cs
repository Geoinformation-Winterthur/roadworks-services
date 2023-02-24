using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using roadwork_portal_service.Model;

namespace roadwork_portal_service.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class RoadWorkProjectController : ControllerBase
    {
        // ONLY FOR TEST PURPOSES:
        public static List<RoadWorkProjectFeature> roadworkProjects = new List<RoadWorkProjectFeature>();
        private readonly ILogger<RoadWorkProjectController> _logger;
        private IConfiguration Configuration { get; }

        public RoadWorkProjectController(ILogger<RoadWorkProjectController> logger,
                        IConfiguration configuration)
        {
            _logger = logger;
            this.Configuration = configuration;
        }

        // GET constructionproject/summaries
        [Route("/roadworkproject/summaries")]
        [Authorize]
        public IEnumerable<RoadWorkProjectFeature> GetSummaries()
        {
            return RoadWorkProjectController.roadworkProjects;
        }

        public static void createDummyData()
        {

            RoadWorkProjectFeature pd1 = new RoadWorkProjectFeature();
            pd1.properties.id = 1186;
            pd1.properties.projectNo = 1186;
            pd1.properties.place = "Ackeretstrasse 28";
            pd1.properties.status = "coordinated";
            pd1.properties.priority = "high";
            Geometry g1 = new Geometry(Geometry.GeometryType.Polygon,
                            new double[] { 2696355.8928168365, 1262142.752563282, 
                            2696361.1914095148, 1262153.6204136857,
                            2696426.181048607, 1262122.1993221624,
                            2696421.1518068938, 1262111.6086983322,
                            2696355.8928168365, 1262142.752563282 });
            pd1.geometry = g1;
            pd1.properties.area = "Habsburgstrasse bis Walkestrasse";
            pd1.properties.project = "Werkleitungsarbeiten Gas";
            pd1.properties.realizationUntil = DateTime.Now;
            pd1.properties.realizationUntil = pd1.properties.realizationUntil.AddYears(-1);

            RoadWorkProjectPart pdp1 = new RoadWorkProjectPart();
            pdp1.id = 5736272;
            pdp1.name = "Ackeretstrasse 28 Teil 1";
            pd1.properties.roadWorkProjectParts = new RoadWorkProjectPart[1];
            pd1.properties.roadWorkProjectParts[0] = pdp1;

            RoadWorkProjectController.roadworkProjects.Add(pd1);

            RoadWorkProjectFeature pd2 = new RoadWorkProjectFeature();
            pd2.properties.id = 938362;
            pd2.properties.projectNo = 938362;
            pd2.properties.place = "Ackerwiesenstrasse";
            Geometry g2 = new Geometry(Geometry.GeometryType.Polygon,
                            new double[] { 2695816.733287473, 1262408.6639447133,
                            2695820.664961812, 1262416.398539494,
                            2695792.617710639, 1262429.1935454537,
                            2695782.1061608004, 1262404.4764712732,
                            2695792.264386795, 1262399.201756592,
                            2695800.527073986, 1262415.932628408,
                            2695816.733287473, 1262408.6639447133 });
            pd2.geometry = g2;
            pd2.properties.area = "Ackerwiesenstrasse bis Wartstrasse";
            pd2.properties.project = "Kanalerneuerung";
            pd2.properties.realizationUntil = DateTime.Now;

            RoadWorkProjectController.roadworkProjects.Add(pd2);

            RoadWorkProjectFeature pd3 = new RoadWorkProjectFeature();
            pd3.properties.id = 38743;
            pd3.properties.projectNo = 132;
            pd3.properties.place = "Ackeretstrasse, Schützenstasse";
            Geometry g3 = new Geometry(Geometry.GeometryType.Polygon,
                            new double[] { 2696457.2288415264, 1262176.5642553968,
                            2696448.0188024184, 1262151.9966673607,
                            2696457.3727237727, 1262148.749425377,
                            2696466.587772522, 1262173.0080437995,
                            2696457.2288415264, 1262176.5642553968 });
            pd3.geometry = g3;
            pd3.properties.area = "An der Ackeretstrasse";
            pd3.properties.project = "Fensterrenovation";
            pd3.properties.realizationUntil = DateTime.Now;

            RoadWorkProjectPart pdp2_1 = new RoadWorkProjectPart();
            pdp2_1.id = 5736272;
            pdp2_1.name = "Baustelle 3 Teil 1";
            pd3.properties.roadWorkProjectParts = new RoadWorkProjectPart[1];
            pd3.properties.roadWorkProjectParts[0] = pdp2_1;

            RoadWorkProjectController.roadworkProjects.Add(pd3);

            RoadWorkProjectFeature pd4 = new RoadWorkProjectFeature();
            pd4.properties.id = 879126;
            pd4.properties.projectNo = 21321;
            pd4.properties.place = "Adlerstrasse";
            Geometry g4 = new Geometry(Geometry.GeometryType.Polygon,
                            new double[] { 2697733.2611742257, 1261529.245388204,
                            2697733.0024021436, 1261517.8903958846,
                            2697726.230749886, 1261508.8759117732,
                            2697730.7496713153, 1261505.6121669635,
                            2697738.842061483, 1261515.7613145083,
                            2697739.290878663, 1261529.1225134702,
                            2697733.2611742257, 1261529.245388204 });
            pd4.geometry = g4;
            pd4.properties.area = "Gärtnerstrasse bis Tösstalstrasse";
            pd4.properties.project = "Kanalersatz/-Vergrösserung";
            pd4.properties.realizationUntil = DateTime.Now;
            pd4.properties.realizationUntil = pd4.properties.realizationUntil.AddYears(-2);

            RoadWorkProjectController.roadworkProjects.Add(pd4);

            RoadWorkProjectFeature pd5 = new RoadWorkProjectFeature();
            pd5.properties.id = 9373612;
            pd5.properties.projectNo = 636313;
            pd5.properties.place = "Archplatz";
            Geometry g5 = new Geometry(Geometry.GeometryType.Polygon,
                            new double[] { 2696832.6722698356, 1261673.326165659, 
                            2696828.7420268985, 1261635.8935820945,
                            2696838.034250036, 1261634.9134285445,
                            2696841.9767991514, 1261671.5913140983, 
                            2696837.1680888226, 1261676.2307286556, 
                            2696832.6722698356, 1261673.326165659 });
            pd5.geometry = g5;
            pd5.properties.area = "Archstrasse";
            pd5.properties.project = "Gasleitungen neu";
            pd5.properties.realizationUntil = DateTime.Now;
            pd5.properties.realizationUntil = pd5.properties.realizationUntil.AddYears(-3);

            RoadWorkProjectController.roadworkProjects.Add(pd5);

            RoadWorkProjectFeature pd6 = new RoadWorkProjectFeature();
            pd6.properties.id = 643735287;
            pd6.properties.projectNo = 73652826;
            pd6.properties.place = "Untertor";
            pd6.properties.status = "coordinated";
            pd6.properties.priority = "low";
            Geometry g6 = new Geometry(Geometry.GeometryType.Polygon,
                            new double[] { 2697018.3755408432, 1261774.55272711,
                            2697022.7617516345, 1261749.5242846052,
                            2697040.3067223947, 1261753.2093412864,
                            2697035.340282594, 1261778.9831302718,
                            2697018.3755408432, 1261774.55272711 });
            pd6.geometry = g6;
            pd6.properties.area = "Ecke Kasinostrasse";
            pd6.properties.project = "Kanalerneuerung";
            pd6.properties.realizationUntil = DateTime.Now;
            pd6.properties.realizationUntil = pd6.properties.realizationUntil.AddYears(-4);

            RoadWorkProjectController.roadworkProjects.Add(pd6);


            RoadWorkProjectFeature pd7 = new RoadWorkProjectFeature();
            pd7.properties.id = 263840387;
            pd7.properties.projectNo = 8257128;
            pd7.properties.place = "Gertrudstrasse";
            Geometry g7 = new Geometry(Geometry.GeometryType.Polygon,
                            new double[] { 2696518.2807199163, 1261841.5136730669,
                            2696526.2459342587, 1261856.8547097328,
                            2696708.4092541053, 1261770.2038484907,
                            2696700.45586999, 1261754.1604673837,
                            2696518.2807199163, 1261841.5136730669 });
            pd7.geometry = g7;
            pd7.properties.area = "Rudolfstrasse bis Neuwiesenstrasse";
            pd7.properties.project = "Umbau";
            pd7.properties.realizationUntil = DateTime.Now;
            pd7.properties.realizationUntil = pd7.properties.realizationUntil.AddYears(-3);

            RoadWorkProjectController.roadworkProjects.Add(pd7);

            RoadWorkProjectFeature pd8 = new RoadWorkProjectFeature();
            pd8.properties.id = 84736337;
            pd8.properties.projectNo = 93736323;
            pd8.properties.place = "Paulstrasse";
            Geometry g8 = new Geometry(Geometry.GeometryType.Polygon,
                            new double[] { 2696683.1908176546, 1261853.8302712764,
                            2696677.7040868304, 1261844.8479468292,
                            2696739.2587915487, 1261812.8587380715,
                            2696746.542424738, 1261826.7849353685,
                            2696683.1908176546, 1261853.8302712764 });
            pd8.geometry = g8;
            pd8.properties.area = "Ecke Rudolfstrasse";
            pd8.properties.project = "Neubau";
            pd8.properties.realizationUntil = DateTime.Now;
            pd8.properties.realizationUntil = pd8.properties.realizationUntil.AddYears(-1);

            RoadWorkProjectController.roadworkProjects.Add(pd8);

            RoadWorkProjectFeature pd9 = new RoadWorkProjectFeature();
            pd9.properties.id = 927363735;
            pd9.properties.projectNo = 7363637;
            pd9.properties.place = "Walkestrasse";
            Geometry g9 = new Geometry(Geometry.GeometryType.Polygon,
                            new double[] { 2696250.8949284353, 1262205.5116099333,
                            2696261.7768248045, 1262200.3068066793,
                            2696295.154888875, 1262269.4123490702,
                            2696284.2730883285, 1262274.6170309854,
                            2696250.8949284353, 1262205.5116099333 });
            pd9.geometry = g9;
            pd9.properties.area = "Salstrasse bis Ackeretstrasse";
            pd9.properties.project = "Erneuerung Abwasserleitungen";
            pd9.properties.realizationUntil = DateTime.Now;
            pd9.properties.realizationUntil = pd9.properties.realizationUntil.AddYears(-1);

            RoadWorkProjectController.roadworkProjects.Add(pd9);

            RoadWorkProjectFeature pd10 = new RoadWorkProjectFeature();
            pd10.properties.id = 63736327;
            pd10.properties.projectNo = 76265253;
            pd10.properties.place = "Salstrasse";
            Geometry g10 = new Geometry(Geometry.GeometryType.Polygon,
                            new double[] { 2696640.075450667, 1262119.2330952685,
                            2696562.147929726, 1262147.9120184046,
                            2696469.800079144, 1262182.9084852203,
                            2696477.4806486834, 1262201.285955998,
                            2696564.038207495, 1262161.515073112,
                            2696645.508406943, 1262131.490158675,
                            2696640.075450667, 1262119.2330952685 });
            pd10.geometry = g10;
            pd10.properties.area = "Schützenstrasse bis Neuwiesenstrasse";
            pd10.properties.project = "Erneuerung Wasserleitungen";
            pd10.properties.realizationUntil = DateTime.Now;
            pd10.properties.realizationUntil = pd10.properties.realizationUntil.AddYears(-3);

            RoadWorkProjectController.roadworkProjects.Add(pd10);


        }


    }
}