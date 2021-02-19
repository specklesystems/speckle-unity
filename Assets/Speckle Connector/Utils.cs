using System;
using UnityEngine;

namespace Speckle.ConnectorUnity
{
  public static class Utils
  {
    public static int ToIntColor(this Color c)
    {
      return
        System.Drawing.Color
          .FromArgb(Convert.ToInt32(c.r * 255), Convert.ToInt32(c.r * 255), Convert.ToInt32(c.r * 255))
          .ToArgb();
    }

    public static Color ToUnityColor(this int c)
    {
      var argb = System.Drawing.Color.FromArgb(c);
      return new Color(argb.R / 255.0f, argb.G / 255.0f, argb.B / 255.0f);
    }
    
  }
}