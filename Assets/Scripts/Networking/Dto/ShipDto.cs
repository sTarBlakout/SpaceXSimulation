using System;
using System.Collections.Generic;

[Serializable]
public class ShipDto
{
    public string id;
    public string name;
    public string type;
    public string home_port;
    public string image;
    public List<string> launches;
}
