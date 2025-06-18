using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AspnetCoreMvcFull.Models
{

  public class Dashboards
  {
    public int FleetCount { get; set; }
    public int CollectorCount { get; set; }
    public int DriverCount { get; set; }
    public int ClientCount { get; set; }

    public int TotalPickups { get; set; }
    public int MissedPickups { get; set; }
    public double SuccessRate => TotalPickups + MissedPickups > 0
        ? Math.Round(((double)TotalPickups / (TotalPickups + MissedPickups)) * 100, 1)
        : 0;
  }
}
