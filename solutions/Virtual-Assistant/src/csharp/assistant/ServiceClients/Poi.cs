using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace VirtualAssistant.ServiceClients
{
    public class PoiQuery
    {
        public string Query { get; set; }

        public Coordinate Location { get; set; }

        public string Price_section { get; set; }

    };

    // Refer to http://lbsyun.baidu.com/index.php?title=webapi/guide/webservice-placeapi
    public class Poi
    {
        public string Uid { get; set; }

        public string Name { get; set; }

        public Coordinate Location { get; set; }

        public string Address { get; set; }

        public string Province { get; set; }

        public string City { get; set; }

        public string Area { get; set; }

        public string Street_id { get; set; }

        public string Telephone { get; set; }

        public string Detail { get; set; }

        public Detail Detail_info { get; set; }
    };

    public class Coordinate
    {
        public double Lng { get; set; }

        public double Lat { get; set; }
    };

    public class Detail
    {
        public double Distance { get; set; }

        public string Tag { get; set; }

        public Coordinate Navi_location { get; set; }

        public string Type { get; set; }

        public string Detail_url { get; set; }

        public double Price { get; set; }

        public string Shop_hours { get; set; }

        public string Overall_rating { get; set; }

        public string Taste_rating { get; set; }

        public string Service_rating { get; set; }

        public string Environment_rating { get; set; }

        public string Facility_rating { get; set; }

        public string Hygiene_rating { get; set; }

        public string Technology_rating { get; set; }

        public string Image_num { get; set; }

        public int Groupon_num { get; set; }

        public int Discount_num { get; set; }

        public string Comment_num { get; set; }

        public string Favorite_num { get; set; }

        public string Checkin_num { get; set; }
    }
}
