namespace CoreServices.Model.Aprs;

public class AprsGrid
{
    public AprsGrid()
    {
        Name = string.Empty;
        Grid = string.Empty;
    }
    public AprsGrid(string name, string grid)
    {
        Name = name;
        Grid = grid;
    }
    public string Name { get; set; }

    public string Grid { get; set; }
}