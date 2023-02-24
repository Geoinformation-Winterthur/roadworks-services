using System;
using System.Collections.Generic;
using System.Linq;

namespace roadwork_portal_service.Model
{
    public class Geometry
    {
        public string type { get; set; } = "";
        public double[] coordinates { get; set; } = new double[0];
        public double[] bbox { get; set; } = new double[0];
        public double[] center { get; set; } = new double[0];

        public Geometry() { }

        public Geometry(GeometryType type, double[] coordinates)
        {
            if (type == GeometryType.Polygon)
            {
                this.type = "Polygon";
            }
            this.coordinates = coordinates;
            this.bbox = this.getBbox();
            this.center = this.getBboxCenter();
        }

        private double[] getBbox()
        {
            double[] bbox = new double[4];

            List<double> allXValues = new List<double>();
            List<double> allYValues = new List<double>();

            Boolean even = true;
            foreach (double polyCoord in this.coordinates)
            {
                if (even)
                {
                    allXValues.Add(polyCoord);
                }
                else
                {
                    allYValues.Add(polyCoord);
                }
                even = !even;
            }
            double minX = allXValues.Min();
            double maxX = allXValues.Max();
            double minY = allYValues.Min();
            double maxY = allYValues.Max();

            bbox[0] = minX;
            bbox[1] = minY;
            bbox[2] = maxX;
            bbox[3] = maxY;

            return bbox;
        }

        private double[] getBboxCenter()
        {
            double[] center = new double[2];
            double[] bbox = this.getBbox();
            center[0] = (bbox[2] - bbox[0]) / 2;
            center[1] = (bbox[3] - bbox[1]) / 2;
            return center;
        }


        public enum GeometryType
        {
            Polygon
        }
    }
}
