
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using NUnit.Framework;
using Speckle.Core.Api;


public class PerformanceTest
{
    //This method is much faster
    // [Test]
    // public void PerformanceTestSimplePasses()
    // {
    //     var stopwatch = Stopwatch.StartNew();
    //
    //     Helpers.Receive("https://latest.speckle.dev/streams/24c3741255/commits/0925840e09").GetAwaiter().GetResult();
    //
    //     stopwatch.Stop();
    //     Console.WriteLine(stopwatch.ElapsedMilliseconds);
    //     Assert.That(stopwatch.ElapsedMilliseconds, Is.Zero);
    // }
    
    
    //This method takes around 46 seconds to complete
     [Test]
     public void PerformanceTestSimplePasses()
     {
         var stopwatch = Stopwatch.StartNew();
    
         Task.Run(async () =>
         {
             await Helpers.Receive("https://latest.speckle.dev/streams/24c3741255/commits/0925840e09");
         }).GetAwaiter().GetResult();
    
         stopwatch.Stop();
         Console.WriteLine(stopwatch.ElapsedMilliseconds);
         Assert.That(stopwatch.ElapsedMilliseconds, Is.Zero);
     }
    
}
