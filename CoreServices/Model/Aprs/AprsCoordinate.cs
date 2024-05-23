namespace CoreServices.Model.Aprs;

public class AprsCoordinate
{
    public AprsCoordinate()
    {
        Name = string.Empty;
    }
    public AprsCoordinate(string name,double lat, double lng)
    {
        Lat = lat;
        Lng = lng;
        Name = name;
    }

    public string Name { get; set; }
    
    public double Lat { get; set; }

    public double Lng { get; set; }
}