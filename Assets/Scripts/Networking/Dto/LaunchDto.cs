using System;
using System.Collections.Generic;

[Serializable]
public class LaunchDto
{
    public string id;
    public string name;
    public string date_utc;
    public string rocket;
    public List<string> payloads;
    public List<string> ships;
}
