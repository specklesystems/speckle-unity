
using System;
using System.Collections;
using System.Diagnostics;
using System.Threading.Tasks;
using NUnit.Framework;
using Speckle.Core.Api;
using UnityEngine;
using UnityEngine.TestTools;


public class PerformanceTest
{
    //This method is much faster
    private static readonly string[] dataSource = new[]
    {
        "https://latest.speckle.dev/streams/24c3741255/commits/0925840e09"
    };
    
    
    [Test, TestCaseSource(nameof(dataSource))]
    public void Receive_GetAwaiterResult(string stream)
    {
        var stopwatch = Stopwatch.StartNew();
    
        Helpers.Receive(stream).GetAwaiter().GetResult();
    
        stopwatch.Stop();
        Console.WriteLine(stopwatch.ElapsedMilliseconds);
        Assert.That(stopwatch.ElapsedMilliseconds, Is.Zero);
    }
    
    
     //This method takes around 46 seconds to complete
     [Test, TestCaseSource(nameof(dataSource))]
     public void Receive_TaskRunAsync(string stream)
     {
         var stopwatch = Stopwatch.StartNew();
    
         Task.Run(async () =>
         {
             await Helpers.Receive(stream);
         }).GetAwaiter().GetResult();
    
         stopwatch.Stop();
         Console.WriteLine(stopwatch.ElapsedMilliseconds);
         Assert.That(stopwatch.ElapsedMilliseconds, Is.Zero);
     }
     
     [UnityTest, TestCaseSource(nameof(dataSource))]
     public IEnumerable Receive_Coroutine(string stream)
     {
         var stopwatch = Stopwatch.StartNew();
         
         Task t = Helpers.Receive(stream);
         t.Start();

         yield return new WaitUntil(() => !t.IsCompleted || stopwatch.ElapsedMilliseconds >= 100000);

         stopwatch.Stop();
         Console.WriteLine(stopwatch.ElapsedMilliseconds);
         Assert.That(stopwatch.ElapsedMilliseconds, Is.Zero);
         Assert.True(t.IsCompletedSuccessfully);
     }
    
    
}
